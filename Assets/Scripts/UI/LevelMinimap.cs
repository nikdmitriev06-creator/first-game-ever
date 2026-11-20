using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cross of every room in the top-right corner. The current room is outlined
/// and carries the player marker.
/// </summary>
public class LevelMinimap : MonoBehaviour
{
    const float MarginX = 28f;
    const float MarginY = 24f;
    const float Cell = 36f;
    const float Gap = 12f;
    const float Pad = 12f;
    const float Marker = 8f;

    static readonly Color PanelColor = new Color(0.10f, 0.09f, 0.08f, 0.94f);
    static readonly Color RimColor = new Color(0.22f, 0.18f, 0.13f, 0.9f);
    static readonly Color FrameColor = new Color(0.16f, 0.14f, 0.12f, 1f);
    static readonly Color CurrentFrame = new Color(0.93f, 0.84f, 0.46f, 1f);
    static readonly Color PathColor = new Color(0.34f, 0.38f, 0.44f, 1f);
    static readonly Color MarkerOutline = new Color(0.08f, 0.07f, 0.06f, 1f);
    static readonly Color MarkerFill = new Color(0.97f, 0.95f, 0.88f, 1f);
    static readonly Color Grass = new Color(0.30f, 0.46f, 0.26f, 1f);

    LevelWorld _world;
    PlayerController _player;
    GameObject _canvasObject;
    readonly MapCell[] _cells = new MapCell[8];
    int _cellCount;
    RectTransform _marker;
    LevelWorld.Room _markedRoom;

    struct MapCell
    {
        public LevelWorld.Room Room;
        public Image Frame;
        public Image Fill;
        public RectTransform Rect;
    }

    public void Initialize(LevelWorld world, PlayerController player)
    {
        _world = world;
        _player = player;
        Build();
        Refresh(force: true);
    }

    void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void LateUpdate()
    {
        Refresh(force: false);
    }

    void Refresh(bool force)
    {
        if (_world == null || _player == null)
            return;

        LevelWorld.Room current = _world.Current;
        if (force || current != _markedRoom)
        {
            for (int i = 0; i < _cellCount; i++)
            {
                bool here = _cells[i].Room == current;
                _cells[i].Frame.color = here ? CurrentFrame : FrameColor;
                _cells[i].Fill.color = RoomColor(_cells[i].Room.Tint, here);
                float border = here ? 3f : 2f;
                _cells[i].Fill.rectTransform.offsetMin = new Vector2(border, border);
                _cells[i].Fill.rectTransform.offsetMax = new Vector2(-border, -border);
            }

            _markedRoom = current;
        }

        if (current == null || _marker == null)
            return;

        MapCell cell = default;
        bool found = false;
        for (int i = 0; i < _cellCount; i++)
        {
            if (_cells[i].Room != current)
                continue;

            cell = _cells[i];
            found = true;
            break;
        }

        if (!found)
            return;

        if (_marker.parent != cell.Rect)
            _marker.SetParent(cell.Rect, false);

        Bounds bounds = current.Bounds;
        float nx = bounds.size.x <= 0.01f
            ? 0.5f
            : Mathf.InverseLerp(bounds.min.x, bounds.max.x, _player.transform.position.x);
        float ny = bounds.size.y <= 0.01f
            ? 0.5f
            : Mathf.InverseLerp(bounds.min.y, bounds.max.y, _player.transform.position.y);

        const float inset = 6f;
        _marker.anchoredPosition = new Vector2(
            Mathf.Lerp(inset, Cell - inset, Mathf.Clamp01(nx)),
            Mathf.Lerp(inset, Cell - inset, Mathf.Clamp01(ny)));
    }

