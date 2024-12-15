require("util")

local clusterio_api = require("__clusterio_lib__/api")

local debug = true

function OnBuiltEntity(event)
    local entity = event.entity

    if not (entity and entity.valid) then return end

    local name = entity.name
    if name == "entity-ghost" then name = entity.ghost_name end

    if entity.type ~= "entity-ghost" then
        AddEntity(entity)
    end
end

function AddEntity(entity)
    if entity.name == "ntc-export-chest" then
        if debug then
            game.print("Add Entity ".. entity.name)
        end
        table.insert(storage.exportChests, entity)
    end
    if entity.name == "ntc-import-chest" then
        if debug then
            game.print("Add Entity ".. entity.name)
        end
        table.insert(storage.importChests, entity)
    end
end

function Reset()
    storage.ticksSinceMasterPinged = 601
    storage.isConnected = false
    storage.exportChests = {}
    storage.exportList = {} --{"itemName:quality"=count, ...}

    storage.importChests = {}
    storage.importList = {} --{"itemName:quality"=count, ...}
    storage.collectImports = true
end

function TickPing(event)
    local ticks = storage.ticksSinceMasterPinged + 1
    storage.ticksSinceMasterPinged = ticks
    if debug then
        if event.tick % 300 == 0 then
            game.print("ticksSinceMasterPinged:"..ticks)
        end
    end
    if ticks < 300 then
        storage.isConnected = true
    else
        storage.isConnected = false
    end
end

function TickExports(event)
    if event.tick % 120 > 0 then return end
    --Loop the export chests, empty them, add the items to a list
    local chests = storage.exportChests
    for _, data in ipairs(chests) do
        local inventory = data.get_inventory(defines.inventory.chest)
        local items = inventory.get_contents()
        for _, iwqc in pairs(items) do
            local itemName = iwqc.name..":"..iwqc.quality
            if debug then game.print(itemName.."="..iwqc.count) end
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
            if debug then game.print("nauvis_trading_corporation:exportFromInstance") end
            clusterio_api.send_json("nauvis_trading_corporation:exportFromInstance", items)
            storage.exportList = {}
        end
    end
end

function TallyImportsForChest(entity)
    --Ignore chest if it's being deconstructed
    if entity.valid and not entity.to_be_deconstructed(entity.force) then
        local l_sections = entity.get_logistic_sections()
        for i = 1, l_sections.sections_count do
            local l_section = l_sections.sections[i]
            if l_section.active then 
                for j = 1, l_section.filters_count do
                    local filter = l_section.filters[j]
                    local amount = filter.min
                    local itemName = filter.value.name .. ":" .. filter.value.quality
                    storage.importList[itemName] = (storage.exportList[itemName] or 0) + amount
                    if debug then game.print("ImportTallyAdd:" .. itemName .. amount) end
                end
            end
        end
    end
end

function TickImports(event)
    if event.tick % 6 > 0 then return end
    --Fill any requests we can

    --If not on the collect imports stage
    if not storage.collectImports then return end

    --Loop the import chests, tally required vs on hand
    local chests = storage.importChests
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
            if debug then game.print("nauvis_trading_corporation:importRequestFromInstance") end
            clusterio_api.send_json("nauvis_trading_corporation:importRequestFromInstance", items)
            storage.importList = {}
            --Stop collecting till we hear back
            storage.collectImports = false
        end
    end
end

script.on_init(function()
	clusterio_api.init()
	--RegisterClusterioEvents()
	Reset()
end)

script.on_load(function()
	clusterio_api.init()
	--RegisterClusterioEvents()
end)

script.on_event(defines.events.on_built_entity, function(event)
	OnBuiltEntity(event)
end)

script.on_event(defines.events.on_robot_built_entity, function(event)
	OnBuiltEntity(event)
end)

script.on_event(defines.events.on_tick, function(event)
    TickPing(event)
    TickExports(event)
    TickImports(event)
end)