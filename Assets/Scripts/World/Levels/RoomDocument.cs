using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// The scene side of one room asset. Edit mode shows the map. Play mode spawns from the pools.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("Game/Room Document")]
public class RoomDocument : MonoBehaviour
{
    static readonly List<RoomDocument> Open = new List<RoomDocument>();

    [SerializeField] RoomDefinition definition;
    [SerializeField] LevelDefinition level;
    [SerializeField] bool playThisRoom = true;
    [SerializeField] bool suppressProceduralWorld;
    [SerializeField] bool drawDebug = true;
    [SerializeField] int seed = 1;
    [SerializeField] PlayerController player;

    [System.NonSerialized] bool _rebuilding;
    [System.NonSerialized] bool _spawned;
    TerrainFieldHost _host;
    PlayArea _area;

    public RoomDefinition Definition => definition;
    public LevelDefinition Level => level;
    public bool IsRebuilding => _rebuilding;
    public bool SuppressProceduralWorld => suppressProceduralWorld;
    public bool DrawDebug => drawDebug;
    public int Seed => seed;
    public TerrainField Field => _host != null ? _host.Field : null;
    public PlayArea Area => _area;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOpen()
    {
        Open.Clear();
    }

    public static RoomDocument Find(RoomDefinition room)
    {
        for (int i = 0; i < Open.Count; i++)
        {
            RoomDocument document = Open[i];
            if (document != null && document.definition == room)
                return document;
        }

        return null;
    }

    public void Assign(RoomDefinition room, LevelDefinition source)
    {
        definition = room;
        level = source;
        Rebuild();
    }

    public void SetSeed(int value)
    {
        seed = value;
    }

    public void SetSuppressProceduralWorld(bool value)
    {
        suppressProceduralWorld = value;
    }

    void OnEnable()
    {
        if (!Open.Contains(this))
            Open.Add(this);

        TerrainNavigation.Install();
        RebindField();
        if (transform.Find("Map") == null && definition != null)
            Rebuild();
    }

    void OnDisable()
    {
        Open.Remove(this);
    }

    void Start()
    {
        if (!Application.isPlaying || !playThisRoom || definition == null)
            return;

        EnsurePlayArea();
        PlacePlayer();
        RunSpawns(false, null);
    }

    public void Rebuild()
    {
        if (definition == null)
            return;

        _rebuilding = true;
        ClearChild("Markers");
        Grid grid = RoomMapBuilder.EnsureGrid(transform);
        Tilemap ground = RoomMapBuilder.Layer(grid.transform, "Ground", RoomMapBuilder.GroundOrder);
        Tilemap details = RoomMapBuilder.Layer(grid.transform, "Details", RoomMapBuilder.DetailOrder);
        Tilemap terrain = RoomMapBuilder.Layer(grid.transform, "Terrain", RoomMapBuilder.TerrainOrder);
        ground.ClearAllTiles();
        details.ClearAllTiles();
        terrain.ClearAllTiles();
        RoomMapBuilder.PaintRoom(definition, Vector2Int.zero, ground, details, terrain);
        BuildMarkers();
        RebindField();
        _rebuilding = false;
    }

    public void Repaint()
    {
        if (definition == null)
            return;

        Transform map = transform.Find("Map");
        if (map == null)
        {
            Rebuild();
            return;
        }

        Tilemap ground = RoomMapBuilder.Layer(map, "Ground", RoomMapBuilder.GroundOrder);
        Tilemap details = RoomMapBuilder.Layer(map, "Details", RoomMapBuilder.DetailOrder);
        Tilemap terrain = RoomMapBuilder.Layer(map, "Terrain", RoomMapBuilder.TerrainOrder);
        ground.ClearAllTiles();
        details.ClearAllTiles();
        terrain.ClearAllTiles();
        RoomMapBuilder.PaintRoom(definition, Vector2Int.zero, ground, details, terrain);
        RebindField();
        SyncMarkers();
    }

    public void RebindField()
    {
        if (definition == null)
            return;

        if (_host == null)
        {
            _host = GetComponent<TerrainFieldHost>();
            if (_host == null)
                _host = gameObject.AddComponent<TerrainFieldHost>();
        }

        _host.Bind(TerrainField.FromRoom(definition, Vector2Int.zero));
    }

    public string LocationId()
    {
        if (level != null && !string.IsNullOrEmpty(level.Id))
            return level.Id;

        return definition != null ? definition.BiomeId : null;
    }

