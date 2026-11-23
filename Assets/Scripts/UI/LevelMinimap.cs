using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Rooms of the current world, top-right. Home shows the whole cross.
/// Level 1 keeps a stable grid but draws only the rooms already walked into.
/// </summary>
public class LevelMinimap : MonoBehaviour
{
    const float MarginX = 28f;
    const float MarginY = 28f;
    const float MaxWidth = 360f;
    const float MaxHeight = 460f;
    const float Pad = 12f;
    const float Marker = 8f;
    const float Caption = 28f;
    const float CaptionGap = 6f;

    static readonly Color PanelColor = new Color(0.10f, 0.09f, 0.08f, 0.94f);
    static readonly Color RimColor = new Color(0.22f, 0.18f, 0.13f, 0.9f);
    static readonly Color FrameColor = new Color(0.16f, 0.14f, 0.12f, 1f);
    static readonly Color CurrentFrame = new Color(0.93f, 0.84f, 0.46f, 1f);
    static readonly Color PathColor = new Color(0.34f, 0.38f, 0.44f, 1f);
    static readonly Color MarkerOutline = new Color(0.08f, 0.07f, 0.06f, 1f);
    static readonly Color MarkerFill = new Color(0.97f, 0.95f, 0.88f, 1f);
    static readonly Color ExitPip = new Color(0.95f, 0.78f, 0.28f, 1f);
    static readonly Color CaptionColor = new Color(0.96f, 0.94f, 0.88f, 1f);

    LevelWorld _world;
    PlayerController _player;
    GameObject _canvasObject;
    RectTransform _panel;
    readonly List<MapCell> _cells = new List<MapCell>();
    readonly List<MapPath> _paths = new List<MapPath>();
    RectTransform _marker;
    RectTransform _caption;
    TMP_Text _captionLabel;
    LevelWorld.Room _markedRoom;
    readonly MapSketch _sketch = new MapSketch();
    readonly List<GameObject> _pins = new List<GameObject>();
    float _cell = 36f;
    float _gap = 12f;
    int _mode;
    int _drawnMode = -1;
    int _interestVersion = -1;

    struct MapCell
    {
        public LevelWorld.Room Room;
        public Image Frame;
        public Image Fill;
        public RectTransform Rect;
    }

    struct MapPath
    {
        public LevelWorld.Room A;
        public LevelWorld.Room B;
        public GameObject Root;
    }

    public void Initialize(LevelWorld world, PlayerController player)
    {
        if (_world != null)
            _world.LayoutChanged -= Rebuild;

        _world = world;
        _player = player;
        _mode = ProgressionService.MapMode;
        _world.LayoutChanged += Rebuild;
        Rebuild();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool key = keyboard != null && keyboard.mKey.wasPressedThisFrame;
        bool click = false;
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && _caption != null)
            click = RectTransformUtility.RectangleContainsScreenPoint(_caption, mouse.position.ReadValue(), null);

        if (!key && !click)
            return;

