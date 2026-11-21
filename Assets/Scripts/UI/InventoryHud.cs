using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hotbar along the bottom, and a full inventory with a crafting list while I is held open.
/// Clicks move stacks. The craft buttons ask CraftingSystem and do not edit the slots themselves.
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
    const float BagSlot = 68f;
    const float BagGap = 8f;
    const float BagPad = 16f;
    const float TitleHeight = 40f;
    const int BagColumns = 5;
    const float CraftWidth = 392f;
    const float CraftHeight = 528f;
    const float RowHeight = 108f;

    static readonly Color FrameShut = new Color(0.16f, 0.09f, 0.05f, 1f);
    static readonly Color FrameOpen = new Color(0.94f, 0.76f, 0.32f, 1f);
    static readonly Color Wood = new Color(0.45f, 0.29f, 0.16f, 1f);
    static readonly Color Lip = new Color(0.67f, 0.46f, 0.27f, 1f);
    static readonly Color Cell = new Color(0.93f, 0.80f, 0.62f, 1f);
    static readonly Color CellHot = new Color(1f, 0.93f, 0.78f, 1f);
    static readonly Color CellShine = new Color(0.99f, 0.92f, 0.80f, 1f);
    static readonly Color CellShade = new Color(0.78f, 0.62f, 0.42f, 1f);
    static readonly Color RimIdle = new Color(0.34f, 0.21f, 0.11f, 1f);
    static readonly Color RimSelected = new Color(0.98f, 0.84f, 0.28f, 1f);
    static readonly Color NumberColor = new Color(0.27f, 0.15f, 0.07f, 1f);
    static readonly Color NumberRim = new Color(0.97f, 0.91f, 0.78f, 0.95f);
    static readonly Color Panel = new Color(0.10f, 0.07f, 0.05f, 0.94f);
    static readonly Color DimColor = new Color(0.04f, 0.03f, 0.02f, 0.58f);
    static readonly Color Ink = new Color(0.97f, 0.93f, 0.84f, 1f);
    static readonly Color Short = new Color(0.86f, 0.28f, 0.18f, 1f);
    static readonly Color ButtonOn = new Color(0.34f, 0.50f, 0.26f, 1f);
    static readonly Color ButtonOnHot = new Color(0.46f, 0.64f, 0.32f, 1f);
    static readonly Color ButtonOff = new Color(0.28f, 0.20f, 0.14f, 1f);

    PlayerInventory _inventory;
    CraftingSystem _crafting;
    IWeapon[] _weapons;
    GameObject _canvasObject;
    Canvas _canvas;
    GameObject _menu;
    CanvasGroup _menuGroup;
    RectTransform _bar;
    RectTransform _bagWindow;
    RectTransform _craftWindow;
    RectTransform _craftContent;
    Image _outer;
    Image _cursorImage;
    RectTransform _cursorRect;
    Text _cursorCount;
    SlotView[] _hotbar;
    SlotView[] _bag;
    readonly List<CraftRow> _rows = new List<CraftRow>();
    readonly List<RecipeDefinition> _visible = new List<RecipeDefinition>();
    float _scroll;

    sealed class SlotView
    {
        public RectTransform Root;
        public Image Rim;
        public Image Fill;
        public RectTransform Plate;
        public Image Icon;
        public Text Count;
    }

    sealed class CraftRow
    {
        public RecipeDefinition Recipe;
        public RectTransform Button;
        public Image ButtonFill;
        public bool Ready;
    }

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _crafting = GetComponent<CraftingSystem>();
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
        ClearRows();
        if (_canvasObject != null)
            Retire(_canvasObject);
    }

    void Update()
    {
        if (_inventory == null || !_inventory.IsOpen)
            return;

        if (_menuGroup != null)
            _menuGroup.alpha = Mathf.MoveTowards(_menuGroup.alpha, 1f, Time.unscaledDeltaTime * 6.5f);

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        ScrollCraft(mouse, screen);
        PaintHover(screen);

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        int craft = HitCraft(screen);
        if (craft >= 0)
        {
            CraftRow row = _rows[craft];
            if (row.Ready && _crafting != null)
                _crafting.TryCraft(row.Recipe);
            return;
        }

        int slot = HitSlot(screen);
        if (slot >= 0)
        {
            _inventory.ClickSlot(slot);
            return;
        }

        if (!InsideChrome(screen))
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

    void ScrollCraft(Mouse mouse, Vector2 screen)
    {
        if (_craftContent == null || !Inside(_craftWindow, screen))
            return;

        float wheel = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) < 0.01f)
            return;

        float max = Mathf.Max(0f, _rows.Count * RowHeight - (CraftHeight - TitleHeight - 36f));
        _scroll = Mathf.Clamp(_scroll - wheel * 22f, 0f, max);
        _craftContent.anchoredPosition = new Vector2(0f, _scroll);
    }

    int HitSlot(Vector2 screen)
    {
        int bag = Hit(_bag, screen);
        if (bag >= 0)
            return bag;

        return Hit(_hotbar, screen);
    }

    static int Hit(SlotView[] slots, Vector2 screen)
    {
        if (slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && RectTransformUtility.RectangleContainsScreenPoint(slots[i].Root, screen, null))
                return i;
        }

        return -1;
    }

    int HitCraft(Vector2 screen)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Button != null && RectTransformUtility.RectangleContainsScreenPoint(_rows[i].Button, screen, null))
                return i;
        }

        return -1;
    }

    bool InsideChrome(Vector2 screen)
    {
        return Inside(_bar, screen) || Inside(_bagWindow, screen) || Inside(_craftWindow, screen);
    }

    static bool Inside(RectTransform rect, Vector2 screen)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
    }

    void PaintHover(Vector2 screen)
    {
        int hot = HitSlot(screen);
        PaintSlotHover(_hotbar, hot);
        PaintSlotHover(_bag, hot);

        int craft = HitCraft(screen);
        for (int i = 0; i < _rows.Count; i++)
        {
            CraftRow row = _rows[i];
            if (row.ButtonFill == null)
                continue;

            bool over = i == craft;
            if (!row.Ready)
                row.ButtonFill.color = over ? new Color(0.36f, 0.26f, 0.18f, 1f) : ButtonOff;
            else
                row.ButtonFill.color = over ? ButtonOnHot : ButtonOn;
        }
    }

    static void PaintSlotHover(SlotView[] slots, int hot)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].Fill != null)
                slots[i].Fill.color = i == hot ? CellHot : Cell;
        }
    }

    void Refresh()
    {
        if (_inventory == null)
            return;

        bool open = _inventory.IsOpen;
        if (_menu != null && _menu.activeSelf != open)
        {
            _menu.SetActive(open);
            if (open && _menuGroup != null)
                _menuGroup.alpha = 0.35f;
            if (!open)
                _scroll = 0f;
        }

        if (_canvas != null)
            _canvas.sortingOrder = open ? 190 : 130;

        PaintSlots(_hotbar);
        PaintSlots(_bag);

        if (_outer != null)
            _outer.color = open ? FrameOpen : FrameShut;

        Sprite carried = IconFor(_inventory.CursorItem);
        bool showCursor = open && carried != null;
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

        if (open)
            RebuildCraft();
    }

    void PaintSlots(SlotView[] slots)
    {
        if (slots == null || _inventory == null)
            return;

        int selected = _inventory.Selected;
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView view = slots[i];
            if (view == null)
                continue;

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
    }

    void RebuildCraft()
    {
        ClearRows();
        if (_crafting == null || _craftContent == null)
            return;

        _crafting.CollectVisible(_visible);
        for (int i = 0; i < _visible.Count; i++)
            _rows.Add(BuildCraftRow(_visible[i], i));

        float height = Mathf.Max(CraftHeight - TitleHeight - 28f, _rows.Count * RowHeight);
        _craftContent.sizeDelta = new Vector2(0f, height);
        _craftContent.anchoredPosition = new Vector2(0f, _scroll);
    }

    void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Button != null)
                Retire(_rows[i].Button.transform.parent.gameObject);
        }

        _rows.Clear();
    }

    CraftRow BuildCraftRow(RecipeDefinition recipe, int index)
    {
        var root = CreateRect("Recipe " + index, _craftContent);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(-16f, RowHeight - 8f);
        root.anchoredPosition = new Vector2(0f, -6f - index * RowHeight);

        var plate = CreateImage("Plate", root, new Color(0.20f, 0.13f, 0.08f, 0.94f));
        Stretch(plate.rectTransform);

        ItemDefinition result = ItemCatalog.Find(recipe.ResultId);
        Sprite icon = result != null ? result.IconSprite : ItemArt.Icon(recipe.ResultId);
        var picture = CreateImage("Icon", root, Color.white);
        picture.rectTransform.anchorMin = new Vector2(0f, 1f);
        picture.rectTransform.anchorMax = new Vector2(0f, 1f);
        picture.rectTransform.pivot = new Vector2(0f, 1f);
        picture.rectTransform.anchoredPosition = new Vector2(10f, -10f);
        picture.rectTransform.sizeDelta = new Vector2(52f, 52f);
        picture.preserveAspect = true;
        picture.sprite = icon;
        picture.enabled = icon != null;

        string name = result != null
            ? UiGlyphs.Pick(result.DisplayName, result.DisplayNameEnglish)
            : UiGlyphs.Pick(recipe.DisplayName, recipe.DisplayNameEnglish);
        if (recipe.ResultCount > 1)
            name += " x" + recipe.ResultCount;

        Text title = CreatePlain(root, "Name", name, 20, TextAnchor.UpperLeft, Ink);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(72f, -12f);
        title.rectTransform.sizeDelta = new Vector2(-150f, 28f);

        Ingredient[] cost = recipe.Ingredients ?? System.Array.Empty<Ingredient>();
        bool shortOn = false;
        for (int i = 0; i < cost.Length; i++)
        {
            int have = _inventory != null ? _inventory.CountOf(cost[i].ItemId) : 0;
            int need = Mathf.Max(1, cost[i].Count);
            bool missing = have < need;
            if (missing)
                shortOn = true;

            ItemDefinition ingredient = ItemCatalog.Find(cost[i].ItemId);
            Sprite ingredientIcon = ingredient != null ? ingredient.IconSprite : ItemArt.Icon(cost[i].ItemId);
            var chip = CreateImage("Cost", root, missing ? new Color(1f, 0.62f, 0.52f, 1f) : Color.white);
            chip.rectTransform.anchorMin = new Vector2(0f, 0f);
            chip.rectTransform.anchorMax = new Vector2(0f, 0f);
            chip.rectTransform.pivot = new Vector2(0f, 0f);
            chip.rectTransform.anchoredPosition = new Vector2(72f + i * 92f, 12f);
            chip.rectTransform.sizeDelta = new Vector2(28f, 28f);
            chip.preserveAspect = true;
            chip.sprite = ingredientIcon;
            chip.enabled = ingredientIcon != null;

            Text amount = CreatePlain(root, "Need", have + "/" + need, 16, TextAnchor.MiddleLeft, missing ? Short : Ink);
            amount.rectTransform.anchorMin = new Vector2(0f, 0f);
            amount.rectTransform.anchorMax = new Vector2(0f, 0f);
            amount.rectTransform.pivot = new Vector2(0f, 0f);
            amount.rectTransform.anchoredPosition = new Vector2(104f + i * 92f, 14f);
            amount.rectTransform.sizeDelta = new Vector2(70f, 24f);
        }

        CraftDeny deny = _crafting != null ? _crafting.DenyReason(recipe) : CraftDeny.Ingredients;
        bool ready = deny == CraftDeny.None;
        string caption = ready
            ? UiGlyphs.Pick("Создать", "Craft")
            : deny == CraftDeny.Space
                ? UiGlyphs.Pick("Нет места", "No room")
                : UiGlyphs.Pick("Не хватает", "Need more");

        var button = CreateRect("Craft", root);
        button.anchorMin = new Vector2(1f, 0f);
        button.anchorMax = new Vector2(1f, 0f);
        button.pivot = new Vector2(1f, 0f);
        button.anchoredPosition = new Vector2(-10f, 12f);
        button.sizeDelta = new Vector2(124f, 36f);

        var buttonFill = CreateImage("Fill", button, ready ? ButtonOn : ButtonOff);
        Stretch(buttonFill.rectTransform);
        Text buttonLabel = CreatePlain(button, "Label", caption, 16, TextAnchor.MiddleCenter, Ink);
        Stretch(buttonLabel.rectTransform);

        if (shortOn)
            plate.color = new Color(0.28f, 0.12f, 0.09f, 0.94f);

        return new CraftRow
        {
            Recipe = recipe,
            Button = button,
            ButtonFill = buttonFill,
            Ready = ready
        };
    }

    Sprite IconFor(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        ItemDefinition defined = ItemCatalog.Find(id);
        if (defined != null && (defined.Category == ItemCategory.Placeable || defined.Category == ItemCategory.Resource || id == ItemIds.Axe))
            return defined.IconSprite;

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

        _canvas = _canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 130;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildMenu();
        BuildHotbar();

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

    void BuildMenu()
    {
        _menu = new GameObject("Menu", typeof(RectTransform));
        _menu.transform.SetParent(_canvasObject.transform, false);
        Stretch(_menu.GetComponent<RectTransform>());
        _menuGroup = _menu.AddComponent<CanvasGroup>();
        _menuGroup.alpha = 0f;
        _menuGroup.blocksRaycasts = false;
        _menuGroup.interactable = false;

        var dim = CreateImage("Dim", _menu.transform, DimColor);
        Stretch(dim.rectTransform);

        _craftWindow = BuildCraftPanel(_menu.transform);
        _bagWindow = BuildBag(_menu.transform);
        _menu.SetActive(false);
    }

    RectTransform BuildCraftPanel(Transform parent)
    {
        var window = CreateRect("Craft", parent);
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = new Vector2(-400f, 28f);
        window.sizeDelta = new Vector2(CraftWidth, CraftHeight);

        var outer = CreateImage("Outer", window, new Color(0.22f, 0.13f, 0.07f, 1f));
        Stretch(outer.rectTransform);

        var wood = CreateRect("Wood", window);
        Stretch(wood);
        wood.offsetMin = new Vector2(Border, Border);
        wood.offsetMax = new Vector2(-Border, -Border);
        var frame = CreateImage("Frame", wood, Panel);
        Stretch(frame.rectTransform);

        Text title = CreatePlain(wood, "Title", UiGlyphs.Pick("Создание", "Crafting"), 24, TextAnchor.MiddleLeft, Ink);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(16f, -4f);
        title.rectTransform.sizeDelta = new Vector2(-24f, TitleHeight);

        var lip = CreateImage("Lip", wood, Lip);
        var lipRect = lip.rectTransform;
        lipRect.anchorMin = new Vector2(0f, 1f);
        lipRect.anchorMax = new Vector2(1f, 1f);
        lipRect.pivot = new Vector2(0.5f, 1f);
        lipRect.anchoredPosition = new Vector2(0f, -TitleHeight);
        lipRect.sizeDelta = new Vector2(0f, 3f);

        var viewport = CreateRect("Viewport", wood);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(8f, 10f);
        viewport.offsetMax = new Vector2(-8f, -(TitleHeight + 8f));
        viewport.gameObject.AddComponent<RectMask2D>();

        _craftContent = CreateRect("Content", viewport);
        _craftContent.anchorMin = new Vector2(0f, 1f);
        _craftContent.anchorMax = new Vector2(1f, 1f);
        _craftContent.pivot = new Vector2(0.5f, 1f);
        _craftContent.anchoredPosition = Vector2.zero;
        _craftContent.sizeDelta = new Vector2(0f, 400f);
        return window;
    }

    RectTransform BuildBag(Transform parent)
    {
        int rows = Mathf.CeilToInt(PlayerInventory.SlotCount / (float)BagColumns);
        float width = BagPad * 2f + BagSlot * BagColumns + BagGap * (BagColumns - 1) + Border * 2f;
        float height = Border * 2f + TitleHeight + BagPad * 2f + BagSlot * rows + BagGap * (rows - 1);

        var window = CreateRect("Bag", parent);
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = new Vector2(210f, 36f);
        window.sizeDelta = new Vector2(width, height);

        var outer = CreateImage("Outer", window, new Color(0.22f, 0.13f, 0.07f, 1f));
        Stretch(outer.rectTransform);

        var wood = CreateRect("Wood", window);
        Stretch(wood);
        wood.offsetMin = new Vector2(Border, Border);
        wood.offsetMax = new Vector2(-Border, -Border);
        var frame = CreateImage("Frame", wood, Wood);
        Stretch(frame.rectTransform);

        Text title = CreatePlain(wood, "Title", UiGlyphs.Pick("Инвентарь", "Inventory"), 24, TextAnchor.MiddleLeft, new Color(0.98f, 0.94f, 0.86f, 1f));
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(14f, -2f);
        title.rectTransform.sizeDelta = new Vector2(-20f, TitleHeight);

        Text hint = CreatePlain(wood, "Hint", "I", 16, TextAnchor.MiddleRight, new Color(0.93f, 0.84f, 0.62f, 1f));
        hint.rectTransform.anchorMin = new Vector2(1f, 1f);
        hint.rectTransform.anchorMax = new Vector2(1f, 1f);
        hint.rectTransform.pivot = new Vector2(1f, 1f);
        hint.rectTransform.anchoredPosition = new Vector2(-14f, -6f);
        hint.rectTransform.sizeDelta = new Vector2(48f, 28f);

        var lip = CreateImage("Lip", wood, Lip);
        var lipRect = lip.rectTransform;
        lipRect.anchorMin = new Vector2(0f, 1f);
        lipRect.anchorMax = new Vector2(1f, 1f);
        lipRect.pivot = new Vector2(0.5f, 1f);
        lipRect.anchoredPosition = new Vector2(0f, -TitleHeight);
        lipRect.sizeDelta = new Vector2(0f, 4f);

        _bag = new SlotView[PlayerInventory.SlotCount];
        for (int i = 0; i < _bag.Length; i++)
        {
            int column = i % BagColumns;
            int row = i / BagColumns;
            float x = BagPad + column * (BagSlot + BagGap);
            float y = BagPad + (rows - 1 - row) * (BagSlot + BagGap);
            _bag[i] = BuildSlot(wood, i, new Vector2(x, y), BagSlot);
        }

        return window;
    }

    void BuildHotbar()
    {
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

        _hotbar = new SlotView[PlayerInventory.SlotCount];
        for (int i = 0; i < _hotbar.Length; i++)
        {
            var position = new Vector2(Pad + i * (Slot + Gap), Pad);
            _hotbar[i] = BuildSlot(wood, i, position, Slot);
        }
    }

    SlotView BuildSlot(RectTransform parent, int index, Vector2 position, float size)
    {
        var root = CreateRect("Slot " + KeyLabel(index), parent);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;
        root.anchoredPosition = position;
        root.sizeDelta = new Vector2(size, size);

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
            Fill = fill,
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
        text.font = UiGlyphs.Font;
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
        text.font = UiGlyphs.Font;
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

    static Text CreatePlain(RectTransform parent, string name, string value, int size, TextAnchor anchor, Color color)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = UiGlyphs.Font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (text.font != null)
            text.font.RequestCharactersInTexture(value, size, FontStyle.Normal);
        return text;
    }

    static void Retire(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    static string KeyLabel(int index)
    {
        return index == 9 ? "0" : (index + 1).ToString();
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
