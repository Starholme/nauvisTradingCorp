require("util")

local clusterio_api = require("__clusterio_lib__/api")

--0 off, 1 low, 2 high
local debug = 1
local exportOnTick = 119
local importOnTick = 120
local zoneRadius = 10

function OnBuiltEntity(event)
    local entity = event.entity

    if not (entity and entity.valid) then return end

    if ZoneCheckFailed(entity) then
        entity.destroy()
        game.print("Nauvis Trading Corporation only transports within " .. zoneRadius .. " tiles of your start location.")
        return
    end

    if entity.type ~= "entity-ghost" then
        AddEntity(entity)
    end
end

function ZoneCheckFailed(entity)
    local name = entity.name
    if name == "entity-ghost" then name = entity.ghost_name end
    --Do we care about this entity?
    if name == "ntc-export-chest" or name == "ntc-import-chest" then
        --Is it in the spawn zone?
        local spawn = game.forces["player"].get_spawn_position(entity.surface)
        local x = entity.position.x - spawn.x
        local y = entity.position.y - spawn.y
        if math.abs(x) > zoneRadius or math.abs(y) > zoneRadius then
            return true
        end
    end

    return false
end

function AddEntity(entity)
    --Track export chests
    if entity.name == "ntc-export-chest" then
        if debug > 1 then
            game.print("Add Entity ".. entity.name)
        end
        table.insert(storage.exportChests, entity)
    end
    --Track import chests
    if entity.name == "ntc-import-chest" then
        if debug > 1 then
            game.print("Add Entity ".. entity.name)
        end
        table.insert(storage.importChests, entity)
    end
end

function Reset()
    storage.ticksSinceMasterPinged = 601
    storage.isConnected = false

    storage.exportChests = {}
    storage.exportList = {}

    storage.importChests = {}
    storage.importList = {}
    storage.collectImports = true
    storage.imported = "EMPTY"
    storage.importsAvailable = {}
    storage.retryImportsCounter = 0

    storage.zoneDraw = {}
end

function TickPing(event)
    local ticks = storage.ticksSinceMasterPinged + 1
    storage.ticksSinceMasterPinged = ticks
    if debug > 1 then
        if event.tick % 300 == 0 then
            game.print("ticksSinceMasterPinged:"..ticks)
        end
    end

    if ticks < 300 then
        storage.isConnected = true
    else --If we don't hear from the server in 300 ticks, mark us disconnected.
        storage.isConnected = false
    end
end

function TickExports(event)
    if event.tick % exportOnTick > 0 then return end

    --Loop the export chests, empty them, add the items to a list
    local chests = storage.exportChests
    for _, data in ipairs(chests) do
        local inventory = data.get_inventory(defines.inventory.chest)
        local items = inventory.get_contents()
        for _, iwqc in pairs(items) do
            local itemName = iwqc.name..":"..iwqc.quality
            if debug > 1 then game.print(itemName.."="..iwqc.count) end
            storage.exportList[itemName] = (storage.exportList[itemName] or 0) + iwqc.count
            inventory.remove(iwqc)
        end
    end
    --if connected, send the list to clusterio
    if storage.isConnected then
        local items = {}
        for name, count in pairs(storage.exportList) do
            table.insert(items, {name, count})
        end
        if #items > 0 then
            if debug > 1 then game.print("nauvis_trading_corporation:exportFromInstance") end
            clusterio_api.send_json("nauvis_trading_corporation:exportFromInstance", items)
            storage.exportList = {}
        end
    end
end

function TallyImportsForChest(entity)
    --Ignore chest if it's being deconstructed
    if entity.valid and not entity.to_be_deconstructed(entity.force) then
        local l_sections = entity.get_logistic_sections()
        local inventory = entity.get_inventory(defines.inventory.item_main)

        for i = 1, l_sections.sections_count do --Loop logistic sections
            local l_section = l_sections.sections[i]
            if l_section.active then 
                for j = 1, l_section.filters_count do --Loop each filter/request slot
                    local filter = l_section.filters[j]
                    local amount = filter.min

                    --Get the amount that's in the inventory
                    local invCount = inventory.get_item_count({name=filter.value.name, quality = filter.value.quality})
                    amount = amount - invCount
                    if (amount < 0) then amount = 0 end

                    local itemName = filter.value.name .. ":" .. filter.value.quality
                    storage.importList[itemName] = (storage.exportList[itemName] or 0) + amount
                    if debug > 1 then game.print("ImportTallyAdd:" .. itemName .. amount) end
                end
            end
        end
    end
end

