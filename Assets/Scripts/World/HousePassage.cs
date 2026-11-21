using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// A doorway. Near it, E is offered. The same Interact action the project already binds to E.
/// </summary>
public class HouseDoor : MonoBehaviour
{
    const float PromptHeight = 1.35f;

    bool _exit;
    Vector2 _point;
    Vector2 _returnPoint;
    float _radius = 1.8f;
    bool _ready;
    InputAction _interact;
    PlayerController _player;
    GameObject _canvasObject;
    RectTransform _panel;
    Text _label;

    public void Configure(bool exit, Vector2 point, float radius, Vector2 returnPoint)
    {
        _exit = exit;
        _point = point;
        _radius = Mathf.Max(0.4f, radius);
        _returnPoint = returnPoint;
        _ready = true;
        if (_interact == null && InputSystem.actions != null)
            _interact = InputSystem.actions.FindAction("Player/Interact", throwIfNotFound: false);
        _interact?.Enable();
        if (_canvasObject == null)
            BuildPrompt();
        else if (_label != null)
            _label.text = Caption();
    }

    void Update()
    {
        if (!_ready)
            return;

        if (_player == null && GameRoot.Active != null)
            _player = GameRoot.Active.Player;
        if (_player == null)
        {
            Show(false);
            return;
        }

        PlayerInventory inventory = _player.GetComponent<PlayerInventory>();
        bool near = GameplayGate.EnsureState().Allows(PlayerAction.Interact);
        if (near)
            near = inventory == null || !inventory.IsOpen;
        if (near)
        {
            float distance = Vector2.Distance(_player.transform.position, _point);
            near = distance <= _radius;
        }

        if (near && LocationFlow.IsBusy)
            near = false;
        if (near && _exit != LocationFlow.IsInside)
            near = false;

        Show(near);
        if (!near || !Pressed() || LocationFlow.IsBusy)
            return;

        if (_exit)
            LocationFlow.Exit();
        else
            LocationFlow.Enter(_returnPoint);
    }

    void LateUpdate()
    {
        if (_panel == null || !_panel.gameObject.activeSelf)
            return;

        Camera cam = Camera.main;
        if (cam == null || _canvasObject == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint((Vector3)(_point + Vector2.up * PromptHeight));
        if (screen.z < 0f)
        {
            Show(false);
            return;
        }

        RectTransform canvasRect = _canvasObject.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
            return;

        _panel.anchoredPosition = local;
    }

    bool Pressed()
    {
        if (_interact != null)
            return _interact.WasPressedThisFrame();

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
    }

    void Show(bool visible)
    {
        if (_panel != null)
            _panel.gameObject.SetActive(visible);
    }

    string Caption()
    {
        Font font = BuiltinFont();
        if (_exit)
            return Supports(font, "Выйти") ? "E — Выйти" : "E - Exit";

        return Supports(font, "Войти") ? "E — Войти" : "E - Enter";
    }

    void BuildPrompt()
    {
        _canvasObject = new GameObject(_exit ? "Exit Prompt" : "Enter Prompt");
        _canvasObject.transform.SetParent(transform, false);

        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 125;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = NewRect("Panel", _canvasObject.transform);
        _panel.sizeDelta = new Vector2(210f, 46f);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);

        var frame = NewImage("Frame", _panel, new Color(0.06f, 0.05f, 0.04f, 0.96f));
        Stretch(frame.rectTransform);
        var fill = NewImage("Fill", _panel, new Color(0.16f, 0.12f, 0.09f, 0.94f));
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(_panel, false);
        var textRect = textObject.AddComponent<RectTransform>();
        Stretch(textRect);
        _label = textObject.AddComponent<Text>();
        _label.font = BuiltinFont();
        _label.fontSize = 22;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.color = new Color(0.96f, 0.93f, 0.84f, 1f);
        _label.raycastTarget = false;
        _label.text = Caption();
        if (_label.font != null)
            _label.font.RequestCharactersInTexture(_label.text, _label.fontSize, FontStyle.Normal);

        Show(false);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static bool Supports(Font font, string sample)
    {
        if (font == null || string.IsNullOrEmpty(sample))
            return false;

        font.RequestCharactersInTexture(sample, 64, FontStyle.Normal);
        for (int i = 0; i < sample.Length; i++)
        {
            if (!font.HasCharacter(sample[i]))
                return false;
        }

        return true;
    }

    static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
