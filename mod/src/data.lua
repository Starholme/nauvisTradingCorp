local entity = table.deepcopy(data.raw["container"]["steel-chest"])
entity.name = "ntc-export-chest"

local item = table.deepcopy(data.raw["item"]["steel-chest"])
item.name = "ntc-export-chest"
item.place_result = "ntc-export-chest"

local recipe = {
    type = "recipe",
    name = "ntc-export-chest",
    enabled = true,
    energy_required = 1,
    ingredients = {
      {type = "item", name = "iron-plate", amount = 1}
    },
    results = {{type = "item", name = "ntc-export-chest", amount = 1}}
}

data:extend{entity, item, recipe}

entity = table.deepcopy(data.raw["container"]["steel-chest"])
entity.name = "ntc-import-chest"
entity.type="logistic-container"
entity.logistic_mode = "buffer"
entity.render_not_in_network_icon = false

item = table.deepcopy(data.raw["item"]["steel-chest"])
item.name = "ntc-import-chest"
item.place_result = "ntc-import-chest"

recipe = {
    type = "recipe",
    name = "ntc-import-chest",
    enabled = true,
    energy_required = 1,
    ingredients = {
      {type = "item", name = "iron-plate", amount = 1}
    },
    results = {{type = "item", name = "ntc-import-chest", amount = 1}}
}

data:extend{entity, item, recipe}