using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The items and recipes the game knows about. Loaded from Resources when those assets exist,
/// otherwise built in memory so play still works before the editor has saved them.
/// </summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Game/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    [SerializeField] ItemDefinition[] items = System.Array.Empty<ItemDefinition>();
    [SerializeField] RecipeDefinition[] recipes = System.Array.Empty<RecipeDefinition>();

    Dictionary<string, ItemDefinition> _items;
    Dictionary<string, RecipeDefinition> _recipes;

    public static ItemCatalog Active { get; private set; }

    public ItemDefinition[] Items => items;
    public RecipeDefinition[] Recipes => recipes;

    public bool IsUsable
    {
        get
        {
            return items != null && items.Length > 0 && recipes != null && recipes.Length > 0;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Ensure();
    }

    public static void Reload()
    {
        Active = null;
        Ensure();
    }

    public static void Ensure()
    {
        if (Active != null && Active.IsUsable)
            return;

        ItemCatalog loaded = Resources.Load<ItemCatalog>("GameContent/ItemCatalog");
        if (loaded != null && loaded.IsUsable)
        {
            Active = loaded;
            return;
        }

        Active = DefaultContent.Build();
    }

    public static ItemDefinition Find(string id)
    {
        Ensure();
        return Active != null ? Active.Get(id) : null;
    }

    public void SetContent(ItemDefinition[] nextItems, RecipeDefinition[] nextRecipes)
    {
        items = nextItems ?? System.Array.Empty<ItemDefinition>();
        recipes = nextRecipes ?? System.Array.Empty<RecipeDefinition>();
        _items = null;
        _recipes = null;
    }

    public ItemDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id) || items == null)
            return null;

        if (_items == null || _items.Count != items.Length)
            Rebuild();

        _items.TryGetValue(id, out ItemDefinition found);
        return found;
    }

    public RecipeDefinition FindRecipe(string id)
    {
        if (string.IsNullOrEmpty(id) || recipes == null)
            return null;

        if (_recipes == null || _recipes.Count != recipes.Length)
            Rebuild();

        _recipes.TryGetValue(id, out RecipeDefinition found);
        return found;
    }

    void Rebuild()
    {
        _items = new Dictionary<string, ItemDefinition>();
        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                ItemDefinition item = items[i];
                if (item != null && !string.IsNullOrEmpty(item.Id))
                    _items[item.Id] = item;
            }
        }

        _recipes = new Dictionary<string, RecipeDefinition>();
        if (recipes != null)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe != null && !string.IsNullOrEmpty(recipe.Id))
                    _recipes[recipe.Id] = recipe;
            }
        }
    }
}
