require("util")

local clusterio_api = require("__clusterio_lib__/api")

--0 off, 1 low, 2 high
local debug = 1

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
        if debug > 1 then
            game.print("Add Entity ".. entity.name)
        end
        table.insert(storage.exportChests, entity)
    end
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
    storage.exportList = {} --{"itemName:quality"=count, ...}

    storage.importChests = {}
    storage.importList = {} --{"itemName:quality"=count, ...}
    storage.collectImports = true
    storage.imported = "EMPTY"
    storage.importsAvailable = {}
    storage.retryImportsCounter = 0
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
            if debug > 0 then game.print(itemName.."="..iwqc.count) end
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
        for i = 1, l_sections.sections_count do
            local l_section = l_sections.sections[i]
            if l_section.active then 
                for j = 1, l_section.filters_count do
                    local filter = l_section.filters[j]
                    local amount = filter.min
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
        for i = 1, l_sections.sections_count do
            local l_section = l_sections.sections[i]
            if l_section.active then 
                for j = 1, l_section.filters_count do
                    local filter = l_section.filters[j]
                    local amount = filter.min
                    local itemName = filter.value.name .. ":" .. filter.value.quality
                    
                    --Compare the filter with actual value in inventory
                    local invCount = inventory.get_item_count({name=filter.value.name, quality = filter.value.quality})
                    local requiredAmount = amount - invCount;
                    local foundAmount = 0;
                    game.print("TryImportChest req amount"..requiredAmount)
                    if requiredAmount > 0 then
                        --Look for this item in the available imports
                        for k, v in pairs(available) do
                            game.print("available loop")
                            --game.print(serpent.block(v))
                            if v.name == itemName then
                                game.print("available loop - found item, count:" .. v.count .. " req:" .. requiredAmount .. " itemname" .. itemName)
                                --Enough items available
                                if requiredAmount < v.count then
                                    game.print("available loop - enough")
                                    v.count = v.count - requiredAmount
                                    foundAmount = requiredAmount
                                --Not enough, or just enough
                                elseif requiredAmount >= v.count then
                                    game.print("available loop - not enough")
                                    foundAmount = foundAmount + v.count
                                    requiredAmount = requiredAmount - v.count
                                    available[k] = nil
                                end
                            end
                        end
                        game.print("available adding foundAmount:"..foundAmount)
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
    if event.tick % 120 > 0 then return end
    
    if storage.imported ~= "EMPTY" then
        --Shuffle from imported to importsAvailable
        game.print("storage.imported")
        game.print(storage.imported)
        local imports = helpers.json_to_table(storage.imported)
        for k, v in pairs(imports) do
            table.insert(storage.importsAvailable, {name = v[1],  count = v[2]})
            game.print("Added to available: ".. v[1] .. " " .. v[2])
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
            game.print("Retrying requests!")
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
            if debug > 1 then game.print("nauvis_trading_corporation:importRequestFromInstance") end
            clusterio_api.send_json("nauvis_trading_corporation:importRequestFromInstance", items)
            storage.importList = {}
            --Stop collecting till we hear back
            storage.collectImports = false
            storage.retryImportsCounter = 0
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