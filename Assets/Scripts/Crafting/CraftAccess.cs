using System.Collections.Generic;

/// <summary>
/// Which recipe gates are open. An empty unlock id on a recipe means the recipe is always known.
/// </summary>
public sealed class CraftAccess
{
    readonly HashSet<string> _open = new HashSet<string>();

    public void Unlock(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _open.Add(id);
    }

    public bool Allows(RecipeDefinition recipe)
    {
        if (recipe == null || !recipe.NeedsUnlock)
            return true;

        return _open.Contains(recipe.UnlockId);
    }
}
