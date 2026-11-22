using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rooms of the current world, top-right. Home shows the whole cross.
/// Level 1 keeps a stable grid but draws only the rooms already walked into.
/// </summary>
public class LevelMinimap : MonoBehaviour
{
    const float MarginX = 28f;
    const float MarginY = 24f;
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
    LevelWorld.Room _markedRoom;
    float _cell = 36f;
    float _gap = 12f;

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
        _world.LayoutChanged += Rebuild;
        Rebuild();
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
        _marker = null;
        _markedRoom = null;
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
        if (force || current != _markedRoom)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                MapCell cell = _cells[i];
                bool show = Shown(cell.Room);
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
                path.Root.SetActive(Shown(path.A) && Shown(path.B));
            }

            _markedRoom = current;
        }

        if (current == null || _marker == null)
            return;

        MapCell hereCell = default;
        bool found = false;
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].Room != current || !Shown(_cells[i].Room))
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

    bool Shown(LevelWorld.Room room)
    {
        return room != null && (!_world.FogOfWar || room.Visited);
    }

    void Build()
    {
        bool wide = _world.Columns >= 4;
        _cell = wide ? 30f : 36f;
        _gap = wide ? 8f : 12f;

        _canvasObject = new GameObject("Level Minimap");

        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        float mapW = _world.Columns * _cell + (_world.Columns - 1) * _gap;
        float mapH = _world.Rows * _cell + (_world.Rows - 1) * _gap;
        float panelW = mapW + Pad * 2f;
        float panelH = mapH + Pad * 2f;

        var caption = CreateRect("Caption", _canvasObject.transform);
        caption.anchorMin = new Vector2(1f, 1f);
        caption.anchorMax = new Vector2(1f, 1f);
        caption.pivot = new Vector2(1f, 1f);
        caption.sizeDelta = new Vector2(panelW, Caption);
        caption.anchoredPosition = new Vector2(-MarginX, -MarginY);

        var captionBg = CreateImage("Background", caption, PanelColor);
        Stretch(captionBg.rectTransform);
        CreateCaption(caption, CaptionText());

        _panel = CreateRect("Panel", _canvasObject.transform);
        _panel.anchorMin = new Vector2(1f, 1f);
        _panel.anchorMax = new Vector2(1f, 1f);
        _panel.pivot = new Vector2(1f, 1f);
        _panel.sizeDelta = new Vector2(panelW, panelH);
        _panel.anchoredPosition = new Vector2(-MarginX, -(MarginY + Caption + CaptionGap));

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

        rect.gameObject.SetActive(Shown(room));
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
        rect.gameObject.SetActive(Shown(a) && Shown(b));

        _paths.Add(new MapPath
        {
            A = a,
            B = b,
            Root = rect.gameObject
        });
    }

    void CreateCaption(RectTransform parent, string caption)
    {
        var rect = CreateRect("Label", parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = parent.sizeDelta;

        var text = rect.gameObject.AddComponent<Text>();
        text.font = UiGlyphs.Font;
        text.fontSize = 16;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = CaptionColor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.text = caption;
        UiGlyphs.Prepare(text, caption);
    }

    string CaptionText()
    {
        string russian = _world.PlaceName;
        Font font = UiGlyphs.Font;
        if (font == null || string.IsNullOrEmpty(russian))
            return _world.PlaceNameEnglish;

        // Space and digits have no bitmap in some dynamic fonts, so they are not a reason to drop Cyrillic.
        font.RequestCharactersInTexture(russian, 64, FontStyle.Normal);
        for (int i = 0; i < russian.Length; i++)
        {
            char c = russian[i];
            if (c == ' ' || (c >= '0' && c <= '9'))
                continue;
            if (!font.HasCharacter(c))
                return _world.PlaceNameEnglish;
        }

        return russian;
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
