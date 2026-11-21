/// <summary>
/// Ids for items that can sit in the hotbar or lie on the ground.
/// </summary>
public static class ItemIds
{
    public const string Sword = "sword";
    public const string Bow = "bow";
    public const string Axe = "axe";
    public const string Wood = "wood";

    public const int WoodStack = 99;

    public static bool Stacks(string id)
    {
        return id == Wood;
    }

    public static int MaxCount(string id)
    {
        return Stacks(id) ? WoodStack : 1;
    }
}
