using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a whole location from a level asset, either from a saved layout or from a seed.
/// </summary>
[AddComponentMenu("Game/Level Document")]
public class LevelDocument : MonoBehaviour
{
    [SerializeField] LevelDefinition level;
    [SerializeField] LevelLayoutDefinition layout;
    [SerializeField] int seed = 1;
    [SerializeField] bool generateOnPlay = true;
    [SerializeField] bool suppressProceduralWorld = true;
    [SerializeField] PlayerController player;

    public LevelDefinition Level => level;
    public LevelLayoutDefinition Layout => layout;
    public int Seed => seed;
    public bool SuppressProceduralWorld => suppressProceduralWorld;
    public LayoutResult Last { get; private set; }

    public void Assign(LevelDefinition source, LevelLayoutDefinition authored, int layoutSeed)
    {
        level = source;
        layout = authored;
        seed = layoutSeed;
    }

    void Start()
    {
        if (Application.isPlaying && generateOnPlay)
            BuildNow(true);
    }

    public LayoutResult BuildNow(bool spawn)
    {
        ClearGenerated();
        Last = layout != null && layout.Placements.Count > 0
            ? RoomLayoutBuilder.FromPlacements(Copy(layout), layout.Seed)
            : RoomLayoutBuilder.Build(level, seed);

        if (Last == null)
            return null;

        var root = new GameObject("Generated");
        root.transform.SetParent(transform, false);
        Grid grid = RoomMapBuilder.EnsureGrid(root.transform);
        Tilemap ground = RoomMapBuilder.Layer(grid.transform, "Ground", RoomMapBuilder.GroundOrder);
        Tilemap details = RoomMapBuilder.Layer(grid.transform, "Details", RoomMapBuilder.DetailOrder);
        Tilemap terrain = RoomMapBuilder.Layer(grid.transform, "Terrain", RoomMapBuilder.TerrainOrder);

        int difficulty = ProgressionService.EffectiveDifficulty(level != null ? level.Id : null);
        if (Application.isPlaying && level != null)
            ProgressionService.Run.LocationId = level.Id;

        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool any = false;
        for (int i = 0; i < Last.Nodes.Count; i++)
        {
            LayoutNode node = Last.Nodes[i];
            if (node.Room == null)
                continue;

            RoomMapBuilder.PaintRoom(node.Room, node.Origin, ground, details, terrain);
            var roomObject = new GameObject(node.Room.DisplayName);
            roomObject.transform.SetParent(root.transform, false);
            TerrainFieldHost host = roomObject.AddComponent<TerrainFieldHost>();
            TerrainField field = TerrainField.FromRoom(node.Room, node.Origin);
            host.Bind(field);

            if (spawn && Application.isPlaying)
            {
                var markers = new List<RoomMarkerData>();
                node.Room.Collect(MarkerKind.Spawn, markers);
                for (int m = 0; m < markers.Count; m++)
                {
                    RoomMarkerData marker = Shift(markers[m], node.Origin);
                    int pointSeed = seed ^ (i * 397) ^ StableHash(marker.id);
                    SpawnDirector.Spawn(marker, difficulty, pointSeed, field, roomObject.transform, null, false);
                }

                SpawnProps(node, roomObject.transform);
            }

            Bounds roomBounds = RoomDocument.RoomBounds(node.Origin, node.Room);
            if (!any)
            {
                bounds = roomBounds;
                any = true;
            }
            else
                bounds.Encapsulate(roomBounds);
        }

        if (spawn && Application.isPlaying)
            PlacePlayer(bounds);

        for (int i = 0; i < Last.Problems.Count; i++)
            Debug.LogWarning(Last.Problems[i]);

        return Last;
    }

    static void SpawnProps(LayoutNode node, Transform parent)
    {
        IReadOnlyList<RoomMarkerData> markers = node.Room.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null || marker.prefab == null)
                continue;

            if (marker.kind != MarkerKind.Object && marker.kind != MarkerKind.Decor && marker.kind != MarkerKind.Special)
                continue;

            Vector2 spot = TerrainCoordinates.CellCenterWorld(node.Origin.x + marker.x, node.Origin.y + marker.y);
            SpawnDirector.SpawnPrefab(marker.prefab, spot, parent, null);
        }
    }

    void PlacePlayer(Bounds bounds)
    {
        if (player == null && GameRoot.Active != null)
            player = GameRoot.Active.Player;
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
        if (player == null || Last == null)
            return;

        for (int i = 0; i < Last.Nodes.Count; i++)
        {
            LayoutNode node = Last.Nodes[i];
            if (node.Room == null)
                continue;

            for (int y = 0; y < node.Room.Height; y++)
            {
                for (int x = 0; x < node.Room.Width; x++)
                {
                    TerrainDefinition surface = node.Room.SurfaceAt(x, y);
                    if (surface != null && !surface.Walkable)
                        continue;

                    player.Place(TerrainCoordinates.CellCenterWorld(node.Origin.x + x, node.Origin.y + y));
                    return;
                }
            }
        }

        player.Place(bounds.center);
    }

    static RoomMarkerData Shift(RoomMarkerData marker, Vector2Int origin)
    {
        RoomMarkerData copy = marker.Clone(marker.id);
        copy.x += origin.x;
        copy.y += origin.y;
        return copy;
    }

    static List<LayoutPlacement> Copy(LevelLayoutDefinition source)
    {
        var list = new List<LayoutPlacement>();
        IReadOnlyList<LayoutPlacement> placements = source.Placements;
        for (int i = 0; i < placements.Count; i++)
            list.Add(placements[i]);
        return list;
    }

    void ClearGenerated()
    {
        Transform child = transform.Find("Generated");
        if (child == null)
            return;

        if (Application.isPlaying)
            Destroy(child.gameObject);
        else
            DestroyImmediate(child.gameObject);
    }

    static int StableHash(string value)
    {
        int hash = 23;
        if (string.IsNullOrEmpty(value))
            return hash;

        for (int i = 0; i < value.Length; i++)
            hash = unchecked(hash * 31 + value[i]);
        return hash;
    }
}
