using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Test control in the bottom-right. Each click drops one slime on a random spot of the room you are in.
/// Clicks on the button are not sword swings.
/// </summary>
public class SlimeSpawnButton : MonoBehaviour
{
    const float Width = 168f;
    const float Height = 52f;
    const float Margin = 28f;

    static readonly Color FrameColor = new Color(0.10f, 0.09f, 0.08f, 1f);
    static readonly Color FillColor = new Color(0.20f, 0.48f, 0.26f, 1f);
    static readonly Color FillHot = new Color(0.30f, 0.66f, 0.34f, 1f);
    static readonly Color LabelColor = new Color(0.96f, 0.97f, 0.93f, 1f);

    static SlimeSpawnButton _instance;

    GameObject _canvasObject;
    RectTransform _root;
    Image _fill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FindAnyObjectByType<PlayerController>() == null)
            return;

        if (FindAnyObjectByType<SlimeSpawnButton>() != null)
            return;

        var host = new GameObject("Slime Spawn");
        host.AddComponent<SlimeSpawnButton>();
    }

    public static bool PointerIsOver()
    {
        if (_instance == null || _instance._root == null)
            return false;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(_instance._root, mouse.position.ReadValue(), null);
    }

    void Awake()
    {
        _instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || _root == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        bool over = RectTransformUtility.RectangleContainsScreenPoint(_root, screen, null);
        if (_fill != null)
            _fill.color = over ? FillHot : FillColor;

        if (!over || !mouse.leftButton.wasPressedThisFrame || GameplayGate.MenuOpen)
            return;

        SpawnOne();
    }

    void SpawnOne()
    {
        LevelWorld world = LevelWorld.Instance;
        if (world == null || world.Current == null || world.Suspended)
            return;

        LevelWorld.Room room = world.Current;
        Vector2 point = RandomPoint(room);
        Slime.Spawn(point, room.Area, room.Root.transform);
    }

    static Vector2 RandomPoint(LevelWorld.Room room)
    {
        // Stay inside the part of the room that is on screen, so a test spawn is somewhere you can see.
        Bounds bounds = VisibleBounds(room);
        const float inset = 1.2f;
        float minX = bounds.min.x + inset;
        float maxX = bounds.max.x - inset;
        float minY = bounds.min.y + inset;
        float maxY = bounds.max.y - inset;
        if (minX > maxX)
            minX = maxX = bounds.center.x;
        if (minY > maxY)
            minY = maxY = bounds.center.y;

        Vector2 player = Vector2.zero;
        PlayerController controller = FindAnyObjectByType<PlayerController>();
        if (controller != null)
            player = controller.transform.position;

        Vector2 picked = bounds.center;
        for (int i = 0; i < 8; i++)
        {
            picked = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (room.Area != null)
                picked = room.Area.Clamp(picked, inset);

            if ((picked - player).sqrMagnitude >= 16f)
                break;
        }

        return picked;
    }

    static Bounds VisibleBounds(LevelWorld.Room room)
    {
        Bounds bounds = room.Bounds;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return bounds;

        Vector3 center = cam.transform.position;
        float halfH = cam.orthographicSize * 0.82f;
        float halfW = halfH * Mathf.Max(0.5f, cam.aspect);
        var view = new Bounds(new Vector3(center.x, center.y, bounds.center.z), new Vector3(halfW * 2f, halfH * 2f, bounds.size.z));
        Vector3 min = Vector3.Max(bounds.min, view.min);
        Vector3 max = Vector3.Min(bounds.max, view.max);
        if (min.x >= max.x || min.y >= max.y)
            return bounds;

        bounds.SetMinMax(min, max);
        return bounds;
    }

    void Build()
    {
        _canvasObject = new GameObject("Slime Spawn Hud");

        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root = CreateRect("Button", _canvasObject.transform);
        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(1f, 0f);
        _root.sizeDelta = new Vector2(Width, Height);
        _root.anchoredPosition = new Vector2(-Margin, Margin);

        var frame = CreateImage("Frame", _root, FrameColor);
        Stretch(frame.rectTransform);

        _fill = CreateImage("Fill", _root, FillColor);
        Stretch(_fill.rectTransform);
        _fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        _fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

        CreateLabel(_root);
    }

    static void CreateLabel(RectTransform parent)
    {
        const float detail = 3f;
        Font font = BuiltinFont();
        int size = Mathf.RoundToInt(22f * detail);
        string caption = "Slime";
        if (font != null)
        {
            font.RequestCharactersInTexture("Слизень", size, FontStyle.Normal);
            if (font.HasCharacter('С'))
                caption = "Слизень";
        }

        var rect = CreateRect("Label", parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(Width * detail, Height * detail);
        rect.localScale = new Vector3(1f / detail, 1f / detail, 1f);

        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.alignByGeometry = true;
        text.color = LabelColor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.text = caption;
    }

    static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
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