        _mode = (_mode + 1) % 3;
        if (ProgressionService.PersistInterface)
            ProgressionService.RememberMap(_mode);
        ApplyMode();
        Refresh(true);
    }

    void OnDestroy()
    {
        if (_world != null)
            _world.LayoutChanged -= Rebuild;

        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void LateUpdate()
    {
        Refresh(false);
    }

    void Rebuild()
    {
        if (_canvasObject != null)
        {
            DestroyImmediate(_canvasObject);
            _canvasObject = null;
        }

        _cells.Clear();
        _paths.Clear();
        _pins.Clear();
        _marker = null;
        _markedRoom = null;
        _interestVersion = -1;
        if (_world == null || _player == null || _world.Columns <= 0 || _world.Rows <= 0)
            return;

        Build();
        Refresh(true);
    }

    void Refresh(bool force)
    {
        if (_world == null || _player == null)
            return;

        LevelWorld.Room current = _world.Current;
        FillSketch();
        if (force || current != _markedRoom || _drawnMode != _mode)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                MapCell cell = _cells[i];
                bool show = i < _sketch.Cells.Count && _sketch.Cells[i].Shown;
                cell.Rect.gameObject.SetActive(show);
                if (!show)
                    continue;

                bool here = cell.Room == current;
                cell.Frame.color = here ? CurrentFrame : FrameColor;
                cell.Fill.color = _world.MiniColor(cell.Room, here);
                float border = here ? 3f : 2f;
                cell.Fill.rectTransform.offsetMin = new Vector2(border, border);
                cell.Fill.rectTransform.offsetMax = new Vector2(-border, -border);
            }

            for (int i = 0; i < _paths.Count; i++)
            {
                MapPath path = _paths[i];
                bool showPath = PathShown(path.A) && PathShown(path.B);
                path.Root.SetActive(showPath);
            }

            _markedRoom = current;
            _drawnMode = _mode;
            ApplyMode();
        }

        if (_interestVersion != _world.InterestVersion || force)
        {
            _interestVersion = _world.InterestVersion;
            RebuildPins();
        }

        if (current == null || _marker == null)
            return;

        MapCell hereCell = default;
        bool found = false;
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].Room != current || !PathShown(_cells[i].Room))
                continue;

            hereCell = _cells[i];
            found = true;
            break;
        }

        if (!found)
            return;

        if (_marker.parent != hereCell.Rect)
            _marker.SetParent(hereCell.Rect, false);

        Vector2 feet = _world.Suspended ? _world.SuspendedFeet : (Vector2)_player.transform.position;
        Bounds bounds = current.Bounds;
        float nx = bounds.size.x <= 0.01f
            ? 0.5f
            : Mathf.InverseLerp(bounds.min.x, bounds.max.x, feet.x);
        float ny = bounds.size.y <= 0.01f
            ? 0.5f
            : Mathf.InverseLerp(bounds.min.y, bounds.max.y, feet.y);

        const float inset = 6f;
        _marker.anchoredPosition = new Vector2(
            Mathf.Lerp(inset, _cell - inset, Mathf.Clamp01(nx)),
            Mathf.Lerp(inset, _cell - inset, Mathf.Clamp01(ny)));
    }

    bool PathShown(LevelWorld.Room room)
    {
        return _mode == 0 && room != null && (!_world.FogOfWar || room.Visited);
    }

    void FillSketch()
    {
        _sketch.Clear();
        _sketch.Mode = _mode;
        if (_world == null)
            return;

        for (int i = 0; i < _cells.Count; i++)
        {
            LevelWorld.Room room = _cells[i].Room;
            bool current = room == _world.Current;
            _sketch.AddCell(Col(room.Grid), Row(room.Grid), room.Visited, current, room.IsExit, _world.FogOfWar);
            if (room.Interests == null)
                continue;

            bool shown = !_world.FogOfWar || room.Visited;
            Bounds bounds = room.Bounds;
            for (int p = 0; p < room.Interests.Count; p++)
            {
                MapInterest interest = room.Interests[p];
                float nx = bounds.size.x <= 0.01f ? 0.5f : Mathf.InverseLerp(bounds.min.x, bounds.max.x, interest.Point.x);
                float ny = bounds.size.y <= 0.01f ? 0.5f : Mathf.InverseLerp(bounds.min.y, bounds.max.y, interest.Point.y);
                _sketch.AddPin(Col(room.Grid), Row(room.Grid), nx, ny, interest.Kind, shown);
            }
        }
    }

    void ApplyMode()
    {
        if (_panel != null)
            _panel.gameObject.SetActive(_mode == 0);
        if (_caption != null)
            _caption.gameObject.SetActive(true);
        if (_captionLabel == null)
            return;

        string caption = _mode == 0
            ? CaptionText()
            : _mode == 1
                ? UiGlyphs.Pick("Карта", "Map")
                : UiGlyphs.Pick("Карта ▸", "Map");
        _captionLabel.text = caption;
        UiGlyphs.Prepare(_captionLabel, caption);
    }

    void RebuildPins()
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            if (_pins[i] != null)
                Destroy(_pins[i]);
        }

        _pins.Clear();
        if (_mode != 0)
            return;

        for (int i = 0; i < _sketch.Pins.Count; i++)
        {
            MapSketch.Pin pin = _sketch.Pins[i];
            if (!pin.Shown)
                continue;

            RectTransform cell = CellRect(pin.Column, pin.Row);
            if (cell == null)
                continue;

            var dot = CreateImage(pin.Kind.ToString(), cell, PinColor(pin.Kind));
            dot.rectTransform.anchorMin = new Vector2(pin.X, pin.Y);
            dot.rectTransform.anchorMax = new Vector2(pin.X, pin.Y);
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(6f, 6f);
            dot.rectTransform.anchoredPosition = Vector2.zero;
            _pins.Add(dot.gameObject);
        }
    }

    RectTransform CellRect(int column, int row)
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            LevelWorld.Room room = _cells[i].Room;
            if (room != null && Col(room.Grid) == column && Row(room.Grid) == row)
                return _cells[i].Rect;
        }

        return null;
    }

    static Color PinColor(MapPinKind kind)
    {
        switch (kind)
        {
            case MapPinKind.Chest: return new Color(0.93f, 0.78f, 0.32f, 1f);
            case MapPinKind.Mechanism: return new Color(0.45f, 0.72f, 0.95f, 1f);
            case MapPinKind.Exit: return new Color(0.95f, 0.78f, 0.28f, 1f);
            default: return new Color(0.96f, 0.94f, 0.88f, 1f);
        }
    }

    void Build()
    {
        bool wide = _world.Columns >= 4;
        _cell = wide ? 30f : 36f;
        _gap = wide ? 8f : 12f;

        _canvasObject = new GameObject("Level Minimap");
        UiScreen.Mount(_canvasObject, 110);

        float mapW = _world.Columns * _cell + (_world.Columns - 1) * _gap;
        float mapH = _world.Rows * _cell + (_world.Rows - 1) * _gap;
        float panelW = mapW + Pad * 2f;
        float panelH = mapH + Pad * 2f;

        var dock = CreateRect("Dock", _canvasObject.transform);
        dock.anchorMin = new Vector2(1f, 1f);
        dock.anchorMax = new Vector2(1f, 1f);
        dock.pivot = new Vector2(1f, 1f);
        dock.sizeDelta = new Vector2(panelW, Caption + CaptionGap + panelH);
        dock.anchoredPosition = new Vector2(-MarginX, -MarginY);
        var fit = dock.gameObject.AddComponent<UiScaleToFit>();
        fit.Margin = 0f;
        fit.MaxWidth = MaxWidth;
        fit.MaxHeight = MaxHeight;
        fit.MinScale = 0.45f;
        fit.Apply();

        _caption = CreateRect("Caption", dock);
        var caption = _caption;
        caption.anchorMin = new Vector2(0f, 1f);
        caption.anchorMax = new Vector2(1f, 1f);
        caption.pivot = new Vector2(0.5f, 1f);
        caption.sizeDelta = new Vector2(0f, Caption);
        caption.anchoredPosition = Vector2.zero;

        var captionBg = CreateImage("Background", caption, PanelColor);
        Stretch(captionBg.rectTransform);
        _captionLabel = CreateCaption(caption, CaptionText());

        _panel = CreateRect("Panel", dock);
        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.sizeDelta = new Vector2(panelW, panelH);
        _panel.anchoredPosition = new Vector2(0f, -(Caption + CaptionGap));

        var background = CreateImage("Background", _panel, PanelColor);
        Stretch(background.rectTransform);

        var rim = CreateImage("Rim", _panel, RimColor);
        var rimRect = rim.rectTransform;
        rimRect.anchorMin = new Vector2(0f, 1f);
        rimRect.anchorMax = new Vector2(1f, 1f);
        rimRect.pivot = new Vector2(0.5f, 1f);
        rimRect.anchoredPosition = Vector2.zero;
        rimRect.sizeDelta = new Vector2(0f, 1f);

        for (int y = 0; y < _world.Rows; y++)
        {
            for (int x = 0; x < _world.Columns; x++)
            {
                Vector2Int grid = new Vector2Int(_world.GridMin.x + x, _world.GridMin.y + (_world.Rows - 1 - y));
                AddRoom(grid);
            }
        }

        for (int i = 0; i < _cells.Count; i++)
        {
            LevelWorld.Room room = _cells[i].Room;
            LevelWorld.Room east = _world.RoomAt(room.Grid + Vector2Int.right);
            if (east != null)
                AddPath(room, east, Col(east.Grid), Row(room.Grid), false);

            LevelWorld.Room north = _world.RoomAt(room.Grid + Vector2Int.up);
            if (north != null)
                AddPath(room, north, Col(room.Grid), Row(room.Grid), true);
        }

        _marker = CreateRect("Marker", _panel);
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

    int Col(Vector2Int grid)
    {
        return grid.x - _world.GridMin.x;
    }

    int Row(Vector2Int grid)
    {
        return (_world.GridMin.y + _world.Rows - 1) - grid.y;
    }

    void AddRoom(Vector2Int grid)
    {
        LevelWorld.Room room = _world.RoomAt(grid);
        if (room == null)
            return;

        int col = Col(grid);
        int row = Row(grid);

        var rect = CreateRect(room.Name, _panel);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(_cell, _cell);
        rect.anchoredPosition = new Vector2(Pad + col * (_cell + _gap), -(Pad + row * (_cell + _gap)));

        var frame = CreateImage("Frame", rect, FrameColor);
        Stretch(frame.rectTransform);

        var fill = CreateImage("Fill", rect, _world.MiniColor(room, false));
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(2f, 2f);
        fill.rectTransform.offsetMax = new Vector2(-2f, -2f);

        if (room.IsExit)
        {
            var pip = CreateImage("Exit", rect, ExitPip);
            pip.rectTransform.anchorMin = new Vector2(1f, 0f);
            pip.rectTransform.anchorMax = new Vector2(1f, 0f);
            pip.rectTransform.pivot = new Vector2(1f, 0f);
            pip.rectTransform.sizeDelta = new Vector2(8f, 8f);
            pip.rectTransform.anchoredPosition = new Vector2(-3f, 3f);
        }

        rect.gameObject.SetActive(PathShown(room));
        _cells.Add(new MapCell
        {
            Room = room,
            Frame = frame,
            Fill = fill,
            Rect = rect
        });
    }

    void AddPath(LevelWorld.Room a, LevelWorld.Room b, int col, int row, bool verticalNeighbor)
    {
        var rect = CreateRect("Path", _panel);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        if (verticalNeighbor)
        {
            float x = Pad + col * (_cell + _gap) + (_cell - 4f) * 0.5f;
            float y = Pad + row * (_cell + _gap) - _gap;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(4f, _gap);
        }
        else
        {
            float x = Pad + col * (_cell + _gap) - _gap;
            float y = Pad + row * (_cell + _gap) + (_cell - 4f) * 0.5f;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(_gap, 4f);
        }

        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = Pixel();
        image.color = PathColor;
        image.raycastTarget = false;
        rect.gameObject.SetActive(PathShown(a) && PathShown(b));

        _paths.Add(new MapPath
        {
            A = a,
            B = b,
            Root = rect.gameObject
        });
    }

    TMP_Text CreateCaption(RectTransform parent, string caption)
    {
        var rect = CreateRect("Label", parent);
        Stretch(rect);
        return UiGlyphs.Place(rect.gameObject, caption, 18f, TextAnchor.MiddleCenter, CaptionColor, UiGlyphs.Wrap.Ellipsis);
    }

    string CaptionText()
    {
        return UiGlyphs.Supports(_world.PlaceName) ? _world.PlaceName : _world.PlaceNameEnglish;
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
