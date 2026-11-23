using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PaintedCell
{
    public int x;
    public int y;
    public int layer;
    public TerrainDefinition terrain;
}

/// <summary>
/// A marker stored on the room asset: prop, spawn rule, or connection socket.
/// The spawn marker names a pool, never a specific creature.
/// </summary>
[Serializable]
public class RoomMarkerData
{
    public string id;
    public MarkerKind kind;
    public int x;
    public int y;
    public GameObject prefab;
    public SpawnPoolDefinition pool;
    public SpawnCategory category;
    public int minCount = 1;
    public int maxCount = 1;
    public float radius = 1.5f;
    public int minDifficulty = 1;
    public int maxDifficulty;
    public SpawnRepeat repeat = SpawnRepeat.OncePerVisit;
    public bool enabled = true;
    public string[] allowedSurfaceIds = Array.Empty<string>();
    public IsoDirection direction = IsoDirection.NorthEast;
    public bool required;
    public string socketTag;

    public RoomMarkerData Clone(string nextId)
    {
        return new RoomMarkerData
        {
            id = nextId,
            kind = kind,
            x = x,
            y = y,
            prefab = prefab,
            pool = pool,
            category = category,
            minCount = minCount,
            maxCount = maxCount,
            radius = radius,
            minDifficulty = minDifficulty,
            maxDifficulty = maxDifficulty,
            repeat = repeat,
            enabled = enabled,
            allowedSurfaceIds = allowedSurfaceIds != null ? (string[])allowedSurfaceIds.Clone() : Array.Empty<string>(),
            direction = direction,
            required = required,
            socketTag = socketTag
        };
    }
}

/// <summary>
/// One reusable room. Painting edits this asset. A placed copy reads it and keeps its own runtime state.
/// </summary>
[CreateAssetMenu(fileName = "Room", menuName = "Game/Levels/Room")]
public class RoomDefinition : ScriptableObject
{
    [SerializeField] string id = "room";
    [SerializeField] string displayName = "Комната";
    [SerializeField] int width = 12;
    [SerializeField] int height = 10;
    [SerializeField] RoomCategory category = RoomCategory.Clearing;
    [SerializeField] string biomeId = "forest";
    [SerializeField] TerrainDefinition defaultGround;
    [SerializeField] List<PaintedCell> cells = new List<PaintedCell>();
    [SerializeField] List<RoomMarkerData> markers = new List<RoomMarkerData>();

    Dictionary<long, TerrainDefinition> _lookup;
    bool _lookupReady;

    public string Id => id;
    public string DisplayName => displayName;
    public int Width => width;
    public int Height => height;
    public RoomCategory Category => category;
    public string BiomeId => biomeId;
    public TerrainDefinition DefaultGround => defaultGround;
    public IReadOnlyList<PaintedCell> Cells => cells;
    public IReadOnlyList<RoomMarkerData> Markers => markers;

    public void Configure(
        string roomId,
        string name,
        int roomWidth,
        int roomHeight,
        RoomCategory roomCategory,
        string biome,
        TerrainDefinition ground)
    {
        id = roomId;
        displayName = name;
        width = Mathf.Clamp(roomWidth, 1, 128);
        height = Mathf.Clamp(roomHeight, 1, 128);
        category = roomCategory;
        biomeId = biome;
        defaultGround = ground;
        Invalidate();
    }

    public void SetDefaultGround(TerrainDefinition ground)
    {
        defaultGround = ground;
        Invalidate();
    }

    public bool Inside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public TerrainDefinition Overlay(int x, int y, RoomLayer layer)
    {
        EnsureLookup();
        if (_lookup.TryGetValue(Key(x, y, (int)layer), out TerrainDefinition terrain))
            return terrain;

        return null;
    }

    public TerrainDefinition SurfaceAt(int x, int y)
    {
        TerrainDefinition overlay = Overlay(x, y, RoomLayer.Terrain);
        if (overlay != null)
            return overlay;

        TerrainDefinition ground = Overlay(x, y, RoomLayer.Ground);
        return ground != null ? ground : defaultGround;
    }

    public void SetTile(int x, int y, RoomLayer layer, TerrainDefinition terrain)
    {
        if (!Inside(x, y) || layer > RoomLayer.Terrain)
            return;

        if (layer == RoomLayer.Ground && terrain == defaultGround)
        {
            ClearTile(x, y, layer);
            return;
        }

        int layerIndex = (int)layer;
        for (int i = 0; i < cells.Count; i++)
        {
            PaintedCell cell = cells[i];
            if (cell.x == x && cell.y == y && cell.layer == layerIndex)
            {
                cell.terrain = terrain;
                Invalidate();
                return;
            }
        }

        cells.Add(new PaintedCell { x = x, y = y, layer = layerIndex, terrain = terrain });
        Invalidate();
    }

    public void ClearTile(int x, int y, RoomLayer layer)
    {
        int layerIndex = (int)layer;
        for (int i = cells.Count - 1; i >= 0; i--)
        {
            PaintedCell cell = cells[i];
            if (cell.x == x && cell.y == y && cell.layer == layerIndex)
                cells.RemoveAt(i);
        }

        Invalidate();
    }

    public RoomMarkerData AddMarker(RoomMarkerData marker)
    {
        if (marker == null)
            return null;

        if (string.IsNullOrEmpty(marker.id))
            marker.id = marker.kind + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        marker.x = Mathf.Clamp(marker.x, 0, width - 1);
        marker.y = Mathf.Clamp(marker.y, 0, height - 1);
        markers.Add(marker);
        return marker;
    }

    public RoomMarkerData FindMarker(string markerId)
    {
        if (string.IsNullOrEmpty(markerId))
            return null;

        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null && markers[i].id == markerId)
                return markers[i];
        }

        return null;
    }

    public bool MoveMarker(string markerId, int x, int y)
    {
        RoomMarkerData marker = FindMarker(markerId);
        if (marker == null || !Inside(x, y))
            return false;

        marker.x = x;
        marker.y = y;
        return true;
    }

    public RoomMarkerData DuplicateMarker(string markerId)
    {
        RoomMarkerData marker = FindMarker(markerId);
        if (marker == null)
            return null;

        RoomMarkerData copy = marker.Clone(marker.kind + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        copy.x = Mathf.Min(width - 1, marker.x + 1);
        markers.Add(copy);
        return copy;
    }

    public bool RemoveMarker(string markerId)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i] != null && markers[i].id == markerId)
            {
                markers.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void Collect(MarkerKind kind, List<RoomMarkerData> into)
    {
        if (into == null)
            return;

        into.Clear();
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null && markers[i].kind == kind)
                into.Add(markers[i]);
        }
    }

    public void ClearAuthored()
    {
        cells.Clear();
        markers.Clear();
        Invalidate();
    }

    void Invalidate()
    {
        _lookupReady = false;
        _lookup = null;
    }

    void EnsureLookup()
    {
        if (_lookupReady && _lookup != null)
            return;

        _lookup = new Dictionary<long, TerrainDefinition>();
        for (int i = 0; i < cells.Count; i++)
        {
            PaintedCell cell = cells[i];
            if (cell == null)
                continue;

            _lookup[Key(cell.x, cell.y, cell.layer)] = cell.terrain;
        }

        _lookupReady = true;
    }

    static long Key(int x, int y, int layer)
    {
        return ((long)x << 42) ^ ((long)y << 21) ^ (uint)layer;
    }
}
