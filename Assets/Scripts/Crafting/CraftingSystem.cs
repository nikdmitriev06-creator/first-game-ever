using System.Collections.Generic;
using UnityEngine;

public enum CraftDeny
{
    None,
    Station,
    Ingredients,
    Space,
    Locked
}

/// <summary>
/// Checks a recipe and, only when every check passes, spends the ingredients and gives the result.
/// The menu calls this. It does not edit slots itself.
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    PlayerInventory _inventory;
    CraftAccess _access;

    public void UseAccess(CraftAccess access)
    {
        _access = access;
    }

    void Awake()
    {
        Bind();
    }

    PlayerInventory Bind()
    {
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
        return _inventory;
    }

    public void CollectVisible(List<RecipeDefinition> into)
    {
        into.Clear();
        ItemCatalog.Ensure();
        ItemCatalog catalog = ItemCatalog.Active;
        if (catalog == null || catalog.Recipes == null || Bind() == null)
            return;

        Vector2 feet = transform.position;
        RecipeDefinition[] recipes = catalog.Recipes;
        for (int i = 0; i < recipes.Length; i++)
        {
            RecipeDefinition recipe = recipes[i];
            if (recipe == null)
                continue;

            if (!Unlocked(recipe))
                continue;

            if (recipe.NeedsStation && !CraftingStation.AnyInRange(recipe.StationId, feet))
                continue;

            into.Add(recipe);
        }
    }

    public CraftDeny DenyReason(RecipeDefinition recipe)
    {
        if (recipe == null || Bind() == null)
            return CraftDeny.Ingredients;

        if (!Unlocked(recipe))
            return CraftDeny.Locked;

        if (!StationReady(recipe))
            return CraftDeny.Station;

        if (!IngredientsReady(recipe))
            return CraftDeny.Ingredients;

        if (!Bind().CanFitAfterSpend(recipe.ResultId, recipe.ResultCount, recipe.Ingredients))
            return CraftDeny.Space;

        return CraftDeny.None;
    }

    public bool CanCraft(RecipeDefinition recipe)
    {
        return DenyReason(recipe) == CraftDeny.None;
    }

    public bool TryCraft(RecipeDefinition recipe)
    {
        if (!CanCraft(recipe))
            return false;

        PlayerInventory inventory = Bind();
        Ingredient[] cost = recipe.Ingredients ?? System.Array.Empty<Ingredient>();
        for (int i = 0; i < cost.Length; i++)
        {
            if (inventory.TryConsume(cost[i].ItemId, cost[i].Count))
                continue;

            for (int refund = 0; refund < i; refund++)
                inventory.TryAdd(cost[refund].ItemId, cost[refund].Count);

            return false;
        }

        int got = inventory.TryAdd(recipe.ResultId, recipe.ResultCount);
        if (got >= recipe.ResultCount)
            return true;

        if (got > 0)
            inventory.TryConsume(recipe.ResultId, got);

        for (int refund = 0; refund < cost.Length; refund++)
            inventory.TryAdd(cost[refund].ItemId, cost[refund].Count);

        return false;
    }

    bool Unlocked(RecipeDefinition recipe)
    {
        if (recipe == null || !recipe.NeedsUnlock)
            return true;

        return _access != null && _access.Allows(recipe);
    }

    bool StationReady(RecipeDefinition recipe)
    {
        if (!recipe.NeedsStation)
            return true;

        return CraftingStation.AnyInRange(recipe.StationId, transform.position);
    }

    bool IngredientsReady(RecipeDefinition recipe)
    {
        Ingredient[] cost = recipe.Ingredients;
        if (cost == null)
            return true;

        for (int i = 0; i < cost.Length; i++)
        {
            if (cost[i].Count <= 0)
                continue;

            if (Bind().CountOf(cost[i].ItemId) < cost[i].Count)
                return false;
        }

        return true;
    }
}
