using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Levels/Level")]
public class LevelDefinition : ScriptableObject
{
    [SerializeField] string id = "forest";
    [SerializeField] string displayName = "Лес";
    [SerializeField] string biomeId = "forest";
    [SerializeField] RoomDefinition startRoom;
    [SerializeField] RoomDefinition endRoom;
    [SerializeField] RoomDefinition[] rooms = Array.Empty<RoomDefinition>();
    [SerializeField] RoomDefinition[] requiredRooms = Array.Empty<RoomDefinition>();
    [SerializeField] int minRooms = 3;
    [SerializeField] int maxRooms = 3;
    [SerializeField] bool allowRepeats;
    [SerializeField] int maxRepeat = 1;
    [SerializeField] SpawnPoolDefinition enemyPool;
    [SerializeField] SpawnPoolDefinition resourcePool;
    [SerializeField] string completionNote;

    public string Id => id;
    public string DisplayName => displayName;
    public string BiomeId => biomeId;
    public RoomDefinition StartRoom => startRoom;
    public RoomDefinition EndRoom => endRoom;
    public RoomDefinition[] Rooms => rooms ?? Array.Empty<RoomDefinition>();
    public RoomDefinition[] RequiredRooms => requiredRooms ?? Array.Empty<RoomDefinition>();
    public int MinRooms => minRooms;
    public int MaxRooms => maxRooms;
    public bool AllowRepeats => allowRepeats;
    public int MaxRepeat => maxRepeat;
    public SpawnPoolDefinition EnemyPool => enemyPool;
    public SpawnPoolDefinition ResourcePool => resourcePool;
    public string CompletionNote => completionNote;

    public void Configure(
        string levelId,
        string name,
        string biome,
        RoomDefinition start,
        RoomDefinition end,
        RoomDefinition[] pool,
        RoomDefinition[] required,
        int min,
        int max,
        bool repeats,
        SpawnPoolDefinition enemies,
        SpawnPoolDefinition resources)
    {
        id = levelId;
        displayName = name;
        biomeId = biome;
        startRoom = start;
        endRoom = end;
        rooms = pool ?? Array.Empty<RoomDefinition>();
        requiredRooms = required ?? Array.Empty<RoomDefinition>();
        minRooms = Mathf.Max(1, min);
        maxRooms = Mathf.Max(minRooms, max);
        allowRepeats = repeats;
        maxRepeat = 1;
        enemyPool = enemies;
        resourcePool = resources;
    }

    public int RollCount(System.Random random)
    {
        int min = Mathf.Max(1, minRooms);
        int max = Mathf.Max(min, maxRooms);
        if (random == null || min == max)
            return min;

        return random.Next(min, max + 1);
    }

    public int RepeatLimit()
    {
        if (!allowRepeats)
            return 1;

        return maxRepeat <= 0 ? 99 : maxRepeat;
    }
}

[Serializable]
public class LayoutPlacement
{
    public RoomDefinition room;
    public Vector2Int origin;
}

/// <summary>
/// A hand-built arrangement of rooms. Generation can write into this asset so the result can be inspected.
/// </summary>
[CreateAssetMenu(fileName = "Layout", menuName = "Game/Levels/Layout")]
public class LevelLayoutDefinition : ScriptableObject
{
    [SerializeField] string id = "layout";
    [SerializeField] LevelDefinition level;
    [SerializeField] int seed = 1;
    [SerializeField] List<LayoutPlacement> placements = new List<LayoutPlacement>();

    public string Id => id;
    public LevelDefinition Level => level;
    public int Seed => seed;
    public IReadOnlyList<LayoutPlacement> Placements => placements;

    public void Configure(string layoutId, LevelDefinition source, int layoutSeed)
    {
        id = layoutId;
        level = source;
        seed = layoutSeed;
    }

    public void SetPlacements(List<LayoutPlacement> next, int layoutSeed)
    {
        placements = next ?? new List<LayoutPlacement>();
        seed = layoutSeed;
    }
}

/// <summary>
/// Points the build at the level assets. Lives in Resources so the demo content is included.
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Game/Levels/Catalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] LevelDefinition[] levels = Array.Empty<LevelDefinition>();
    [SerializeField] TerrainLibrary terrains;
    [SerializeField] LevelLayoutDefinition demoLayout;

    public LevelDefinition[] Levels => levels ?? Array.Empty<LevelDefinition>();
    public TerrainLibrary Terrains => terrains;
    public LevelLayoutDefinition DemoLayout => demoLayout;

    public void Set(LevelDefinition[] next, TerrainLibrary library, LevelLayoutDefinition layout)
    {
        levels = next ?? Array.Empty<LevelDefinition>();
        terrains = library;
        demoLayout = layout;
    }
}
