local entity = table.deepcopy(data.raw["container"]["steel-chest"])
entity.name = "ntc-export-chest"

local item = table.deepcopy(data.raw["item"]["steel-chest"])
item.name = "ntc-export-chest"
item.place_result = "ntc-export-chest"

local recipe = {
    type = "recipe",
    name = "ntc-export-chest",
    enabled = true,
    energy_required = 1, -- time to craft in seconds (at crafting speed 1)
    ingredients = {
      {type = "item", name = "iron-plate", amount = 1}
    },
    results = {{type = "item", name = "ntc-export-chest", amount = 1}}
}

data:extend{entity, item, recipe}