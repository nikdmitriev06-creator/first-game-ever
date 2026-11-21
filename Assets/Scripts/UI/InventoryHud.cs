using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Terraria-style hotbar: one wood row of ten light-brown cells along the bottom.
/// The selected cell has a gold rim. I turns the outer rim gold and lets the mouse lift items.
/// </summary>
public class InventoryHud : MonoBehaviour
{
    const float Slot = 58f;
    const float Gap = 4f;
    const float Pad = 8f;
    const float Border = 5f;
    const float Bottom = 18f;
    const float IdleRim = 3f;
    const float SelectedRim = 4f;
    const float NumberSize = 13f;
    const float TextDetail = 3f;

    static readonly Color FrameShut = new Color(0.16f, 0.09f, 0.05f, 1f);
    static readonly Color FrameOpen = new Color(0.94f, 0.76f, 0.32f, 1f);
    static readonly Color Wood = new Color(0.45f, 0.29f, 0.16f, 1f);
    static readonly Color Lip = new Color(0.67f, 0.46f, 0.27f, 1f);
    static readonly Color Cell = new Color(0.93f, 0.80f, 0.62f, 1f);
    static readonly Color CellShine = new Color(0.99f, 0.92f, 0.80f, 1f);
    static readonly Color CellShade = new Color(0.78f, 0.62f, 0.42f, 1f);
    static readonly Color RimIdle = new Color(0.34f, 0.21f, 0.11f, 1f);
    static readonly Color RimSelected = new Color(0.98f, 0.84f, 0.28f, 1f);
    static readonly Color NumberColor = new Color(0.27f, 0.15f, 0.07f, 1f);
    static readonly Color NumberRim = new Color(0.97f, 0.91f, 0.78f, 0.95f);

    PlayerInventory _inventory;
    IWeapon[] _weapons;
    GameObject _canvasObject;
    RectTransform _bar;
    Image _outer;
    Image _cursorImage;
    RectTransform _cursorRect;
    Text _cursorCount;
    SlotView[] _slots;