function TryImportChest(entity)
    --Ignore chest if it's being deconstructed
    if entity.valid and not entity.to_be_deconstructed(entity.force) then
        local l_sections = entity.get_logistic_sections()
        local inventory = entity.get_inventory(defines.inventory.item_main)
        local available = storage.importsAvailable

        for i = 1, l_sections.sections_count do --Loop logistic sections
            local l_section = l_sections.sections[i]
            if l_section.active then 
                for j = 1, l_section.filters_count do --Loop each filter/request
                    local filter = l_section.filters[j]
                    local amount = filter.min
                    local itemName = filter.value.name .. ":" .. filter.value.quality

                    --Compare the filter with actual value in inventory
                    local invCount = inventory.get_item_count({name=filter.value.name, quality = filter.value.quality})
                    local requiredAmount = amount - invCount;
                    local foundAmount = 0;
                    if requiredAmount > 0 then
                        --Look for this item in the available imports
                        for k, v in pairs(available) do
                            if v.name == itemName then
                                --Enough items available
                                if requiredAmount < v.count then
                                    v.count = v.count - requiredAmount
                                    foundAmount = requiredAmount
                                --Not enough, or just enough
                                elseif requiredAmount >= v.count then
                                    foundAmount = foundAmount + v.count
                                    requiredAmount = requiredAmount - v.count
                                    available[k] = nil
                                end
                            end
                        end
                        --If some found, add them to inventory
                        if foundAmount > 0 then
                            inventory.insert({name=filter.value.name, quality = filter.value.quality, count = foundAmount})
                        end

                    end
                end
            end
        end
    end
end

function TickImports(event)
    if event.tick % importOnTick > 0 then return end

    if storage.imported ~= "EMPTY" then
        --Shuffle from imported to importsAvailable
        local imports = helpers.json_to_table(storage.imported)
        for k, v in pairs(imports) do
            table.insert(storage.importsAvailable, {name = v[1],  count = v[2]})
        end
        storage.imported = "EMPTY";
    end

    --Fill any requests we can
    local chests = storage.importChests
    for _, data in ipairs(chests) do
        TryImportChest(data)
    end

    if debug > 1 then 
        if storage.collectImports then game.print("Collect imports: true") end
        if not storage.collectImports then game.print("Collect imports: false") end
    end

    --If not on the collect imports stage
    if not storage.collectImports then 
        storage.retryImportsCounter = storage.retryImportsCounter + 1
        if storage.retryImportsCounter > 30 then
            storage.collectImports = true
            game.print("Retrying requests! There might be something wrong with the server.")
        end
        return
    end

    --Loop the import chests, tally required vs on hand
    for _, data in ipairs(chests) do
        TallyImportsForChest(data)
    end

    --if connected, send the list to clusterio
    if storage.isConnected then
        local items = {}
        for name, count in pairs(storage.importList) do
            table.insert(items, {name, count})
        end
        if #items > 0 then
            clusterio_api.send_json("nauvis_trading_corporation:importRequestFromInstance", items)
            storage.importList = {}
            --Stop collecting till we hear back
            storage.collectImports = false
            storage.retryImportsCounter = 0
        end
    end
end

function RemoveEntity(list, entity)
	for i, v in ipairs(list) do
		if v == entity then
			table.remove(list, i)
			break
		end
	end
end

function OnKilledEntity(event)
	local entity = event.entity
	if entity.type ~= "entity-ghost" then
		--remove the entities from the tables as they are dead
		if entity.name == "ntc-export-chest" then
			RemoveEntity(storage.exportChests, entity)
		elseif entity.name == "ntc-import-chest" then
			RemoveEntity(storage.importChests, entity)
        end
	end
end

script.on_init(function()
	clusterio_api.init()
	Reset()
end)

script.on_load(function()
	clusterio_api.init()
end)

script.on_event(defines.events.on_built_entity, function(event)
	OnBuiltEntity(event)
end)

script.on_event(defines.events.on_robot_built_entity, function(event)
	OnBuiltEntity(event)
end)

script.on_event(defines.events.on_entity_died, function(event)
	OnKilledEntity(event)
end)

script.on_event(defines.events.on_robot_pre_mined, function(event)
	OnKilledEntity(event)
end)

script.on_event(defines.events.on_pre_player_mined_item, function(event)
	OnKilledEntity(event)
end)

script.on_event(defines.events.script_raised_destroy, function(event)
	OnKilledEntity(event)
end)

script.on_event(defines.events.on_tick, function(event)
    TickPing(event)
    TickExports(event)
    TickImports(event)
end)

script.on_event(defines.events.on_player_cursor_stack_changed, function(event)
	local player = game.players[event.player_index]
	if not player or not player.valid then
		return
	end

	local drawZone = false

    local stack = player.cursor_stack
    if stack and stack.valid and stack.valid_for_read then
        if stack.name == "ntc-export-chest" or stack.name == "ntc-import-chest" then
            drawZone = true
        end
    elseif player.cursor_ghost then
        if player.cursor_ghost.name == "ntc-export-chest" or player.cursor_ghost.name == "ntc-import-chest" then
            drawZone = true
        end
    end

	if drawZone then
		if not storage.zoneDraw[event.player_index] then
			local spawn = player.force.get_spawn_position(player.surface)
			local x0 = spawn.x
			local y0 = spawn.y

			storage.zoneDraw[event.player_index] = rendering.draw_rectangle {
				color = {r=0.8 , g=0.1, b=0},
				width = 12,
				filled = false,
				left_top = {x0 - zoneRadius, y0 - zoneRadius},
				right_bottom = {x0 + zoneRadius, y0 + zoneRadius},
				surface = player.surface,
				players = {player},
				draw_on_ground = true,
			}
		end
	else
		if storage.zoneDraw[event.player_index] then
            storage.zoneDraw[event.player_index].destroy()
			storage.zoneDraw[event.player_index] = nil
		end
	end
end)