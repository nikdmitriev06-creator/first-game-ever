using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Picks a variant from a pool. The room only supplies the pool and the current difficulty.
/// </summary>
public static class SpawnResolver
{
    public static bool EntryAllows(SpawnEntry entry, int difficulty)
    {
        if (entry == null || !entry.enabled || entry.weight <= 0)
            return false;

        if (difficulty < entry.minDifficulty)
            return false;

        if (entry.maxDifficulty > 0 && difficulty > entry.maxDifficulty)
            return false;

        return true;
    }

    public static bool PointAllows(bool enabled, int difficulty, int minDifficulty, int maxDifficulty)
    {
        if (!enabled)
            return false;

        if (difficulty < minDifficulty)
            return false;

        if (maxDifficulty > 0 && difficulty > maxDifficulty)
            return false;

        return true;
    }

    public static List<SpawnEntry> Available(SpawnPoolDefinition pool, int difficulty)
    {
        var list = new List<SpawnEntry>();
        if (pool == null)
            return list;

        IReadOnlyList<SpawnEntry> entries = pool.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (EntryAllows(entries[i], difficulty))
                list.Add(entries[i]);
        }

        return list;
    }

    public static SpawnEntry Pick(IReadOnlyList<SpawnEntry> entries, float roll01)
    {
        if (entries == null || entries.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];
            if (entry != null && entry.weight > 0)
                total += entry.weight;
        }

        if (total <= 0)
            return null;

        float roll = Mathf.Clamp(roll01, 0f, 0.999999f);
        float target = roll * total;
        float cursor = 0f;
        SpawnEntry last = null;
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];
            if (entry == null || entry.weight <= 0)
                continue;

            last = entry;
            cursor += entry.weight;
            if (target < cursor)
                return entry;
        }

        return last;
    }

    public static int PickCount(int min, int max, float roll01)
    {
        min = Mathf.Max(0, min);
        max = Mathf.Max(min, max);
        if (min == max)
            return min;

        int span = max - min + 1;
        float roll = Mathf.Clamp(roll01, 0f, 0.999999f);
        int offset = Mathf.FloorToInt(roll * span);
        if (offset >= span)
            offset = span - 1;

        return min + offset;
    }
}

public sealed class SpawnRoll
{
    readonly System.Random _random;

    public SpawnRoll(int seed)
    {
        _random = new System.Random(seed);
    }

    public float Next01()
    {
        return (float)_random.NextDouble();
    }

    public SpawnEntry Pick(IReadOnlyList<SpawnEntry> entries)
    {
        return SpawnResolver.Pick(entries, Next01());
    }

    public int Count(int min, int max)
    {
        return SpawnResolver.PickCount(min, max, Next01());
    }

    public int Index(int count)
    {
        if (count <= 1)
            return 0;

        return _random.Next(0, count);
    }
}
