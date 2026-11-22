using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes the item, recipe, and placeable assets the inspector can edit later.
/// Existing assets are left alone, except a missing prefab reference is filled in.
/// </summary>
[InitializeOnLoad]
public static class GameContentBuilder
{
    const string Content = "Assets/Resources/GameContent";
    const string Placeables = "Assets/Resources/Placeables";

    static GameContentBuilder()
    {
        EditorApplication.delayCall += () => EnsureAssets();
    }

    [MenuItem("Game/Ensure Crafting Content")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureFolder("Assets/Resources");
        EnsureFolder(Content);
        EnsureFolder(Placeables);

        ImpactType mine = EnsureImpact("Mine", "mine");
        GameObject workbench = EnsurePrefab("Workbench", ItemIds.Workbench, true, new Vector2(1.35f, 0.75f));
        GameObject chair = EnsurePrefab("Chair", ItemIds.Chair, false, new Vector2(0.75f, 0.55f));
        GameObject furnaceObject = EnsurePrefab("Furnace", ItemIds.Furnace, false, new Vector2(1.15f, 0.85f));

        ItemDefinition wood = EnsureItem("Wood", ItemIds.Wood, null);
        ItemDefinition sword = EnsureItem("Sword", ItemIds.Sword, null);
        ItemDefinition bow = EnsureItem("Bow", ItemIds.Bow, null);
        ItemDefinition axe = EnsureItem("Axe", ItemIds.Axe, null);
        ItemDefinition benchItem = EnsureItem("Workbench", ItemIds.Workbench, workbench);
        ItemDefinition chairItem = EnsureItem("Chair", ItemIds.Chair, chair);
        ItemDefinition stone = EnsureItem("Stone", ItemIds.Stone, null);
        ItemDefinition ore = EnsureItem("CopperOre", ItemIds.CopperOre, null);
        ItemDefinition pick = EnsureItem("Pickaxe", ItemIds.Pickaxe, null);
        ItemDefinition furnace = EnsureItem("Furnace", ItemIds.Furnace, furnaceObject);
        LinkPickaxe(pick, mine);
        EnsureRockLoot(stone, ore);

        RecipeDefinition benchRecipe = EnsureRecipe("Workbench", "recipe.workbench");
        RecipeDefinition chairRecipe = EnsureRecipe("Chair", "recipe.chair");
        RecipeDefinition furnaceRecipe = EnsureRecipe("Furnace", "recipe.furnace");

        EnsureCatalog(
            new[] { wood, sword, bow, axe, benchItem, chairItem, stone, ore, pick, furnace },
            new[] { benchRecipe, chairRecipe, furnaceRecipe });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static GameObject EnsurePrefab(string name, string itemId, bool station, Vector2 box)
    {
        string path = Placeables + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        go.AddComponent<SpriteRenderer>();
        PlaceableObject placeable = go.AddComponent<PlaceableObject>();
        placeable.BindId(itemId);
        go.AddComponent<BillboardSort>();
        if (station)
            go.AddComponent<CraftingStation>();

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = box;
        collider.offset = new Vector2(0f, box.y * 0.45f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static ItemDefinition EnsureItem(string file, string id, GameObject prefab)
    {
        string path = Content + "/Item_" + file + ".asset";
        ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            DefaultContent.ConfigureItem(item, id, prefab);
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        if (item.Id != id)
        {
            DefaultContent.ConfigureItem(item, id, prefab);
            EditorUtility.SetDirty(item);
        }
        else if (prefab != null && item.Prefab == null)
        {
            item.AssignPrefab(prefab);
            EditorUtility.SetDirty(item);
        }

        return item;
    }

    static RecipeDefinition EnsureRecipe(string file, string id)
    {
        string path = Content + "/Recipe_" + file + ".asset";
        RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
        if (recipe == null)
        {
            recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            DefaultContent.ConfigureRecipe(recipe, id);
            AssetDatabase.CreateAsset(recipe, path);
            return recipe;
        }

        if (recipe.Id != id)
        {
            DefaultContent.ConfigureRecipe(recipe, id);
            EditorUtility.SetDirty(recipe);
        }

        return recipe;
    }

    static void EnsureCatalog(ItemDefinition[] items, RecipeDefinition[] recipes)
    {
        string path = Content + "/ItemCatalog.asset";
        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            catalog.SetContent(items, recipes);
            AssetDatabase.CreateAsset(catalog, path);
            return;
        }

        if (!catalog.IsUsable)
        {
            catalog.SetContent(items, recipes);
            EditorUtility.SetDirty(catalog);
            return;
        }

        var nextItems = new List<ItemDefinition>();
        ItemDefinition[] currentItems = catalog.Items;
        if (currentItems != null)
        {
            for (int i = 0; i < currentItems.Length; i++)
            {
                if (currentItems[i] != null)
                    nextItems.Add(currentItems[i]);
            }
        }

        var nextRecipes = new List<RecipeDefinition>();
        RecipeDefinition[] currentRecipes = catalog.Recipes;
        if (currentRecipes != null)
        {
            for (int i = 0; i < currentRecipes.Length; i++)
            {
                if (currentRecipes[i] != null)
                    nextRecipes.Add(currentRecipes[i]);
            }
        }

        bool changed = AddMissingItems(nextItems, items);
        changed |= AddMissingRecipes(nextRecipes, recipes);
        if (!changed)
            return;

        catalog.SetContent(nextItems.ToArray(), nextRecipes.ToArray());
        EditorUtility.SetDirty(catalog);
    }

    static ImpactType EnsureImpact(string file, string typeId)
    {
        string path = Content + "/Impact_" + file + ".asset";
        ImpactType impact = AssetDatabase.LoadAssetAtPath<ImpactType>(path);
        if (impact == null)
        {
            impact = ScriptableObject.CreateInstance<ImpactType>();
            impact.Configure(typeId);
            AssetDatabase.CreateAsset(impact, path);
            return impact;
        }

        if (impact.Id != typeId)
        {
            impact.Configure(typeId);
            EditorUtility.SetDirty(impact);
        }

        return impact;
    }

    static void LinkPickaxe(ItemDefinition item, ImpactType mine)
    {
        if (item == null || mine == null)
            return;

        ToolProfile current = item.ResolvedTool;
        if (current != null && current.Impact == mine)
            return;

        float power = current != null && current.Power > 0f ? current.Power : 1f;
        float range = current != null && current.Range > 0f ? current.Range : 3.6f;
        float stamina = current != null && current.Stamina > 0f ? current.Stamina : 6f;
        item.AssignTool(ToolProfile.Swing(mine, power, range, stamina));
        EditorUtility.SetDirty(item);
    }

    static void EnsureRockLoot(ItemDefinition stone, ItemDefinition ore)
    {
        string path = Content + "/Loot_Rock.asset";
        LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootTable>();
            table.Configure(LootLibrary.RockId, LootLibrary.RockEntries(stone, ore));
            AssetDatabase.CreateAsset(table, path);
            return;
        }

        if (table.Drops == null || table.Drops.Length == 0)
        {
            table.Configure(LootLibrary.RockId, LootLibrary.RockEntries(stone, ore));
            EditorUtility.SetDirty(table);
        }
    }

    static bool AddMissingItems(List<ItemDefinition> into, ItemDefinition[] items)
    {
        bool changed = false;
        if (items == null)
            return false;

        for (int i = 0; i < items.Length; i++)
        {
            ItemDefinition item = items[i];
            if (item == null || string.IsNullOrEmpty(item.Id) || HasItem(into, item.Id))
                continue;

            into.Add(item);
            changed = true;
        }

        return changed;
    }

    static bool AddMissingRecipes(List<RecipeDefinition> into, RecipeDefinition[] recipes)
    {
        bool changed = false;
        if (recipes == null)
            return false;

        for (int i = 0; i < recipes.Length; i++)
        {
            RecipeDefinition recipe = recipes[i];
            if (recipe == null || string.IsNullOrEmpty(recipe.Id) || HasRecipe(into, recipe.Id))
                continue;

            into.Add(recipe);
            changed = true;
        }

        return changed;
    }

    static bool HasItem(List<ItemDefinition> items, string id)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Id == id)
                return true;
        }

        return false;
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

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
