using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dark Souls style status bars in the top-left of the screen.
/// Health is the long red bar, mana the short blue bar, stamina the green bar between them in length.
/// </summary>
[RequireComponent(typeof(PlayerVitals))]
public class StatusHud : MonoBehaviour
{
    const float MarginX = 28f;
    const float MarginY = 28f;
    const float Gap = 6f;
    const float Frame = 3f;
    const float NumberSize = 18f;

    static readonly Color HealthColor = new Color(0.74f, 0.15f, 0.10f, 1f);
    static readonly Color HealthShine = new Color(0.90f, 0.34f, 0.16f, 1f);
    static readonly Color ManaColor = new Color(0.13f, 0.34f, 0.74f, 1f);
    static readonly Color ManaShine = new Color(0.38f, 0.58f, 0.90f, 1f);
    static readonly Color StaminaColor = new Color(0.58f, 0.64f, 0.16f, 1f);
    static readonly Color StaminaShine = new Color(0.76f, 0.78f, 0.30f, 1f);
    static readonly Color FrameColor = new Color(0.10f, 0.09f, 0.08f, 1f);
    static readonly Color TrackColor = new Color(0f, 0f, 0f, 1f);
    static readonly Color RimColor = new Color(0.55f, 0.44f, 0.24f, 0.95f);
    static readonly Color NumberColor = new Color(0.97f, 0.95f, 0.90f, 1f);

    PlayerVitals _vitals;
    GameObject _canvasObject;
    BarView _health;
    BarView _mana;
    BarView _stamina;

    struct BarView
    {
        public RectTransform Fill;
        public TMP_Text Label;
        public float InnerWidth;
        public float Shown;
        public float Target;
        public int ShownText;
    }

    void Awake()
    {
        _vitals = GetComponent<PlayerVitals>();
        Build();
    }

    void OnEnable()
    {
        if (_vitals != null)
            _vitals.Changed += Refresh;
    }

    void Start()
    {
        Refresh();
    }

    void OnDisable()
    {
        if (_vitals != null)
            _vitals.Changed -= Refresh;
    }

    void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void Update()
    {
        Tick(ref _health);
        Tick(ref _mana);
        Tick(ref _stamina);
    }

    void Refresh()
    {
        if (_vitals == null)
            return;

        Aim(ref _health, _vitals.Health, _vitals.MaxHealth);
        Aim(ref _mana, _vitals.Mana, _vitals.MaxMana);
        Aim(ref _stamina, _vitals.Stamina, _vitals.MaxStamina);
    }

    static void Aim(ref BarView bar, float current, float max)
    {
        bar.Target = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        if (bar.Label == null)
            return;

        int shown = Mathf.RoundToInt(current);
        if (shown == bar.ShownText)
            return;

        bar.ShownText = shown;
        bar.Label.SetText("{0}", shown);
    }

    static void Tick(ref BarView bar)
    {
        if (bar.Fill == null)
            return;

        if (Mathf.Abs(bar.Shown - bar.Target) < 0.001f)
        {
            if (bar.Shown != bar.Target)
            {
                bar.Shown = bar.Target;
                bar.Fill.sizeDelta = new Vector2(bar.InnerWidth * bar.Target, 0f);
            }

            return;
        }

        bar.Shown = Mathf.MoveTowards(bar.Shown, bar.Target, Time.unscaledDeltaTime * 2.8f);
        bar.Fill.sizeDelta = new Vector2(bar.InnerWidth * bar.Shown, 0f);
    }

    void Build()
    {
        _canvasObject = new GameObject("Status Hud");
        UiScreen.Mount(_canvasObject, 100);

        float y = MarginY;
        _health = BuildBar("Health", y, 312f, 28f, HealthColor, HealthShine, 2f);
        y += 28f + Gap;
        _mana = BuildBar("Mana", y, 176f, 22f, ManaColor, ManaShine, 1f);
        y += 22f + Gap;
        _stamina = BuildBar("Stamina", y, 248f, 24f, StaminaColor, StaminaShine, 2f);
    }

    BarView BuildBar(string name, float y, float width, float height, Color fill, Color shine, float shineHeight)
    {
        var root = CreateRect(name, _canvasObject.transform);
        AnchorTopLeft(root, MarginX, y, width, height);

        var frame = CreateImage("Frame", root, FrameColor);
        Stretch(frame.rectTransform);

        var rim = CreateImage("Rim", root, RimColor);
        var rimRect = rim.rectTransform;
        rimRect.anchorMin = new Vector2(0f, 1f);
        rimRect.anchorMax = new Vector2(1f, 1f);
        rimRect.pivot = new Vector2(0.5f, 1f);
        rimRect.anchoredPosition = Vector2.zero;
        rimRect.sizeDelta = new Vector2(0f, 2f);

        var track = CreateImage("Track", root, TrackColor);
        Stretch(track.rectTransform);
        track.rectTransform.offsetMin = new Vector2(Frame, Frame);
        track.rectTransform.offsetMax = new Vector2(-Frame, -Frame);

        var fillImage = CreateImage("Fill", track.rectTransform, fill);
        var fillRect = fillImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);

        var shineImage = CreateImage("Shine", fillRect, shine);
        var shineRect = shineImage.rectTransform;
        shineRect.anchorMin = new Vector2(0f, 1f);
        shineRect.anchorMax = new Vector2(1f, 1f);
        shineRect.pivot = new Vector2(0.5f, 1f);
        shineRect.anchoredPosition = Vector2.zero;
        shineRect.sizeDelta = new Vector2(0f, shineHeight);

        var label = CreateValue(root, width, height);

        return new BarView
        {
            Fill = fillRect,
            Label = label,
            InnerWidth = width - Frame * 2f,
            ShownText = -1
        };
    }

    static TMP_Text CreateValue(RectTransform bar, float width, float height)
    {
        var rect = CreateRect("Value", bar);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        return UiGlyphs.Place(rect.gameObject, string.Empty, NumberSize, TextAnchor.MiddleCenter, NumberColor, UiGlyphs.Wrap.Single, true);
    }

    static void AnchorTopLeft(RectTransform rect, float x, float yFromTop, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
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
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _pixel.hideFlags = HideFlags.HideAndDontSave;
        return _pixel;
    }
}