    void Build()
    {
        _canvasObject = new GameObject("Level Minimap");

        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        float map = Cell * 3f + Gap * 2f;
        float panelSize = map + Pad * 2f;

        var panel = CreateRect("Panel", _canvasObject.transform);
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.sizeDelta = new Vector2(panelSize, panelSize);
        panel.anchoredPosition = new Vector2(-MarginX, -MarginY);

        var background = CreateImage("Background", panel, PanelColor);
        Stretch(background.rectTransform);

        var rim = CreateImage("Rim", panel, RimColor);
        var rimRect = rim.rectTransform;
        rimRect.anchorMin = new Vector2(0f, 1f);
        rimRect.anchorMax = new Vector2(1f, 1f);
        rimRect.pivot = new Vector2(0.5f, 1f);
        rimRect.anchoredPosition = Vector2.zero;
        rimRect.sizeDelta = new Vector2(0f, 1f);

        // Connector sits in the gap just above or just left of the given cell.
        AddPath(panel, 1, 1, true);
        AddPath(panel, 1, 2, true);
        AddPath(panel, 1, 1, false);
        AddPath(panel, 2, 1, false);

        AddRoom(panel, new Vector2Int(0, 1));
        AddRoom(panel, new Vector2Int(-1, 0));
        AddRoom(panel, new Vector2Int(0, 0));
        AddRoom(panel, new Vector2Int(1, 0));
        AddRoom(panel, new Vector2Int(0, -1));

        _marker = CreateRect("Marker", panel);
        _marker.anchorMin = Vector2.zero;
        _marker.anchorMax = Vector2.zero;
        _marker.pivot = new Vector2(0.5f, 0.5f);
        _marker.sizeDelta = new Vector2(Marker + 4f, Marker + 4f);
        _marker.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var outline = CreateImage("Outline", _marker, MarkerOutline);
        Stretch(outline.rectTransform);

        var dot = CreateImage("Dot", _marker, MarkerFill);
        Stretch(dot.rectTransform);
        dot.rectTransform.offsetMin = new Vector2(2f, 2f);
        dot.rectTransform.offsetMax = new Vector2(-2f, -2f);
    }

    void AddRoom(RectTransform panel, Vector2Int grid)
    {
        LevelWorld.Room room = _world.RoomAt(grid);
        if (room == null || _cellCount >= _cells.Length)
            return;

        int col = grid.x + 1;
        int row = 1 - grid.y;

        var rect = CreateRect(room.Name, panel);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(Cell, Cell);
        rect.anchoredPosition = new Vector2(Pad + col * (Cell + Gap), -(Pad + row * (Cell + Gap)));

        var frame = CreateImage("Frame", rect, FrameColor);
        Stretch(frame.rectTransform);

        var fill = CreateImage("Fill", rect, RoomColor(room.Tint, false));
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(2f, 2f);
        fill.rectTransform.offsetMax = new Vector2(-2f, -2f);

        _cells[_cellCount++] = new MapCell
        {
            Room = room,
            Frame = frame,
            Fill = fill,
            Rect = rect
        };
    }

    void AddPath(RectTransform panel, int col, int row, bool verticalNeighbor)
    {
        var rect = CreateRect("Path", panel);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        if (verticalNeighbor)
        {
            float x = Pad + col * (Cell + Gap) + (Cell - 4f) * 0.5f;
            float y = Pad + row * (Cell + Gap) - Gap;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(4f, Gap);
        }
        else
        {
            float x = Pad + col * (Cell + Gap) - Gap;
            float y = Pad + row * (Cell + Gap) + (Cell - 4f) * 0.5f;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Gap, 4f);
        }

        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = Pixel();
        image.color = PathColor;
        image.raycastTarget = false;
    }

    static Color RoomColor(Color tint, bool current)
    {
        Color mixed = new Color(Grass.r * tint.r, Grass.g * tint.g, Grass.b * tint.b, 1f);
        return current ? Color.Lerp(mixed, Color.white, 0.42f) : Color.Lerp(mixed, Color.black, 0.28f);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = Pixel();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Sprite _pixel;

    static Sprite Pixel()
    {
        if (_pixel != null)
            return _pixel;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _pixel.hideFlags = HideFlags.HideAndDontSave;
        return _pixel;
    }
}
