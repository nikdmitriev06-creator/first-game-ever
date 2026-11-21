using UnityEngine;

/// <summary>
/// The starting items and the two recipes. Editor assets are filled from here,
/// and play mode uses the same descriptions when those assets are missing.
/// </summary>
public static class DefaultContent
{
    public static ItemCatalog Build()
    {
        ItemDefinition[] items =
        {
            Create(ItemIds.Wood, null),
            Create(ItemIds.Sword, null),
            Create(ItemIds.Bow, null),
            Create(ItemIds.Axe, null),
            Create(ItemIds.Workbench, Resources.Load<GameObject>("Placeables/Workbench")),
            Create(ItemIds.Chair, Resources.Load<GameObject>("Placeables/Chair"))
        };

        RecipeDefinition[] recipes =
        {
            CreateRecipe("recipe.workbench"),
            CreateRecipe("recipe.chair")
        };

        ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
        catalog.SetContent(items, recipes);
        return catalog;
    }

    public static ItemDefinition Create(string id, GameObject prefab)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        ConfigureItem(item, id, prefab);
        return item;
    }

    public static RecipeDefinition CreateRecipe(string id)
    {
        RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        ConfigureRecipe(recipe, id);
        return recipe;
    }

    public static void ConfigureItem(ItemDefinition item, string id, GameObject prefab)
    {
        if (item == null)
            return;

        switch (id)
        {
            case ItemIds.Wood:
                item.Configure(id, "Древесина", "Wood", ItemIds.WoodStack, ItemCategory.Resource, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.Sword:
                item.Configure(id, "Меч", "Sword", 1, ItemCategory.Weapon, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.Bow:
                item.Configure(id, "Лук", "Bow", 1, ItemCategory.Weapon, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.Axe:
                item.Configure(id, "Топор", "Axe", 1, ItemCategory.Tool, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.Workbench:
                item.Configure(id, "Верстак", "Workbench", 1, ItemCategory.Placeable, PlacementZone.Home, CraftingStation.WorkbenchId, 2.8f, 3, 0.9f, prefab);
                break;
            case ItemIds.Chair:
                item.Configure(id, "Стул", "Chair", 1, ItemCategory.Placeable, PlacementZone.Indoor, null, 0f, 2, 0.55f, prefab);
                break;
            default:
                item.Configure(id, id, id, 1, ItemCategory.Resource, PlacementZone.None, null, 0f, 1, 0.4f, prefab);
                break;
        }
    }

    public static void ConfigureRecipe(RecipeDefinition recipe, string id)
    {
        if (recipe == null)
            return;

        if (id == "recipe.chair")
        {
            recipe.Configure(
                id,
                "Стул",
                "Chair",
                ItemIds.Chair,
                1,
                CraftingStation.WorkbenchId,
                new[] { new Ingredient { ItemId = ItemIds.Wood, Count = 10 } });
            return;
        }

        recipe.Configure(
            "recipe.workbench",
            "Верстак",
            "Workbench",
            ItemIds.Workbench,
            1,
            null,
            new[] { new Ingredient { ItemId = ItemIds.Wood, Count = 10 } });
    }
}
