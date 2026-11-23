using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Top-center travel list. Home is the grass cross. Level 1 is rolled again on every entry.
/// </summary>
public class WorldMenu : MonoBehaviour
{
    const float ButtonWidth = 168f;
    const float ButtonHeight = 46f;
    const float Top = 52f;
    const float PanelWidth = 248f;
    const float EntryHeight = 50f;
    const float EntryGap = 8f;
    const float Pad = 12f;

    static readonly Color FrameColor = new Color(0.10f, 0.09f, 0.08f, 1f);
    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f, 1f);
    static readonly Color PanelColor = new Color(0.12f, 0.11f, 0.10f, 0.96f);
    static readonly Color ButtonFill = new Color(0.24f, 0.22f, 0.19f, 1f);
    static readonly Color ButtonHot = new Color(0.36f, 0.32f, 0.26f, 1f);
    static readonly Color HomeFill = new Color(0.22f, 0.42f, 0.24f, 1f);
    static readonly Color HomeHot = new Color(0.32f, 0.58f, 0.34f, 1f);
    static readonly Color LevelFill = new Color(0.26f, 0.28f, 0.42f, 1f);
    static readonly Color LevelHot = new Color(0.38f, 0.40f, 0.60f, 1f);
    static readonly Color LabelColor = new Color(0.96f, 0.95f, 0.90f, 1f);

    static WorldMenu _instance;
    static int _blockFrame;

    LevelWorld _world;
    GameObject _canvasObject;
    RectTransform _button;
    RectTransform _panel;
    RectTransform _homeHit;
    RectTransform _levelHit;
    Image _buttonFill;
    Image _buttonFrame;
    Image _homeFill;
    Image _homeFrame;
    Image _levelFill;
    Image _levelFrame;
    bool _open;

    public static bool PointerIsOver()
    {
        if (_blockFrame == Time.frameCount)
            return true;
        if (_instance == null)
            return false;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        Vector2 screen = mouse.position.ReadValue();
        if (Contains(_instance._button, screen))
            return true;

        return _instance._open && Contains(_instance._panel, screen);
    }

    public void Initialize(LevelWorld world)
    {
        _world = world;
        Build();
    }

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            GameplayGate.EnsureState().Pop("world-menu");
        }

        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void Update()
    {
        if (_world == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetOpen(!_open);

        Mouse mouse = Mouse.current;
        Vector2 screen = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool overButton = Contains(_button, screen);
        bool overHome = _open && Contains(_homeHit, screen);
        bool overLevel = _open && Contains(_levelHit, screen);

        if (_buttonFill != null)
            _buttonFill.color = overButton ? ButtonHot : ButtonFill;
        if (_homeFill != null)
            _homeFill.color = overHome ? HomeHot : HomeFill;
        if (_levelFill != null)
            _levelFill.color = overLevel ? LevelHot : LevelFill;

        bool here = _world.Place == WorldPlace.Home;
        if (_homeFrame != null)
            _homeFrame.color = here ? Gold : FrameColor;
        if (_levelFrame != null)
            _levelFrame.color = here ? FrameColor : Gold;
        if (_buttonFrame != null)
            _buttonFrame.color = _open ? Gold : FrameColor;

        if (!pressed || GameplayGate.EnsureState().Has("inventory"))
            return;

        if (overHome)
        {
            Block();
            SetOpen(false);
            _world.TravelHome();
            return;
        }

        if (overLevel)
        {
            Block();
            SetOpen(false);
            _world.TravelLevelOne();
            return;
        }

        if (overButton)
        {
            Block();
            SetOpen(!_open);
        }
    }

    void SetOpen(bool open)
    {
        _open = open;
        if (_open)
            GameplayGate.EnsureState().Push("world-menu");
        else
            GameplayGate.EnsureState().Pop("world-menu");
        if (_panel != null)
            _panel.gameObject.SetActive(open);
    }

    static void Block()
    {
        _blockFrame = Time.frameCount;
    }

    void Build()
    {
        _canvasObject = new GameObject("World Menu");
        UiScreen.Mount(_canvasObject, 170);

        bool cyrillic = UiGlyphs.Supports("ДомУровеньМиры");

        _button = CreateRect("Worlds", _canvasObject.transform);
        _button.anchorMin = new Vector2(0.5f, 1f);
        _button.anchorMax = new Vector2(0.5f, 1f);
        _button.pivot = new Vector2(0.5f, 1f);
        _button.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        _button.anchoredPosition = new Vector2(0f, -Top);

        _buttonFrame = CreateImage("Frame", _button, FrameColor);
        Stretch(_buttonFrame.rectTransform);
        _buttonFill = CreateImage("Fill", _button, ButtonFill);
        Stretch(_buttonFill.rectTransform);
        _buttonFill.rectTransform.offsetMin = new Vector2(3f, 3f);
        _buttonFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        CreateLabel(_button, cyrillic ? "Миры" : "Worlds", 22f);

        float panelHeight = Pad * 2f + EntryHeight * 2f + EntryGap;
        _panel = CreateRect("Panel", _canvasObject.transform);
        _panel.anchorMin = new Vector2(0.5f, 1f);
        _panel.anchorMax = new Vector2(0.5f, 1f);
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.sizeDelta = new Vector2(PanelWidth, panelHeight);
        _panel.anchoredPosition = new Vector2(0f, -(Top + ButtonHeight + 8f));

        var panelFrame = CreateImage("Frame", _panel, Gold);
        Stretch(panelFrame.rectTransform);
        var plate = CreateImage("Plate", _panel, PanelColor);
        Stretch(plate.rectTransform);
        plate.rectTransform.offsetMin = new Vector2(2f, 2f);
        plate.rectTransform.offsetMax = new Vector2(-2f, -2f);

        float entryWidth = PanelWidth - Pad * 2f;
        _homeHit = CreateEntry(_panel, "Home", cyrillic ? "Дом" : "Home", 0f, entryWidth, out _homeFrame, out _homeFill, HomeFill);
        _levelHit = CreateEntry(_panel, "Level", cyrillic ? "Уровень 1" : "Level 1", EntryHeight + EntryGap, entryWidth, out _levelFrame, out _levelFill, LevelFill);

        SetOpen(false);
    }

    RectTransform CreateEntry(RectTransform parent, string name, string caption, float drop, float width, out Image frame, out Image fill, Color fillColor)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, EntryHeight);
        rect.anchoredPosition = new Vector2(0f, -(Pad + drop));

        frame = CreateImage("Frame", rect, FrameColor);
        Stretch(frame.rectTransform);
        fill = CreateImage("Fill", rect, fillColor);
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        CreateLabel(rect, caption, 22f);
        return rect;
    }

    static void CreateLabel(RectTransform parent, string caption, float size)
    {
        var rect = CreateRect("Label", parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);
        UiGlyphs.Place(rect.gameObject, caption, size, TextAnchor.MiddleCenter, LabelColor, UiGlyphs.Wrap.Ellipsis);
    }

    static bool Contains(RectTransform rect, Vector2 screen)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
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
