using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelSide
{
    North,
    East,
    South,
    West
}

public enum WorldPlace
{
    Home,
    LevelOne
}

/// <summary>
/// Home is the grass cross, each room a different cut of the meadow sheet.
/// Level 1 keeps the original flat grass and grows a forest on it: trees in pairs,
/// rocks on their own, a random start, and one gold circle that leads home.
/// Walking into a colored edge enters the next room from the opposite side.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LevelWorld : MonoBehaviour
{
    public sealed class Room
    {
        public string Name;
        public Vector2Int Grid;
        public Color Tint;
        public Bounds Bounds;
        public PlayArea Area;
        public GameObject Root;
        public Gate[] Gates;
        public Gate Exit;
        public bool Visited;
        public bool IsExit;
        public readonly List<MapInterest> Interests = new List<MapInterest>();
    }

    public sealed class Gate
    {
        public LevelSide Side;
        public Rect Rect;
        public Room Destination;
        public LevelSide ArrivalSide;
        public bool ReturnsHome;
        public SpriteRenderer Fill;
        public Color Color;
    }

    const float RoomGap = 40f;
    const float GateEdgeFraction = 0.24f;
    const float SpawnClearance = 2.2f;
    const float TransitionCooldown = 0.12f;
    const float ExitTrigger = 4.8f;

    static readonly Color GateColor = new Color(0.22f, 0.55f, 1f, 0.78f);
    static readonly Color GateBorder = new Color(0.06f, 0.20f, 0.58f, 1f);
    static readonly Color ForestGate = new Color(0.72f, 0.58f, 0.28f, 0.88f);
    static readonly Color ForestGateBorder = new Color(0.36f, 0.24f, 0.08f, 1f);
    static readonly Color ExitCore = new Color(1f, 0.86f, 0.34f, 0.92f);
    static readonly Color ExitGlow = new Color(0.95f, 0.62f, 0.16f, 0.34f);
    static readonly Color ForestVoid = new Color(0.04f, 0.08f, 0.045f, 1f);
    static readonly Color GrassMap = new Color(0.30f, 0.46f, 0.26f, 1f);
    static readonly Color ForestMap = new Color(0.22f, 0.40f, 0.20f, 1f);
    static readonly Color ExitMap = new Color(0.90f, 0.74f, 0.28f, 1f);

    static readonly Color[] ForestTints =
    {
        new Color(0.94f, 1f, 0.92f),
        new Color(1f, 0.98f, 0.90f),
        new Color(0.90f, 0.98f, 0.92f),
        new Color(0.96f, 1f, 0.94f)
    };

    public static LevelWorld Instance { get; private set; }

    public Room Current { get; private set; }
    public bool Suspended => _suspended;
    public Vector2 SuspendedFeet => _suspendedFeet;
    public Transform HomeAnchor => _homeCenter != null && _homeCenter.Root != null ? _homeCenter.Root.transform : transform;

    public WorldPlace Place => _place;
    public string PlaceName => _place == WorldPlace.Home ? "Дом" : "Уровень 1";
    public string PlaceNameEnglish => _place == WorldPlace.Home ? "Home" : "Level 1";
    public bool FogOfWar => _place == WorldPlace.LevelOne;
    public Vector2Int GridMin { get; private set; }
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public IReadOnlyList<Room> Rooms => Active;

    public event Action LayoutChanged;
    public int InterestVersion { get; private set; }

    public Room RoomContaining(Vector2 point)
    {
        List<Room> rooms = Active;
        for (int i = 0; i < rooms.Count; i++)
        {
            Bounds bounds = rooms[i].Bounds;
            if (bounds.size.x <= 0.01f || bounds.size.y <= 0.01f)
                continue;

            Vector3 probe = new Vector3(point.x, point.y, bounds.center.z);
            if (bounds.Contains(probe))
                return rooms[i];
        }

        return null;
    }

    public void NoteInterest(Vector2 point, MapPinKind kind)
    {
        Room room = RoomContaining(point);
        if (room == null)
            return;

        for (int i = 0; i < room.Interests.Count; i++)
        {
            MapInterest sitting = room.Interests[i];
            if (sitting.Kind == kind && (sitting.Point - point).sqrMagnitude < 0.16f)
                return;
        }

        room.Interests.Add(new MapInterest { Point = point, Kind = kind });
        InterestVersion++;
    }

    readonly List<Room> _home = new List<Room>();
    readonly List<Room> _dungeon = new List<Room>();
    List<Room> Active => _place == WorldPlace.LevelOne ? _dungeon : _home;

    PlayerController _player;
    GameObject _template;
    GameObject _dungeonRoot;
    Room _homeCenter;
    WorldPlace _place = WorldPlace.Home;
    Vector2 _previousFeet;
    float _cooldown;
    Sprite _quad;
    Sprite _disc;
    Camera _camera;
    Color _homeBackground;
    bool _backgroundStored;
    bool _suspended;
    Vector2 _suspendedFeet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FindAnyObjectByType<LevelWorld>() != null)
            return;

        if (FindAnyObjectByType<PlayArea>() == null || FindAnyObjectByType<PlayerController>() == null)
            return;

        if (AuthoredLevelOwnsTheScene())
            return;

        var host = new GameObject("Level World");
        host.AddComponent<LevelWorld>();
    }

    static bool AuthoredLevelOwnsTheScene()
    {
        RoomDocument[] rooms = UnityEngine.Object.FindObjectsByType<RoomDocument>();
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null && rooms[i].SuppressProceduralWorld)
                return true;
        }

        LevelDocument[] levels = UnityEngine.Object.FindObjectsByType<LevelDocument>();
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].SuppressProceduralWorld)
                return true;
        }

        return false;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_suspended || Current == null)
            return;

        float wave = 0.72f + 0.28f * Mathf.Sin(Time.time * 2.8f);
        Pulse(Current.Exit, wave);
        Gate[] gates = Current.Gates;
        for (int i = 0; i < gates.Length; i++)
            Pulse(gates[i], wave);
    }

    void LateUpdate()
    {
        if (_suspended || _player == null || Current == null)
            return;

        Vector2 feet = _player.transform.position;
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
            _previousFeet = feet;
            return;
        }

        if (Current.Exit != null && Current.Exit.ReturnsHome && Crossed(Current.Exit.Rect, _previousFeet, feet))
        {
            _previousFeet = feet;
            TravelHome();
            return;
        }

        Gate[] gates = Current.Gates;
        for (int i = 0; i < gates.Length; i++)
        {
            Gate gate = gates[i];
            if (gate == null || gate.Destination == null)
                continue;

            if (!Crossed(gate.Rect, _previousFeet, feet))
                continue;

            Enter(gate.Destination, gate.ArrivalSide);
            _previousFeet = _player.transform.position;
            return;
        }

        _previousFeet = feet;
    }

    public Room RoomAt(Vector2Int grid)
    {
        List<Room> rooms = Active;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Grid == grid)
                return rooms[i];
        }

        return null;
    }

    public Color MiniColor(Room room, bool current)
    {
        Color baseColor = _place == WorldPlace.Home ? GrassMap : ForestMap;
        var mixed = new Color(baseColor.r * room.Tint.r, baseColor.g * room.Tint.g, baseColor.b * room.Tint.b, 1f);
        if (room.IsExit)
            mixed = Color.Lerp(mixed, ExitMap, 0.7f);

        return current ? Color.Lerp(mixed, Color.white, 0.42f) : Color.Lerp(mixed, Color.black, 0.28f);
    }

    public void TravelHome()
    {
        if (LocationFlow.IsInside)
        {
            LocationFlow.Exit();
            return;
        }

        if (LocationFlow.IsBusy || _place == WorldPlace.Home)
            return;

        ClearDungeon();
        _place = WorldPlace.Home;
        Measure(_home);
        Present(_homeCenter, _homeCenter.Bounds.center, true);
        LayoutChanged?.Invoke();
    }

    public void SuspendForInterior(Vector2 outdoorFeet)
    {
        _suspended = true;
        _suspendedFeet = outdoorFeet;
        SetShown(_home, false, null);
        SetShown(_dungeon, false, null);
    }

    public void ResumeFromInterior(Vector2 point)
    {
        _suspended = false;
        if (_homeCenter == null || _player == null)
            return;

        if (_place != WorldPlace.Home)
        {
            ClearDungeon();
            _place = WorldPlace.Home;
            Measure(_home);
        }

        Present(_homeCenter, point, true);
        LayoutChanged?.Invoke();
    }

    public void TravelLevelOne()
    {
        if (LocationFlow.IsBusy)
            return;

        if (LocationFlow.IsInside)
        {
            LocationFlow.Exit(TravelLevelOne);
            return;
        }

        ClearDungeon();
        _place = WorldPlace.LevelOne;
        BuildDungeon();
        Measure(_dungeon);

        Room start = _dungeon[UnityEngine.Random.Range(0, _dungeon.Count)];
        Vector2 spawn = ArrivalPoint(start);
        ScatterForest(start, spawn);
        SpawnSlimes(start, spawn);
        Present(start, spawn, true);
        LayoutChanged?.Invoke();
    }

    void Build()
    {
        PlayArea centerArea = FindAnyObjectByType<PlayArea>();
        _player = FindAnyObjectByType<PlayerController>();
        if (centerArea == null || _player == null)
        {
            enabled = false;
            return;
        }

        _template = Instantiate(centerArea.gameObject);
        _template.name = "Room Template";
        _template.SetActive(false);
        _template.transform.SetParent(transform, false);

        _homeCenter = Register(centerArea.gameObject, Vector2Int.zero, "Home Center", Color.white, _home, transform);
        Room north = CreateHomeRoom(_homeCenter, Vector2Int.up, "Home North", new Color(0.82f, 0.90f, 1f));
        Room south = CreateHomeRoom(_homeCenter, Vector2Int.down, "Home South", new Color(1f, 0.90f, 0.78f));
        Room east = CreateHomeRoom(_homeCenter, Vector2Int.right, "Home East", new Color(0.84f, 1f, 0.84f));
        Room west = CreateHomeRoom(_homeCenter, Vector2Int.left, "Home West", new Color(0.93f, 0.84f, 1f));

        ApplyHomeFloors();

        Link(_homeCenter, north, GateColor, GateBorder);
        Link(_homeCenter, south, GateColor, GateBorder);
        Link(_homeCenter, east, GateColor, GateBorder);
        Link(_homeCenter, west, GateColor, GateBorder);
        HomeHouse.Place(_homeCenter);

        _place = WorldPlace.Home;
        Measure(_home);
        Present(_homeCenter, _player.transform.position, false);

        var minimap = gameObject.AddComponent<LevelMinimap>();
        minimap.Initialize(this, _player);
        var menu = gameObject.AddComponent<WorldMenu>();
        menu.Initialize(this);
    }

    void ApplyHomeFloors()
    {
        for (int i = 0; i < _home.Count; i++)
        {
            Room room = _home[i];
            var renderer = room.Root.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                continue;

            int seed = unchecked((room.Grid.x * 92837111) ^ (room.Grid.y * 68928749)) & 0x7fffffff;
            Sprite meadow = HomeFloor.Build(renderer.sprite, seed == 0 ? 1 : seed);
            if (meadow == null)
                continue;

            renderer.sprite = meadow;
            renderer.color = Color.white;
            renderer.sortingOrder = IsoSort.FloorOrder;
            if (room.Area != null)
                room.Area.CacheBounds();
            room.Bounds = room.Area != null ? room.Area.CachedBounds : renderer.bounds;
        }
    }

    void ScatterForest(Room start, Vector2 spawn)
    {
        for (int i = 0; i < _dungeon.Count; i++)
            ForestScenery.Place(_dungeon[i], _dungeon[i] == start, spawn);
    }

    void BuildDungeon()
    {
        _dungeonRoot = new GameObject("Level 1");
        _dungeonRoot.transform.SetParent(transform, false);

        int size = UnityEngine.Random.Range(3, 5);
        var grid = new Room[size, size];
        Vector3 origin = new Vector3(0f, 5000f, 0f);
        float stepX = _homeCenter.Bounds.size.x + RoomGap;
        float stepY = _homeCenter.Bounds.size.y + RoomGap;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 position = origin + new Vector3(x * stepX, y * stepY, 0f);
                Color tint = ForestTints[(x + y * 3) % ForestTints.Length];
                grid[x, y] = CreateDungeonRoom(new Vector2Int(x, y), position, tint);
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x + 1 < size)
                    Link(grid[x, y], grid[x + 1, y], ForestGate, ForestGateBorder);
                if (y + 1 < size)
                    Link(grid[x, y], grid[x, y + 1], ForestGate, ForestGateBorder);
            }
        }

        Room exit = grid[UnityEngine.Random.Range(0, size), UnityEngine.Random.Range(0, size)];
        exit.IsExit = true;
        AddExit(exit);
    }

    void SpawnSlimes(Room start, Vector2 startPoint)
    {
        for (int i = 0; i < _dungeon.Count; i++)
        {
            Room room = _dungeon[i];
            // Inclusive 0 through 4, rolled on its own for this room.
            int count = UnityEngine.Random.Range(0, 5);
            for (int s = 0; s < count; s++)
            {
                Vector2 point = SlimePoint(room, start, startPoint);
                Slime.Spawn(point, room.Area, room.Root.transform);
            }
        }
    }

    static Vector2 SlimePoint(Room room, Room start, Vector2 startPoint)
    {
        Bounds bounds = room.Bounds;
        const float edge = 8f;
        float minX = bounds.min.x + edge;
        float maxX = bounds.max.x - edge;
        float minY = bounds.min.y + edge;
        float maxY = bounds.max.y - edge;
        if (minX > maxX)
            minX = maxX = bounds.center.x;
        if (minY > maxY)
            minY = maxY = bounds.center.y;

        Vector2 portal = bounds.center;
        Vector2 picked = portal;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            picked = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
            if (room.Area != null)
                picked = room.Area.Clamp(picked, 2f);

            if (room.IsExit && (picked - portal).sqrMagnitude < 36f)
                continue;
            if (room == start && (picked - startPoint).sqrMagnitude < 64f)
                continue;

            return picked;
        }

        return picked;
    }

    void ClearDungeon()
    {
        if (Current != null && _dungeon.Contains(Current))
            Current = null;

        WorldPresentation.ClearTransient();
        if (_dungeonRoot != null)
            DestroyImmediate(_dungeonRoot);

        _dungeonRoot = null;
        _dungeon.Clear();
    }

    Room CreateHomeRoom(Room origin, Vector2Int grid, string name, Color tint)
    {
        Vector2Int delta = grid - origin.Grid;
        Vector3 position = origin.Root.transform.position + new Vector3(
            delta.x * (origin.Bounds.size.x + RoomGap),
            delta.y * (origin.Bounds.size.y + RoomGap),
            0f);

        GameObject copy = Instantiate(origin.Root, position, Quaternion.identity, transform);
        var renderer = copy.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = tint;

        return Register(copy, grid, name, tint, _home, transform);
    }

    Room CreateDungeonRoom(Vector2Int grid, Vector3 position, Color tint)
    {
        GameObject copy = Instantiate(_template, position, Quaternion.identity, _dungeonRoot.transform);
        copy.SetActive(true);
        copy.transform.localScale = Vector3.one;
        copy.transform.position = position;

        var renderer = copy.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.white;
            renderer.sortingOrder = IsoSort.FloorOrder;
        }

        return Register(copy, grid, "Level 1 " + grid.x + "," + grid.y, tint, _dungeon, _dungeonRoot.transform);
    }

    Room Register(GameObject root, Vector2Int grid, string name, Color tint, List<Room> list, Transform parent)
    {
        root.name = name;
        root.transform.SetParent(parent, true);

        var area = root.GetComponent<PlayArea>();
        if (area != null)
            area.CacheBounds();

        var room = new Room
        {
            Name = name,
            Grid = grid,
            Tint = tint,
            Bounds = area != null ? area.CachedBounds : new Bounds(root.transform.position, Vector3.one),
            Area = area,
            Root = root,
            Gates = new Gate[4]
        };

        list.Add(room);
        LocationContext context = root.GetComponent<LocationContext>();
        if (context == null)
            context = root.AddComponent<LocationContext>();
        RegionKind region = list == _home ? RegionKind.Home : RegionKind.Wilderness;
        context.Configure(region, LocationKind.Outdoor, area);
        return room;
    }

    void Link(Room from, Room to, Color fill, Color border)
    {
        LevelSide side = SideBetween(from.Grid, to.Grid);
        AddGate(from, side, to, fill, border);
        AddGate(to, Opposite(side), from, fill, border);
    }

    void AddGate(Room room, LevelSide side, Room destination, Color fill, Color border)
    {
        Vector2 size = GateSize(room.Bounds, side, _player.BodyInset());
        Vector2 center = GateCenter(room.Bounds, side, size);

        var root = new GameObject("Gate " + side);
        root.transform.SetParent(room.Root.transform, false);

        CreateQuad(root.transform, "Border", center, size + new Vector2(0.45f, 0.45f), border, IsoSort.FloorOrder + 1);
        SpriteRenderer fillRenderer = CreateQuad(root.transform, "Fill", center, size, fill, IsoSort.FloorOrder + 2);

        room.Gates[(int)side] = new Gate
        {
            Side = side,
            Rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y),
            Destination = destination,
            ArrivalSide = Opposite(side),
            Fill = fillRenderer,
            Color = fill
        };
    }

    void AddExit(Room room)
    {
        Vector2 center = room.Bounds.center;
        var root = new GameObject("Exit Home");
        root.transform.SetParent(room.Root.transform, false);

        CreateDisc(root.transform, "Glow", center, 8.6f, ExitGlow, IsoSort.FloorOrder + 3);
        SpriteRenderer core = CreateDisc(root.transform, "Core", center, ExitTrigger, ExitCore, IsoSort.FloorOrder + 4);

        room.Exit = new Gate
        {
            Rect = new Rect(center.x - ExitTrigger * 0.5f, center.y - ExitTrigger * 0.5f, ExitTrigger, ExitTrigger),
            ReturnsHome = true,
            Fill = core,
            Color = ExitCore
        };
    }

    void Enter(Room room, LevelSide arrivalSide)
    {
        Present(room, SpawnPoint(room, arrivalSide), true);
    }

    void Present(Room room, Vector2 point, bool movePlayer)
    {
        room.Visited = true;
        Show(room);
        LocationContext context = room.Root != null ? room.Root.GetComponent<LocationContext>() : null;
        LocationContext.Use(context);
        _player.SetPlayArea(room.Area);
        if (movePlayer)
            _player.Place(point);

        Current = room;
        _cooldown = movePlayer ? TransitionCooldown : 0f;
        _previousFeet = _player.transform.position;
        WorldPresentation.ClearTransient();
        ApplyAtmosphere();
    }

    void Show(Room room)
    {
        bool dungeon = _place == WorldPlace.LevelOne;
        SetShown(_home, !dungeon, room);
        SetShown(_dungeon, dungeon, room);
    }

    static void SetShown(List<Room> rooms, bool worldOn, Room current)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            GameObject root = rooms[i].Root;
            if (root != null)
                root.SetActive(worldOn && rooms[i] == current);
        }
    }

    Vector2 ArrivalPoint(Room room)
    {
        Vector2 center = room.Bounds.center;
        if (!room.IsExit)
            return center;

        float y = Mathf.Max(room.Bounds.min.y + 8f, center.y - 12f);
        return new Vector2(center.x, y);
    }

    Vector2 SpawnPoint(Room room, LevelSide arrivalSide)
    {
        Gate gate = room.Gates[(int)arrivalSide];
        if (gate == null)
            return room.Bounds.center;

        Rect rect = gate.Rect;
        Vector2 center = rect.center;
        switch (arrivalSide)
        {
            case LevelSide.North:
                return new Vector2(center.x, rect.yMin - SpawnClearance);
            case LevelSide.South:
                return new Vector2(center.x, rect.yMax + SpawnClearance);
            case LevelSide.East:
                return new Vector2(rect.xMin - SpawnClearance, center.y);
            default:
                return new Vector2(rect.xMax + SpawnClearance, center.y);
        }
    }

    void Measure(List<Room> rooms)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2Int grid = rooms[i].Grid;
            if (grid.x < minX) minX = grid.x;
            if (grid.y < minY) minY = grid.y;
            if (grid.x > maxX) maxX = grid.x;
            if (grid.y > maxY) maxY = grid.y;
        }

        GridMin = new Vector2Int(minX, minY);
        Columns = maxX - minX + 1;
        Rows = maxY - minY + 1;
    }

    void ApplyAtmosphere()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        if (!_backgroundStored)
        {
            _homeBackground = _camera.backgroundColor;
            _backgroundStored = true;
        }

        _camera.backgroundColor = _place == WorldPlace.LevelOne ? ForestVoid : _homeBackground;
    }

    static void Pulse(Gate gate, float wave)
    {
        if (gate == null || gate.Fill == null)
            return;

        Color color = gate.Color;
        color.a *= wave;
        gate.Fill.color = color;
    }

    static Vector2 GateSize(Bounds bounds, LevelSide side, float bodyInset)
    {
        bool horizontalEdge = side == LevelSide.North || side == LevelSide.South;
        float along = (horizontalEdge ? bounds.size.x : bounds.size.y) * GateEdgeFraction;
        along = Mathf.Clamp(along, 8f, 20f);
        // The body stops short of the edge, so the zone has to reach past that inset.
        float depth = Mathf.Max(4.2f, bodyInset + 2.6f);
        return horizontalEdge ? new Vector2(along, depth) : new Vector2(depth, along);
    }

    static Vector2 GateCenter(Bounds bounds, LevelSide side, Vector2 size)
    {
        switch (side)
        {
            case LevelSide.North:
                return new Vector2(bounds.center.x, bounds.max.y - size.y * 0.5f);
            case LevelSide.South:
                return new Vector2(bounds.center.x, bounds.min.y + size.y * 0.5f);
            case LevelSide.East:
                return new Vector2(bounds.max.x - size.x * 0.5f, bounds.center.y);
            default:
                return new Vector2(bounds.min.x + size.x * 0.5f, bounds.center.y);
        }
    }

    static LevelSide SideBetween(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (delta.y > 0)
            return LevelSide.North;
        if (delta.y < 0)
            return LevelSide.South;
        if (delta.x > 0)
            return LevelSide.East;
        return LevelSide.West;
    }

    static LevelSide Opposite(LevelSide side)
    {
        switch (side)
        {
            case LevelSide.North:
                return LevelSide.South;
            case LevelSide.South:
                return LevelSide.North;
            case LevelSide.East:
                return LevelSide.West;
            default:
                return LevelSide.East;
        }
    }

    static bool Crossed(Rect rect, Vector2 from, Vector2 to)
    {
        if (rect.Contains(to))
            return true;

        float distance = Vector2.Distance(from, to);
        int steps = Mathf.CeilToInt(distance / 0.4f);
        for (int i = 1; i < steps; i++)
        {
            if (rect.Contains(Vector2.Lerp(from, to, i / (float)steps)))
                return true;
        }

        return false;
    }

    SpriteRenderer CreateQuad(Transform parent, string name, Vector2 worldCenter, Vector2 size, Color color, int sortingOrder)
    {
        return CreateSprite(parent, name, worldCenter, size, color, sortingOrder, Quad());
    }

    SpriteRenderer CreateDisc(Transform parent, string name, Vector2 worldCenter, float diameter, Color color, int sortingOrder)
    {
        return CreateSprite(parent, name, worldCenter, new Vector2(diameter, diameter), color, sortingOrder, Disc());
    }

    SpriteRenderer CreateSprite(Transform parent, string name, Vector2 worldCenter, Vector2 size, Color color, int sortingOrder, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        SpriteRenderer ground = parent.parent != null
            ? parent.parent.GetComponent<SpriteRenderer>()
            : null;
        if (ground != null && ground.sharedMaterial != null)
            renderer.sharedMaterial = ground.sharedMaterial;

        return renderer;
    }

    Sprite Quad()
    {
        if (_quad != null)
            return _quad;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        _quad = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _quad.hideFlags = HideFlags.HideAndDontSave;
        return _quad;
    }

    Sprite Disc()
    {
        if (_disc != null)
            return _disc;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(0.72f, 1f, distance));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        _disc = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        _disc.hideFlags = HideFlags.HideAndDontSave;
        return _disc;
    }
}
