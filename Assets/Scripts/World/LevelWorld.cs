using System.Collections.Generic;
using UnityEngine;

public enum LevelSide
{
    North,
    East,
    South,
    West
}

/// <summary>
/// Five rooms in a cross. The middle of a side that leads somewhere is a blue
/// zone: walking into it enters the next room from the opposite side, a short
/// step inside that zone so the way back is not taken on the same step.
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
    }

    public sealed class Gate
    {
        public LevelSide Side;
        public Rect Rect;
        public Room Destination;
        public LevelSide ArrivalSide;
        public SpriteRenderer Fill;
        public Color Color;
    }

    const float RoomGap = 40f;
    const float GateEdgeFraction = 0.24f;
    const float SpawnClearance = 2.2f;
    const float TransitionCooldown = 0.12f;

    static readonly Color GateColor = new Color(0.22f, 0.55f, 1f, 0.78f);
    static readonly Color GateBorder = new Color(0.06f, 0.20f, 0.58f, 1f);

    public static LevelWorld Instance { get; private set; }

    public Room Current { get; private set; }
    public IReadOnlyList<Room> Rooms => _rooms;

    readonly List<Room> _rooms = new List<Room>();
    PlayerController _player;
    Vector2 _previousFeet;
    float _cooldown;
    Sprite _quad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FindAnyObjectByType<LevelWorld>() != null)
            return;

        if (FindAnyObjectByType<PlayArea>() == null || FindAnyObjectByType<PlayerController>() == null)
            return;

        var host = new GameObject("Level World");
        host.AddComponent<LevelWorld>();
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
        if (Current == null)
            return;

        float wave = 0.72f + 0.28f * Mathf.Sin(Time.time * 2.8f);
        Gate[] gates = Current.Gates;
        for (int i = 0; i < gates.Length; i++)
        {
            Gate gate = gates[i];
            if (gate == null || gate.Fill == null)
                continue;

            Color color = gate.Color;
            color.a *= wave;
            gate.Fill.color = color;
        }
    }

    void LateUpdate()
    {
        if (_player == null || Current == null)
            return;

        Vector2 feet = _player.transform.position;
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
            _previousFeet = feet;
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
        for (int i = 0; i < _rooms.Count; i++)
        {
            if (_rooms[i].Grid == grid)
                return _rooms[i];
        }

        return null;
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

        Room center = Register(centerArea.gameObject, Vector2Int.zero, "Center", Color.white);
        Room north = CreateRoom(center, Vector2Int.up, "North", new Color(0.82f, 0.90f, 1f));
        Room south = CreateRoom(center, Vector2Int.down, "South", new Color(1f, 0.90f, 0.78f));
        Room east = CreateRoom(center, Vector2Int.right, "East", new Color(0.84f, 1f, 0.84f));
        Room west = CreateRoom(center, Vector2Int.left, "West", new Color(0.93f, 0.84f, 1f));

        Link(center, north);
        Link(center, south);
        Link(center, east);
        Link(center, west);

        _previousFeet = _player.transform.position;
        Show(center);
        _player.SetPlayArea(center.Area);
        Current = center;

        var minimap = gameObject.AddComponent<LevelMinimap>();
        minimap.Initialize(this, _player);
    }

    Room CreateRoom(Room origin, Vector2Int grid, string name, Color tint)
    {
        Vector2Int delta = grid - origin.Grid;
        Vector3 position = origin.Root.transform.position + new Vector3(
            delta.x * (origin.Bounds.size.x + RoomGap),
            delta.y * (origin.Bounds.size.y + RoomGap),
            0f);

        GameObject copy = Instantiate(origin.Root, position, Quaternion.identity, transform);
        copy.name = "Level " + name;

        var renderer = copy.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = tint;

        return Register(copy, grid, name, tint);
    }

    Room Register(GameObject root, Vector2Int grid, string name, Color tint)
    {
        root.name = "Level " + name;
        root.transform.SetParent(transform, true);

        var renderer = root.GetComponent<SpriteRenderer>();
        var area = root.GetComponent<PlayArea>();
        if (area != null)
            area.CacheBounds();

        var room = new Room
        {
            Name = name,
            Grid = grid,
            Tint = tint,
            Bounds = renderer != null ? renderer.bounds : new Bounds(root.transform.position, Vector3.one),
            Area = area,
            Root = root,
            Gates = new Gate[4]
        };

        _rooms.Add(room);
        return room;
    }

    void Link(Room from, Room to)
    {
        LevelSide side = SideBetween(from.Grid, to.Grid);
        AddGate(from, side, to);
        AddGate(to, Opposite(side), from);
    }

    void AddGate(Room room, LevelSide side, Room destination)
    {
        Vector2 size = GateSize(room.Bounds, side, _player.BodyInset());
        Vector2 center = GateCenter(room.Bounds, side, size);

        var root = new GameObject("Gate " + side);
        root.transform.SetParent(room.Root.transform, false);

        CreateQuad(root.transform, "Border", center, size + new Vector2(0.45f, 0.45f), GateBorder, 1);
        SpriteRenderer fill = CreateQuad(root.transform, "Fill", center, size, GateColor, 2);

        room.Gates[(int)side] = new Gate
        {
            Side = side,
            Rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y),
            Destination = destination,
            ArrivalSide = Opposite(side),
            Fill = fill,
            Color = GateColor
        };
    }

    void Enter(Room room, LevelSide arrivalSide)
    {
        Show(room);
        _player.SetPlayArea(room.Area);
        _player.Place(SpawnPoint(room, arrivalSide));
        Current = room;
        _cooldown = TransitionCooldown;
    }

    void Show(Room room)
    {
        for (int i = 0; i < _rooms.Count; i++)
            _rooms[i].Root.SetActive(_rooms[i] == room);
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
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Quad();
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
}