    public void RunSpawns(bool force, int? difficulty)
    {
        if (definition == null || Field == null)
            return;

        if (_spawned && !force)
            return;

        int levelDifficulty = difficulty ?? ProgressionService.EffectiveDifficulty(LocationId());
        if (Application.isPlaying)
            ProgressionService.Run.LocationId = LocationId();

        var markers = new List<RoomMarkerData>();
        definition.Collect(MarkerKind.Spawn, markers);
        Transform props = MarkerRoot();
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            int pointSeed = seed ^ StableHash(marker.id);
            SpawnDirector.Spawn(marker, levelDifficulty, pointSeed, Field, props, _area, force);
        }

        SpawnProps(props);
        _spawned = true;
    }

    public void PlacePlayer()
    {
        if (player == null && GameRoot.Active != null)
            player = GameRoot.Active.Player;

        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (player == null || Field == null)
            return;

        Vector2 spot = FindStand();
        if (_area != null)
            player.SetPlayArea(_area);

        player.Place(spot);
    }

    public bool HasOther(RoomMarker marker)
    {
        if (marker == null || string.IsNullOrEmpty(marker.MarkerId))
            return false;

        Transform root = transform.Find("Markers");
        if (root == null)
            return false;

        for (int i = 0; i < root.childCount; i++)
        {
            RoomMarker other = root.GetChild(i).GetComponent<RoomMarker>();
            if (other != null && other != marker && other.MarkerId == marker.MarkerId)
                return true;
        }

        return false;
    }

    public void CommitMarkerCell(string markerId, int x, int y)
    {
        if (_rebuilding || definition == null)
            return;

        definition.MoveMarker(markerId, x, y);
        RebindField();
    }

    public void CommitMarkerDeleted(string markerId)
    {
        if (_rebuilding || definition == null)
            return;

        definition.RemoveMarker(markerId);
    }

    public void AdoptCopy(RoomMarker copy)
    {
        if (_rebuilding || definition == null || copy == null)
            return;

        RoomMarkerData source = definition.FindMarker(copy.MarkerId);
        if (source == null)
            return;

        RoomMarkerData clone = definition.DuplicateMarker(source.id);
        if (clone == null)
            return;

        Vector2Int cell = TerrainCoordinates.WorldToCell(copy.transform.position);
        definition.MoveMarker(clone.id, cell.x, cell.y);
        copy.Bind(this, clone.id);
        Snap(copy, clone.x, clone.y);
    }

    void SpawnProps(Transform parent)
    {
        IReadOnlyList<RoomMarkerData> markers = definition.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null || marker.prefab == null)
                continue;

            if (marker.kind != MarkerKind.Object && marker.kind != MarkerKind.Decor && marker.kind != MarkerKind.Special)
                continue;

            Vector2 spot = TerrainCoordinates.CellCenterWorld(marker.x, marker.y);
            SpawnDirector.SpawnPrefab(marker.prefab, spot, parent, _area);
        }
    }

    void BuildMarkers()
    {
        Transform root = MarkerRoot();
        IReadOnlyList<RoomMarkerData> markers = definition.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null)
                continue;

            var go = new GameObject(string.IsNullOrEmpty(marker.id) ? marker.kind.ToString() : marker.id);
            go.transform.SetParent(root, false);
            Snap(go.transform, marker.x, marker.y);
            RoomMarker handle = go.AddComponent<RoomMarker>();
            handle.Bind(this, marker.id);
        }
    }

    void SyncMarkers()
    {
        Transform root = MarkerRoot();
        var seen = new HashSet<string>();
        IReadOnlyList<RoomMarkerData> markers = definition.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null || string.IsNullOrEmpty(marker.id))
                continue;

            seen.Add(marker.id);
            RoomMarker handle = FindHandle(root, marker.id);
            if (handle == null)
            {
                var go = new GameObject(marker.id);
                go.transform.SetParent(root, false);
                handle = go.AddComponent<RoomMarker>();
                handle.Bind(this, marker.id);
            }

            Snap(handle.transform, marker.x, marker.y);
        }
    }

    static RoomMarker FindHandle(Transform root, string id)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            RoomMarker handle = root.GetChild(i).GetComponent<RoomMarker>();
            if (handle != null && handle.MarkerId == id)
                return handle;
        }

        return null;
    }

    Transform MarkerRoot()
    {
        Transform root = transform.Find("Markers");
        if (root != null)
            return root;

        var go = new GameObject("Markers");
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    void EnsurePlayArea()
    {
        if (definition == null)
            return;

        Transform existing = transform.Find("Play Area");
        GameObject host = existing != null ? existing.gameObject : new GameObject("Play Area");
        if (existing == null)
            host.transform.SetParent(transform, false);

        SpriteRenderer renderer = host.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = host.AddComponent<SpriteRenderer>();

        renderer.sprite = TerrainFallbackSprites.Get(Color.white, "play-area");
        renderer.enabled = true;
        Bounds bounds = RoomBounds(Vector2Int.zero, definition);
        host.transform.position = bounds.center;
        Vector3 spriteSize = renderer.sprite.bounds.size;
        host.transform.localScale = new Vector3(
            bounds.size.x / Mathf.Max(0.01f, spriteSize.x),
            bounds.size.y / Mathf.Max(0.01f, spriteSize.y),
            1f);
        _area = host.GetComponent<PlayArea>();
        if (_area == null)
            _area = host.AddComponent<PlayArea>();
        _area.CacheBounds();
        renderer.enabled = false;

        LocationContext context = GetComponent<LocationContext>();
        if (context == null)
            context = gameObject.AddComponent<LocationContext>();
        context.Configure(RegionKind.Wilderness, LocationKind.Outdoor, _area);
        context.SetDropParent(transform);
        LocationContext.Use(context);
    }

    Vector2 FindStand()
    {
        for (int y = 0; y < definition.Height; y++)
        {
            for (int x = 0; x < definition.Width; x++)
            {
                TerrainDefinition surface = definition.SurfaceAt(x, y);
                if (surface == null || surface.Walkable)
                    return TerrainCoordinates.CellCenterWorld(x, y);
            }
        }

        return TerrainCoordinates.CellCenterWorld(0, 0);
    }

    public static Bounds RoomBounds(Vector2Int origin, RoomDefinition room)
    {
        Vector3 south = TerrainCoordinates.CornerWorld(origin.x, origin.y);
        Vector3 east = TerrainCoordinates.CornerWorld(origin.x + room.Width, origin.y);
        Vector3 north = TerrainCoordinates.CornerWorld(origin.x + room.Width, origin.y + room.Height);
        Vector3 west = TerrainCoordinates.CornerWorld(origin.x, origin.y + room.Height);
        var bounds = new Bounds(south, Vector3.zero);
        bounds.Encapsulate(east);
        bounds.Encapsulate(north);
        bounds.Encapsulate(west);
        if (bounds.size.y < 0.2f)
            bounds.Expand(new Vector3(0f, 0.2f, 0f));
        return bounds;
    }

    static void Snap(Transform target, int x, int y)
    {
        if (target == null)
            return;

        Vector2 center = TerrainCoordinates.CellCenterWorld(x, y);
        target.position = new Vector3(center.x, center.y, 0f);
    }

    static void Snap(RoomMarker marker, int x, int y)
    {
        if (marker != null)
            Snap(marker.transform, x, y);
    }

    void ClearChild(string childName)
    {
        Transform child = transform.Find(childName);
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

    void OnDrawGizmosSelected()
    {
        if (!drawDebug || definition == null)
            return;

        var corners = new Vector3[4];
        for (int y = 0; y < definition.Height; y++)
        {
            for (int x = 0; x < definition.Width; x++)
            {
                TerrainDefinition surface = definition.SurfaceAt(x, y);
                if (surface == null || surface.Walkable)
                    continue;

                TerrainCoordinates.WriteDiamond(x, y, corners);
                Gizmos.color = new Color(0.8f, 0.15f, 0.1f, 0.35f);
                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);
            }
        }

        IReadOnlyList<RoomMarkerData> markers = definition.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null)
                continue;

            Vector3 at = TerrainCoordinates.CellCenterWorld(marker.x, marker.y);
            if (marker.kind == MarkerKind.Spawn)
            {
                Gizmos.color = new Color(0.9f, 0.35f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(at, Mathf.Max(0.15f, marker.radius));
            }
            else if (marker.kind == MarkerKind.Socket)
            {
                Gizmos.color = marker.required ? Color.yellow : Color.cyan;
                Vector2 step = IsoSides.Step(marker.direction);
                Vector3 end = at + new Vector3(step.x, step.y * 0.5f, 0f);
                Gizmos.DrawLine(at, end);
            }
        }
    }
}
