using UnityEngine;

public enum ItemRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    VeryRare = 3
}

public static class ItemRarityColors
{
    public static Color For(ItemDefinition item)
    {
        if (item == null)
            return Color.white;

        switch (item.Rarity)
        {
            case ItemRarity.Uncommon: return new Color(0.72f, 0.9f, 0.62f, 1f);
            case ItemRarity.Rare: return new Color(0.62f, 0.78f, 1f, 1f);
            case ItemRarity.VeryRare: return new Color(0.86f, 0.66f, 1f, 1f);
            default: return Color.white;
        }
    }
}
