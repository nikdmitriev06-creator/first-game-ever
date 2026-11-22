/// <summary>
/// Ids for items that can sit in the hotbar or lie on the ground.
/// </summary>
public static class ItemIds
{
    public const string Sword = "sword";
    public const string Bow = "bow";
    public const string Axe = "axe";
    public const string Wood = "wood";
    public const string Workbench = "workbench";
    public const string Chair = "chair";
    public const string Stone = "stone";
    public const string CopperOre = "copper_ore";
    public const string Pickaxe = "pickaxe";
    public const string Furnace = "furnace";

    public const int WoodStack = 99;
    public const int ResourceStack = 99;

    public static bool Stacks(string id)
    {
        ItemDefinition item = ItemCatalog.Find(id);
        if (item != null)
            return item.MaxStack > 1;

        return id == Wood;
    }

    public static int MaxCount(string id)
    {
        ItemDefinition item = ItemCatalog.Find(id);
        if (item != null)
            return item.MaxStack;

        return id == Wood ? WoodStack : 1;
    }
}
