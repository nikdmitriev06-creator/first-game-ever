using System.Collections.Generic;
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
            Create(ItemIds.Chair, Resources.Load<GameObject>("Placeables/Chair")),
            Create(ItemIds.Stone, null),
            Create(ItemIds.CopperOre, null),
            Create(ItemIds.Pickaxe, null),
            Create(ItemIds.Furnace, Resources.Load<GameObject>("Placeables/Furnace"))
        };

        RecipeDefinition[] recipes =
        {
            CreateRecipe("recipe.workbench"),
            CreateRecipe("recipe.chair"),
            CreateRecipe("recipe.furnace")
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

    public static ItemCatalog IncludeMissing(ItemCatalog source)
    {
        if (source == null)
            return Build();

        if (source.HasMiningContent())
            return source;

        var items = new List<ItemDefinition>();
        AppendItems(items, source.Items);
        AddIfMissing(items, ItemIds.Stone, null);
        AddIfMissing(items, ItemIds.CopperOre, null);
        AddIfMissing(items, ItemIds.Pickaxe, null);
        AddIfMissing(items, ItemIds.Furnace, Resources.Load<GameObject>("Placeables/Furnace"));

        var recipes = new List<RecipeDefinition>();
        AppendRecipes(recipes, source.Recipes);
        if (!HasRecipe(recipes, "recipe.furnace"))
            recipes.Add(CreateRecipe("recipe.furnace"));

        ItemCatalog memory = ScriptableObject.CreateInstance<ItemCatalog>();
        memory.hideFlags = HideFlags.HideAndDontSave;
        memory.SetContent(items.ToArray(), recipes.ToArray());
        return memory;
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
            case ItemIds.Stone:
                item.Configure(id, "Камень", "Stone", ItemIds.ResourceStack, ItemCategory.Resource, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.CopperOre:
                item.Configure(id, "Медная руда", "Copper Ore", ItemIds.ResourceStack, ItemCategory.Resource, PlacementZone.None, null, 0f, 1, 0.4f, null);
                break;
            case ItemIds.Pickaxe:
                item.Configure(id, "Кирка", "Pickaxe", 1, ItemCategory.Tool, PlacementZone.None, null, 0f, 1, 0.4f, null);
                ImpactLibrary.Ensure();
                item.AssignTool(ToolProfile.Swing(ImpactLibrary.Mine, 1f, 3.6f, 6f));
                break;
            case ItemIds.Furnace:
                item.Configure(id, "Печь", "Furnace", 1, ItemCategory.Placeable, PlacementZone.Home, null, 0f, 3, 0.85f, prefab);
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

        if (id == "recipe.furnace")
        {
            recipe.Configure(
                id,
                "Печь",
                "Furnace",
                ItemIds.Furnace,
                1,
                CraftingStation.WorkbenchId,
                new[]
                {
                    new Ingredient { ItemId = ItemIds.Stone, Count = 20 },
                    new Ingredient { ItemId = ItemIds.Wood, Count = 10 }
                });
            return;
        }

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

    static void AppendItems(List<ItemDefinition> into, ItemDefinition[] source)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && !string.IsNullOrEmpty(source[i].Id))
                into.Add(source[i]);
        }
    }

    static void AppendRecipes(List<RecipeDefinition> into, RecipeDefinition[] source)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && !string.IsNullOrEmpty(source[i].Id))
                into.Add(source[i]);
        }
    }

    static void AddIfMissing(List<ItemDefinition> items, string id, GameObject prefab)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Id == id)
                return;
        }

        items.Add(Create(id, prefab));
    }

    static bool HasRecipe(List<RecipeDefinition> recipes, string id)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].Id == id)
                return true;
        }

        return false;
    }
}
