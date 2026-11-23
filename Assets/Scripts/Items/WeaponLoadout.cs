/// <summary>
/// Which held component plays an item. The sword and the bow keep their own presenters.
/// Every other weapon profile shares <see cref="ProfileWeapon"/>.
/// </summary>
public static class WeaponLoadout
{
    public static bool UsesProfileHand(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || itemId == ItemIds.Sword || itemId == ItemIds.Bow)
            return false;

        ItemDefinition definition = ItemCatalog.Find(itemId);
        if (definition == null || definition.Category != ItemCategory.Weapon)
            return false;

        return definition.ResolvedWeapon != null;
    }
}
