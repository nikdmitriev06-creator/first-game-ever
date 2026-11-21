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

        GameObject workbench = EnsurePrefab("Workbench", ItemIds.Workbench, true, new Vector2(1.35f, 0.75f));
        GameObject chair = EnsurePrefab("Chair", ItemIds.Chair, false, new Vector2(0.75f, 0.55f));

        ItemDefinition wood = EnsureItem("Wood", ItemIds.Wood, null);
        ItemDefinition sword = EnsureItem("Sword", ItemIds.Sword, null);
        ItemDefinition bow = EnsureItem("Bow", ItemIds.Bow, null);
        ItemDefinition axe = EnsureItem("Axe", ItemIds.Axe, null);
        ItemDefinition benchItem = EnsureItem("Workbench", ItemIds.Workbench, workbench);
        ItemDefinition chairItem = EnsureItem("Chair", ItemIds.Chair, chair);

        RecipeDefinition benchRecipe = EnsureRecipe("Workbench", "recipe.workbench");
        RecipeDefinition chairRecipe = EnsureRecipe("Chair", "recipe.chair");

        EnsureCatalog(
            new[] { wood, sword, bow, axe, benchItem, chairItem },
            new[] { benchRecipe, chairRecipe });

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
        }
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
