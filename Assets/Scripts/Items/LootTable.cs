using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One possible stack from a break. Chance of 1 always drops. Chance of 0 never does.
/// Entries are rolled independently, so a later resource does not replace an earlier one.
/// </summary>
[Serializable]
public struct LootDrop
{
    public ItemDefinition Item;
    public int Min;
    public int Max;
    public float Chance;

    public string ResolvedId => Item != null ? Item.Id : null;

    public bool Succeeds(ILootRandom random)
    {
        if (Chance >= 1f)
            return true;

        if (Chance <= 0f || random == null)
            return false;

        return random.Next01() < Chance;
    }

    public int RollCount(ILootRandom random)
    {
        int min = Mathf.Max(0, Min);
        int max = Mathf.Max(min, Max);
        if (min == max || random == null)
            return min;

        return random.Range(min, max + 1);
    }
}

public struct LootGrant
{
    public string ItemId;
    public int Count;
}

/// <summary>
/// Source of the rolls in a loot table. Tests pass a fixed sequence or a seeded generator.
/// </summary>
public interface ILootRandom
{
    float Next01();
    int Range(int minInclusive, int maxExclusive);
}

public sealed class UnityLootRandom : ILootRandom
{
    public static readonly UnityLootRandom Default = new UnityLootRandom();

    public float Next01()
    {
        return UnityEngine.Random.value;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return UnityEngine.Random.Range(minInclusive, maxExclusive);
    }
}

public sealed class SeededLootRandom : ILootRandom
{
    readonly System.Random _random;

    public SeededLootRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    public float Next01()
    {
        return (float)_random.NextDouble();
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }
}

/// <summary>
/// What a broken body can leave on the ground. The body does not know the item ids itself.
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] LootDrop[] drops = Array.Empty<LootDrop>();

    public string Id => id;
    public LootDrop[] Drops => drops ?? Array.Empty<LootDrop>();

    public void Configure(string tableId, LootDrop[] entries)
    {
        id = tableId;
        drops = entries ?? Array.Empty<LootDrop>();
    }

    public void Roll(List<LootGrant> into, ILootRandom random)
    {
        if (into == null)
            return;

        into.Clear();
        if (drops == null || drops.Length == 0)
            return;

        ILootRandom roll = random ?? UnityLootRandom.Default;
        for (int i = 0; i < drops.Length; i++)
        {
            LootDrop drop = drops[i];
            string itemId = drop.ResolvedId;
            if (string.IsNullOrEmpty(itemId))
                continue;

            if (!drop.Succeeds(roll))
                continue;

            int count = drop.RollCount(roll);
            if (count <= 0)
                continue;

            into.Add(new LootGrant { ItemId = itemId, Count = count });
        }
    }
}

/// <summary>
/// Shared loot tables. A rock type points at one table. Another type can use another table.
/// </summary>
public static class LootLibrary
{
    public const string RockId = "loot.rock";
    public const float RockCopperChance = 0.15f;
    public const int RockStoneMin = 1;
    public const int RockStoneMax = 2;
    public const int RockCopperCount = 1;

    public static LootTable Rock { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Rock = null;
    }

    public static void Ensure()
    {
        if (Rock != null)
            return;

        LootTable[] loaded = Resources.LoadAll<LootTable>("GameContent");
        Rock = Find(loaded, RockId);
        if (Rock == null)
            Rock = BuildFallback();
    }

    public static LootDrop[] RockEntries(ItemDefinition stone, ItemDefinition ore)
    {
        return new[]
        {
            new LootDrop { Item = stone, Min = RockStoneMin, Max = RockStoneMax, Chance = 1f },
            new LootDrop { Item = ore, Min = RockCopperCount, Max = RockCopperCount, Chance = RockCopperChance }
        };
    }

    static LootTable Find(LootTable[] loaded, string tableId)
    {
        if (loaded == null)
            return null;

        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].Id == tableId)
                return loaded[i];
        }

        return null;
    }

    static LootTable BuildFallback()
    {
        ItemCatalog.Ensure();
        LootTable table = ScriptableObject.CreateInstance<LootTable>();
        table.hideFlags = HideFlags.HideAndDontSave;
        table.Configure(RockId, RockEntries(ItemCatalog.Find(ItemIds.Stone), ItemCatalog.Find(ItemIds.CopperOre)));
        return table;
    }
}