    sealed class SlotView
    {
        public RectTransform Root;
        public Image Rim;
        public RectTransform Plate;
        public Image Icon;
        public Text Count;
    }

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _weapons = GetComponentsInChildren<IWeapon>(true);
        Build();
    }

    void OnEnable()
    {
        if (_inventory != null)
            _inventory.Changed += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        if (_inventory != null)
            _inventory.Changed -= Refresh;
    }

    void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void Update()
    {
        if (_inventory == null || !_inventory.IsOpen)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector2 screen = mouse.position.ReadValue();
        int slot = HitSlot(screen);
        if (slot >= 0)
            _inventory.ClickSlot(slot);
        else if (!InsideBar(screen))
            _inventory.ClickOutside();
    }

    void LateUpdate()
    {
        if (_cursorRect == null || _cursorImage == null || !_cursorImage.enabled)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        _cursorRect.position = new Vector3(screen.x, screen.y, 0f);
    }

    int HitSlot(Vector2 screen)
    {
        if (_slots == null)
            return -1;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(_slots[i].Root, screen, null))
                return i;
        }

        return -1;
    }

    bool InsideBar(Vector2 screen)
    {
        return _bar != null && RectTransformUtility.RectangleContainsScreenPoint(_bar, screen, null);
    }

    void Refresh()
    {
        if (_inventory == null || _slots == null)
            return;

        int selected = _inventory.Selected;
        for (int i = 0; i < _slots.Length; i++)
        {
            SlotView view = _slots[i];
            bool chosen = i == selected;
            view.Rim.color = chosen ? RimSelected : RimIdle;
            float rim = chosen ? SelectedRim : IdleRim;
            view.Plate.offsetMin = new Vector2(rim, rim);
            view.Plate.offsetMax = new Vector2(-rim, -rim);

            Sprite icon = IconFor(_inventory.SlotAt(i));
            view.Icon.sprite = icon;
            view.Icon.enabled = icon != null;

            int amount = _inventory.CountAt(i);
            if (view.Count != null)
            {
                view.Count.enabled = amount > 1;
                if (amount > 1)
                    view.Count.text = amount.ToString();
            }
        }

        if (_outer != null)
            _outer.color = _inventory.IsOpen ? FrameOpen : FrameShut;

        Sprite carried = IconFor(_inventory.CursorItem);
        bool showCursor = _inventory.IsOpen && carried != null;
        if (_cursorImage != null)
        {
            _cursorImage.sprite = carried;
            _cursorImage.enabled = showCursor;
        }

        if (_cursorCount != null)
        {
            int carriedCount = _inventory.CursorCount;
            _cursorCount.enabled = showCursor && carriedCount > 1;
            if (carriedCount > 1)
                _cursorCount.text = carriedCount.ToString();
        }
    }

    Sprite IconFor(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (id == ItemIds.Axe || id == ItemIds.Wood)
            return ItemArt.Icon(id);

        if (_weapons != null)
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                IWeapon weapon = _weapons[i];
                if (weapon != null && weapon.ItemId == id)
                    return weapon.IconSprite;
            }
        }

        return ItemArt.Icon(id);
    }

    void Build()
    {
        _canvasObject = new GameObject("Inventory Hud");

        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        float slotsWidth = Slot * PlayerInventory.SlotCount + Gap * (PlayerInventory.SlotCount - 1);
        float barWidth = slotsWidth + Pad * 2f;
        float barHeight = Slot + Pad * 2f;

        _bar = CreateRect("Bar", _canvasObject.transform);
        _bar.anchorMin = new Vector2(0.5f, 0f);
        _bar.anchorMax = new Vector2(0.5f, 0f);
        _bar.pivot = new Vector2(0.5f, 0f);
        _bar.anchoredPosition = new Vector2(0f, Bottom);
        _bar.sizeDelta = new Vector2(barWidth + Border * 2f, barHeight + Border * 2f);

        _outer = CreateImage("Outer", _bar, FrameShut);
        Stretch(_outer.rectTransform);

        var wood = CreateRect("Wood", _bar);
        Stretch(wood);
        wood.offsetMin = new Vector2(Border, Border);
        wood.offsetMax = new Vector2(-Border, -Border);

        var frame = CreateImage("Frame", wood, Wood);
        Stretch(frame.rectTransform);

        var lip = CreateImage("Lip", wood, Lip);
        var lipRect = lip.rectTransform;
        lipRect.anchorMin = new Vector2(0f, 1f);
        lipRect.anchorMax = new Vector2(1f, 1f);
        lipRect.pivot = new Vector2(0.5f, 1f);
        lipRect.anchoredPosition = Vector2.zero;
        lipRect.sizeDelta = new Vector2(0f, 4f);

        _slots = new SlotView[PlayerInventory.SlotCount];
        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = BuildSlot(wood, i);

        _cursorRect = CreateRect("Cursor Item", _canvasObject.transform);
        _cursorRect.anchorMin = new Vector2(0.5f, 0.5f);
        _cursorRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cursorRect.pivot = new Vector2(0.5f, 0.5f);
        _cursorRect.sizeDelta = new Vector2(46f, 46f);

        _cursorImage = _cursorRect.gameObject.AddComponent<Image>();
        _cursorImage.sprite = null;
        _cursorImage.color = Color.white;
        _cursorImage.preserveAspect = true;
        _cursorImage.raycastTarget = false;
        _cursorImage.enabled = false;

        _cursorCount = CreateStackCount(_cursorRect);

        Canvas.ForceUpdateCanvases();
    }

    SlotView BuildSlot(RectTransform parent, int index)
    {
        var root = CreateRect("Slot " + KeyLabel(index), parent);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;
        root.anchoredPosition = new Vector2(Pad + index * (Slot + Gap), Pad);
        root.sizeDelta = new Vector2(Slot, Slot);

        var rim = CreateImage("Rim", root, RimIdle);
        Stretch(rim.rectTransform);

        var plate = CreateRect("Plate", root);
        Stretch(plate);
        plate.offsetMin = new Vector2(IdleRim, IdleRim);
        plate.offsetMax = new Vector2(-IdleRim, -IdleRim);

        var fill = CreateImage("Fill", plate, Cell);
        Stretch(fill.rectTransform);

        var shine = CreateImage("Shine", plate, CellShine);
        var shineRect = shine.rectTransform;
        shineRect.anchorMin = new Vector2(0f, 1f);
        shineRect.anchorMax = new Vector2(1f, 1f);
        shineRect.pivot = new Vector2(0.5f, 1f);
        shineRect.anchoredPosition = Vector2.zero;
        shineRect.sizeDelta = new Vector2(0f, 3f);

        var shade = CreateImage("Shade", plate, CellShade);
        var shadeRect = shade.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(1f, 0f);
        shadeRect.pivot = new Vector2(0.5f, 0f);
        shadeRect.anchoredPosition = Vector2.zero;
        shadeRect.sizeDelta = new Vector2(0f, 4f);

        var icon = CreateImage("Icon", root, Color.white);
        Stretch(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(7f, 7f);
        icon.rectTransform.offsetMax = new Vector2(-7f, -7f);
        icon.preserveAspect = true;
        icon.sprite = null;
        icon.enabled = false;

        CreateNumber(root, KeyLabel(index));

        return new SlotView
        {
            Root = root,
            Rim = rim,
            Plate = plate,
            Icon = icon,
            Count = CreateStackCount(root)
        };
    }

    static void CreateNumber(RectTransform slot, string label)
    {
        float detail = TextDetail;
        var rect = CreateRect("Number", slot);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(3f, -2f);
        rect.sizeDelta = new Vector2(24f * detail, 16f * detail);
        rect.localScale = new Vector3(1f / detail, 1f / detail, 1f);

        var text = rect.gameObject.AddComponent<Text>();
        text.font = BuiltinFont();
        text.fontSize = Mathf.Max(1, Mathf.RoundToInt(NumberSize * detail));
        text.alignment = TextAnchor.UpperLeft;
        text.alignByGeometry = true;
        text.color = NumberColor;
        text.text = label;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;

        if (text.font != null)
            text.font.RequestCharactersInTexture("0123456789", text.fontSize, FontStyle.Normal);

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = NumberRim;
        outline.effectDistance = new Vector2(detail, -detail);
        outline.useGraphicAlpha = true;
    }

    static Text CreateStackCount(RectTransform parent)
    {
        float detail = TextDetail;
        var rect = CreateRect("Count", parent);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-2f, 1f);
        rect.sizeDelta = new Vector2(36f * detail, 16f * detail);
        rect.localScale = new Vector3(1f / detail, 1f / detail, 1f);

        var text = rect.gameObject.AddComponent<Text>();
        text.font = BuiltinFont();
        text.fontSize = Mathf.Max(1, Mathf.RoundToInt(14f * detail));
        text.alignment = TextAnchor.LowerRight;
        text.alignByGeometry = true;
        text.color = new Color(0.99f, 0.96f, 0.88f, 1f);
        text.text = "";
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.enabled = false;

        if (text.font != null)
            text.font.RequestCharactersInTexture("0123456789", text.fontSize, FontStyle.Normal);

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.08f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(detail, -detail);
        outline.useGraphicAlpha = true;
        return text;
    }

    static string KeyLabel(int index)
    {
        return index == 9 ? "0" : (index + 1).ToString();
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
