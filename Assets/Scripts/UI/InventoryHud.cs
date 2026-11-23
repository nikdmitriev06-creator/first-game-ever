using System.Collections.Generic;
using TMPro;
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
    const float Bottom = 36f;
    const float IdleRim = 3f;
    const float SelectedRim = 5f;
    const float NumberSize = 15f;
    const float WindowGap = 20f;
    const float BagSlot = 68f;
    const float BagGap = 8f;
    const float BagPad = 16f;
    const float TitleHeight = 40f;
    const int BagColumns = 5;
    const float CraftWidth = 392f;
    const float CraftHeight = 560f;
    const float RowHeight = 124f;
    const float FilterHeight = 34f;

    static readonly Color FrameShut = new Color(0.16f, 0.09f, 0.05f, 1f);
    static readonly Color FrameOpen = new Color(0.94f, 0.76f, 0.32f, 1f);
    static readonly Color Wood = new Color(0.45f, 0.29f, 0.16f, 1f);
    static readonly Color Lip = new Color(0.67f, 0.46f, 0.27f, 1f);
    static readonly Color Cell = new Color(0.93f, 0.80f, 0.62f, 1f);
    static readonly Color CellHot = new Color(1f, 0.93f, 0.78f, 1f);
    static readonly Color CellSelected = new Color(0.99f, 0.86f, 0.48f, 1f);
    static readonly Color CellShine = new Color(0.99f, 0.92f, 0.80f, 1f);
    static readonly Color CellShade = new Color(0.78f, 0.62f, 0.42f, 1f);
    static readonly Color RimIdle = new Color(0.34f, 0.21f, 0.11f, 1f);
    static readonly Color RimSelected = new Color(0.98f, 0.84f, 0.28f, 1f);
    static readonly Color NumberColor = new Color(0.27f, 0.15f, 0.07f, 1f);
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
    TMP_Text _cursorCount;
    SlotView[] _hotbar;
    SlotView[] _bag;
    readonly List<CraftRow> _rows = new List<CraftRow>();
    readonly List<RecipeDefinition> _visible = new List<RecipeDefinition>();
    readonly List<RecipeDefinition> _sorted = new List<RecipeDefinition>();
    readonly List<CraftGap> _gaps = new List<CraftGap>();
    readonly ItemTooltip _tooltip = new ItemTooltip();
    RectTransform[] _filters;
    Image[] _filterFill;
    float _scroll;
    int _filter = -1;
    int _pressSlot = -1;
    int _pressCraft = -1;
    int _pressFilter = -1;
    int _seenMove;
    int _savedSlot = -1;
    Vector2 _pressPos;
    bool _dragged;
    bool _slotReady;

    static readonly int[] FilterCategories =
    {
        -1,
        (int)ItemCategory.Resource,
        (int)ItemCategory.Tool,
        (int)ItemCategory.Weapon,
        (int)ItemCategory.Placeable
    };

    sealed class SlotView
    {
        public int Index;
        public RectTransform Root;
        public Image Rim;
        public Image Fill;
        public RectTransform Plate;
        public Image Icon;
        public TMP_Text Count;
        public Image Wear;
        public Image Hand;
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

    void Start()
    {
        if (_inventory == null || GameRoot.Active == null)
            return;

        _inventory.SelectSlot(ProgressionService.HotbarSlot);
        _savedSlot = _inventory.Selected;
        _slotReady = true;
    }

    void Update()
    {
        if (_inventory == null)
            return;

        WatchMoves();
        bool open = _inventory.IsOpen;
        if (open && _menuGroup != null)
            _menuGroup.alpha = Mathf.MoveTowards(_menuGroup.alpha, 1f, Time.unscaledDeltaTime * 6.5f);

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        if (open)
            ScrollCraft(mouse, screen);
        PaintHover(screen, open);
        Pointer(mouse, screen, open);
    }

    void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        if (_cursorRect != null && _cursorImage != null && _cursorImage.enabled)
            _cursorRect.position = new Vector3(screen.x, screen.y, 0f);

        PlaceTooltip(screen);
    }

    void WatchMoves()
    {
        if (_inventory.MoveSerial == _seenMove)
            return;

        _seenMove = _inventory.MoveSerial;
        if (_inventory.LastMove == SlotMove.RefusedSpace)
            Notices.Post(NoticeKind.Impossible, "space", 1, 0);
        else if (_inventory.LastMove == SlotMove.RefusedUnique)
            Notices.Post(NoticeKind.Impossible, "unique", 1, 0);
    }

    void Pointer(Mouse mouse, Vector2 screen, bool open)
    {
        if (mouse.rightButton.wasPressedThisFrame)
        {
            int slot = HitSlot(screen, open);
            if (slot >= 0)
                _inventory.SplitSlot(slot);
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _pressFilter = open ? HitFilter(screen) : -1;
            _pressCraft = open && _pressFilter < 0 ? HitCraft(screen) : -1;
            _pressSlot = _pressFilter < 0 && _pressCraft < 0 ? HitSlot(screen, open) : -1;
            _pressPos = screen;
            _dragged = false;
        }

        if (mouse.leftButton.isPressed && !_dragged && _pressSlot >= 0 && (screen - _pressPos).sqrMagnitude > 64f)
        {
            _dragged = true;
            if (_inventory.CursorCount <= 0)
                _inventory.ClickSlot(_pressSlot);
        }

        if (!mouse.leftButton.wasReleasedThisFrame)
            return;

        if (_dragged)
        {
            int slot = HitSlot(screen, open);
            if (slot >= 0)
                _inventory.ClickSlot(slot);
            else if (_inventory.CursorCount > 0)
                _inventory.ClickOutside();
        }
        else if (_pressFilter >= 0 && HitFilter(screen) == _pressFilter)
        {
            _filter = FilterCategories[_pressFilter];
            _scroll = 0f;
            RebuildCraft();
        }
        else if (_pressCraft >= 0 && HitCraft(screen) == _pressCraft)
        {
            AskCraft(_rows[_pressCraft]);
        }
        else if (_pressSlot >= 0 && HitSlot(screen, open) == _pressSlot)
        {
            if (open)
                _inventory.ClickSlot(_pressSlot);
            else if (_pressSlot < PlayerInventory.HotbarCount)
                _inventory.SelectSlot(_pressSlot);
        }
        else if (open && _pressSlot < 0 && _pressCraft < 0 && _pressFilter < 0 && !InsideChrome(screen))
        {
            _inventory.ClickOutside();
        }

        _pressSlot = -1;
        _pressCraft = -1;
        _pressFilter = -1;
        _dragged = false;
    }

    void AskCraft(CraftRow row)
    {
        if (row == null || row.Recipe == null || _crafting == null)
            return;

        CraftDeny deny = _crafting.DenyReason(row.Recipe);
        if (deny == CraftDeny.None)
        {
            if (_crafting.TryCraft(row.Recipe))
                Notices.Post(NoticeKind.Crafted, row.Recipe.ResultId, row.Recipe.ResultCount, 0);
            return;
        }

        if (deny == CraftDeny.Station)
            Notices.Post(NoticeKind.Impossible, "station:" + row.Recipe.StationId, 1, 0);
        else if (deny == CraftDeny.Ingredients)
            Notices.Post(NoticeKind.Impossible, "ingredients:" + row.Recipe.Id, 1, 0);
        else if (deny == CraftDeny.Space)
            Notices.Post(NoticeKind.Impossible, "space", 1, 0);
        else
            Notices.Post(NoticeKind.Impossible, deny.ToString(), 1, 0);
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

    int HitSlot(Vector2 screen, bool open)
    {
        if (open)
        {
            int bag = Hit(_bag, screen);
            if (bag >= 0)
                return bag;
        }

        return Hit(_hotbar, screen);
    }

    int HitFilter(Vector2 screen)
    {
        if (_filters == null)
            return -1;

        for (int i = 0; i < _filters.Length; i++)
        {
            if (_filters[i] != null && RectTransformUtility.RectangleContainsScreenPoint(_filters[i], screen, null))
                return i;
        }

        return -1;
    }

    static int Hit(SlotView[] slots, Vector2 screen)
    {
        if (slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && RectTransformUtility.RectangleContainsScreenPoint(slots[i].Root, screen, null))
                return slots[i].Index;
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

    void PaintHover(Vector2 screen, bool open)
    {
        int hot = HitSlot(screen, open);
        PaintSlotHover(_hotbar, hot);
        PaintSlotHover(_bag, hot);
        PaintFilters();

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

    void PaintSlotHover(SlotView[] slots, int hot)
    {
        if (slots == null || _inventory == null)
            return;

        int selected = _inventory.Selected;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].Fill == null)
                continue;

            if (slots[i].Index == hot)
                slots[i].Fill.color = CellHot;
            else if (slots[i].Index == selected)
                slots[i].Fill.color = CellSelected;
            else
                slots[i].Fill.color = Cell;
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
        bool showCursor = carried != null && (open || _inventory.CursorCount > 0);
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

        if (_slotReady && ProgressionService.PersistInterface && _inventory.Selected != _savedSlot)
        {
            _savedSlot = _inventory.Selected;
            ProgressionService.RememberHotbar(_savedSlot);
        }
    }

    void PlaceTooltip(Vector2 screen)
    {
        if (_inventory == null)
            return;

        _tooltip.Ensure(_canvasObject.transform);
        bool open = _inventory.IsOpen;
        int slot = HitSlot(screen, open);
        if (slot >= 0)
        {
            ItemInstance stack = _inventory.InstanceAt(slot);
            if (stack == null)
                _tooltip.ShowEmpty(screen);
            else
                _tooltip.Show(ItemCatalog.Find(stack.DefinitionId), stack, screen);
            return;
        }

        if (open)
        {
            ItemInstance selected = _inventory.SelectedInstance;
            if (selected != null)
            {
                _tooltip.Show(ItemCatalog.Find(selected.DefinitionId), selected, screen);
                return;
            }
        }

        _tooltip.Hide();
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

            bool chosen = view.Index == selected;
            view.Rim.color = chosen ? RimSelected : RimIdle;
            float rim = chosen ? SelectedRim : IdleRim;
            view.Plate.offsetMin = new Vector2(rim, rim);
            view.Plate.offsetMax = new Vector2(-rim, -rim);

            string slotId = _inventory.SlotAt(view.Index);
            Sprite icon = IconFor(slotId);
            view.Icon.sprite = icon;
            view.Icon.enabled = icon != null;
            view.Icon.color = ItemRarityColors.For(ItemCatalog.Find(slotId));

            int amount = _inventory.CountAt(view.Index);
            if (view.Count != null)
            {
                view.Count.enabled = amount > 1;
                if (amount > 1)
                    view.Count.text = amount.ToString();
            }

            ItemInstance stack = _inventory.InstanceAt(view.Index);
            ItemDefinition defined = ItemCatalog.Find(slotId);
            bool worn = stack != null && stack.TracksDurability && defined != null && defined.WearCapacity > 0;
            if (view.Wear != null)
            {
                view.Wear.enabled = worn;
                if (worn)
                {
                    float ratio = Mathf.Clamp01(stack.Durability / (float)defined.WearCapacity);
                    float width = Mathf.Max(6f, (view.Root.sizeDelta.x - 10f) * ratio);
                    view.Wear.rectTransform.sizeDelta = new Vector2(width, 5f);
                    view.Wear.color = ratio <= 0.25f ? Short : new Color(0.85f, 0.72f, 0.28f, 1f);
                }
            }

            if (view.Hand != null)
                view.Hand.enabled = chosen && view.Index < PlayerInventory.HotbarCount && !string.IsNullOrEmpty(slotId);
        }
    }

    void RebuildCraft()
    {
        ClearRows();
        if (_crafting == null || _craftContent == null)
            return;

        _crafting.CollectKnown(_visible);
        _sorted.Clear();
        for (int i = 0; i < _visible.Count; i++)
        {
            RecipeDefinition recipe = _visible[i];
            ItemDefinition result = ItemCatalog.Find(recipe.ResultId);
            if (_filter >= 0 && (result == null || (int)result.Category != _filter))
                continue;

            _sorted.Add(recipe);
        }

        _sorted.Sort(CompareRecipes);
        for (int i = 0; i < _sorted.Count; i++)
            _rows.Add(BuildCraftRow(_sorted[i], i));

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

        TMP_Text title = CreatePlain(root, "Name", name, 20, TextAnchor.UpperLeft, Ink, UiGlyphs.Wrap.Ellipsis);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(72f, -12f);
        title.rectTransform.sizeDelta = new Vector2(-150f, 28f);

        _gaps.Clear();
        if (_crafting != null)
            _crafting.CopyCosts(recipe, _gaps);

        bool shortOn = false;
        for (int i = 0; i < _gaps.Count; i++)
        {
            CraftGap gap = _gaps[i];
            if (gap.Short)
                shortOn = true;

            ItemDefinition ingredient = ItemCatalog.Find(gap.ItemId);
            Sprite ingredientIcon = ingredient != null ? ingredient.IconSprite : ItemArt.Icon(gap.ItemId);
            var chip = CreateImage("Cost", root, gap.Short ? new Color(1f, 0.62f, 0.52f, 1f) : Color.white);
            chip.rectTransform.anchorMin = new Vector2(0f, 0f);
            chip.rectTransform.anchorMax = new Vector2(0f, 0f);
            chip.rectTransform.pivot = new Vector2(0f, 0f);
            chip.rectTransform.anchoredPosition = new Vector2(72f + i * 92f, 12f);
            chip.rectTransform.sizeDelta = new Vector2(28f, 28f);
            chip.preserveAspect = true;
            chip.sprite = ingredientIcon;
            chip.enabled = ingredientIcon != null;

            TMP_Text amount = CreatePlain(root, "Need", gap.Have + "/" + gap.Need, 16, TextAnchor.MiddleLeft, gap.Short ? Short : Ink);
            amount.rectTransform.anchorMin = new Vector2(0f, 0f);
            amount.rectTransform.anchorMax = new Vector2(0f, 0f);
            amount.rectTransform.pivot = new Vector2(0f, 0f);
            amount.rectTransform.anchoredPosition = new Vector2(104f + i * 92f, 14f);
            amount.rectTransform.sizeDelta = new Vector2(70f, 24f);
        }

        CraftDeny deny = _crafting != null ? _crafting.DenyReason(recipe) : CraftDeny.Ingredients;
        bool ready = deny == CraftDeny.None;
        string caption = CraftCaption(deny);
        if (recipe.NeedsStation)
        {
            ItemDefinition station = ItemCatalog.Find(recipe.StationId);
            string stationName = station != null
                ? UiGlyphs.Pick(station.DisplayName, station.DisplayNameEnglish)
                : recipe.StationId;
            TMP_Text stationLine = CreatePlain(root, "Station", UiGlyphs.Pick("Станок: ", "Station: ") + stationName, 15, TextAnchor.UpperLeft, deny == CraftDeny.Station ? Short : Ink, UiGlyphs.Wrap.Ellipsis);
            stationLine.rectTransform.anchorMin = new Vector2(0f, 1f);
            stationLine.rectTransform.anchorMax = new Vector2(1f, 1f);
            stationLine.rectTransform.pivot = new Vector2(0f, 1f);
            stationLine.rectTransform.anchoredPosition = new Vector2(72f, -40f);
            stationLine.rectTransform.sizeDelta = new Vector2(-90f, 22f);
        }

        var button = CreateRect("Craft", root);
        button.anchorMin = new Vector2(1f, 0f);
        button.anchorMax = new Vector2(1f, 0f);
        button.pivot = new Vector2(1f, 0f);
        button.anchoredPosition = new Vector2(-10f, 12f);
        button.sizeDelta = new Vector2(124f, 36f);

        var buttonFill = CreateImage("Fill", button, ready ? ButtonOn : ButtonOff);
        Stretch(buttonFill.rectTransform);
        TMP_Text buttonLabel = CreatePlain(button, "Label", caption, 16, TextAnchor.MiddleCenter, Ink, UiGlyphs.Wrap.Ellipsis, true);
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
        if (defined != null && (defined.Category == ItemCategory.Placeable || defined.Category == ItemCategory.Resource || defined.Category == ItemCategory.Tool))
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
        _canvas = UiScreen.Mount(_canvasObject, 130);

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

        var stage = CreateRect("Stage", _menu.transform);
        stage.anchorMin = new Vector2(0.5f, 0.5f);
        stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.pivot = new Vector2(0.5f, 0.5f);
        stage.anchoredPosition = new Vector2(0f, 28f);

        _craftWindow = BuildCraftPanel(stage);
        _bagWindow = BuildBag(stage);
        Vector2 footprint = OpenedFootprint();
        stage.sizeDelta = footprint;

        _craftWindow.anchorMin = new Vector2(0f, 0.5f);
        _craftWindow.anchorMax = new Vector2(0f, 0.5f);
        _craftWindow.pivot = new Vector2(0f, 0.5f);
        _craftWindow.anchoredPosition = Vector2.zero;

        _bagWindow.anchorMin = new Vector2(1f, 0.5f);
        _bagWindow.anchorMax = new Vector2(1f, 0.5f);
        _bagWindow.pivot = new Vector2(1f, 0.5f);
        _bagWindow.anchoredPosition = Vector2.zero;

        var fit = stage.gameObject.AddComponent<UiScaleToFit>();
        fit.Margin = 36f;
        fit.MinScale = 0.7f;
        fit.Apply();
        _menu.SetActive(false);
    }

    public static Vector2 OpenedFootprint()
    {
        Vector2 bag = BagSize();
        return new Vector2(CraftWidth + WindowGap + bag.x, Mathf.Max(CraftHeight, bag.y));
    }

    public static Vector2 HotbarFootprint()
    {
        return new Vector2(HotbarOuterWidth(), HotbarOuterHeight());
    }

    static Vector2 BagSize()
    {
        int columns = PlayerInventory.HotbarCount;
        int rows = PlayerInventory.Rows - 1;
        float slotsW = BagSlot * columns + BagGap * (columns - 1);
        float slotsH = BagSlot * rows + BagGap * (rows - 1);
        return new Vector2(slotsW + BagPad * 2f + Border * 2f, Border * 2f + TitleHeight + BagPad + slotsH + BagPad);
    }

    RectTransform BuildCraftPanel(Transform parent)
    {
        var window = CreateRect("Craft", parent);
        window.anchorMin = new Vector2(0f, 0.5f);
        window.anchorMax = new Vector2(0f, 0.5f);
        window.pivot = new Vector2(0f, 0.5f);
        window.anchoredPosition = Vector2.zero;
        window.sizeDelta = new Vector2(CraftWidth, CraftHeight);

        var outer = CreateImage("Outer", window, new Color(0.22f, 0.13f, 0.07f, 1f));
        Stretch(outer.rectTransform);

        var wood = CreateRect("Wood", window);
        Stretch(wood);
        wood.offsetMin = new Vector2(Border, Border);
        wood.offsetMax = new Vector2(-Border, -Border);
        var frame = CreateImage("Frame", wood, Panel);
        Stretch(frame.rectTransform);

        TMP_Text title = CreatePlain(wood, "Title", UiGlyphs.Pick("Создание", "Crafting"), 24, TextAnchor.MiddleLeft, Ink, UiGlyphs.Wrap.Ellipsis);
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
        viewport.offsetMax = new Vector2(-8f, -(TitleHeight + FilterHeight + 8f));
        BuildFilters(wood);
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
        int columns = PlayerInventory.HotbarCount;
        int extra = PlayerInventory.SlotCount - PlayerInventory.HotbarCount;
        Vector2 size = BagSize();

        var window = CreateRect("Bag", parent);
        window.anchorMin = new Vector2(1f, 0.5f);
        window.anchorMax = new Vector2(1f, 0.5f);
        window.pivot = new Vector2(1f, 0.5f);
        window.anchoredPosition = Vector2.zero;
        window.sizeDelta = size;

        var outer = CreateImage("Outer", window, new Color(0.22f, 0.13f, 0.07f, 1f));
        Stretch(outer.rectTransform);

        var wood = CreateRect("Wood", window);
        Stretch(wood);
        wood.offsetMin = new Vector2(Border, Border);
        wood.offsetMax = new Vector2(-Border, -Border);
        var frame = CreateImage("Frame", wood, Wood);
        Stretch(frame.rectTransform);

        TMP_Text title = CreatePlain(wood, "Title", UiGlyphs.Pick("Инвентарь", "Inventory"), 24, TextAnchor.MiddleLeft, new Color(0.98f, 0.94f, 0.86f, 1f), UiGlyphs.Wrap.Ellipsis);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(14f, -2f);
        title.rectTransform.sizeDelta = new Vector2(-72f, TitleHeight);

        TMP_Text hint = CreatePlain(wood, "Hint", "I", 16, TextAnchor.MiddleRight, new Color(0.93f, 0.84f, 0.62f, 1f));
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

        _bag = new SlotView[extra];
        for (int i = 0; i < _bag.Length; i++)
        {
            int column = i % columns;
            int band = i / columns;
            float x = BagPad + column * (BagSlot + BagGap);
            float y = BagPad + band * (BagSlot + BagGap);
            int index = PlayerInventory.HotbarCount + band * columns + column;
            _bag[i] = BuildSlot(wood, index, new Vector2(x, y), BagSlot, false);
        }

        return window;
    }

    void BuildHotbar()
    {
        _bar = CreateRect("Bar", _canvasObject.transform);
        _bar.anchorMin = new Vector2(0.5f, 0f);
        _bar.anchorMax = new Vector2(0.5f, 0f);
        _bar.pivot = new Vector2(0.5f, 0f);
        _bar.anchoredPosition = new Vector2(0f, Bottom);
        _bar.sizeDelta = new Vector2(HotbarOuterWidth(), HotbarOuterHeight());

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

        _hotbar = new SlotView[PlayerInventory.HotbarCount];
        for (int i = 0; i < _hotbar.Length; i++)
        {
            var position = new Vector2(Pad + i * (Slot + Gap), Pad);
            _hotbar[i] = BuildSlot(wood, i, position, Slot, true);
        }
    }

    static float HotbarOuterWidth()
    {
        float slots = Slot * PlayerInventory.HotbarCount + Gap * (PlayerInventory.HotbarCount - 1);
        return slots + Pad * 2f + Border * 2f;
    }

    static float HotbarOuterHeight()
    {
        return Slot + Pad * 2f + Border * 2f;
    }

    SlotView BuildSlot(RectTransform parent, int index, Vector2 position, float size, bool numbered)
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

        if (numbered)
            CreateNumber(root, KeyLabel(index));

        return new SlotView
        {
            Index = index,
            Root = root,
            Rim = rim,
            Fill = fill,
            Plate = plate,
            Icon = icon,
            Count = CreateStackCount(root),
            Wear = CreateWear(root),
            Hand = CreateHand(root)
        };
    }

    static Image CreateWear(RectTransform slot)
    {
        var image = CreateImage("Wear", slot, new Color(0.85f, 0.72f, 0.28f, 1f));
        image.rectTransform.anchorMin = new Vector2(0f, 0f);
        image.rectTransform.anchorMax = new Vector2(0f, 0f);
        image.rectTransform.pivot = new Vector2(0f, 0f);
        image.rectTransform.anchoredPosition = new Vector2(5f, 4f);
        image.rectTransform.sizeDelta = new Vector2(6f, 5f);
        image.enabled = false;
        return image;
    }

    static Image CreateHand(RectTransform slot)
    {
        var image = CreateImage("Hand", slot, RimSelected);
        image.rectTransform.anchorMin = new Vector2(1f, 1f);
        image.rectTransform.anchorMax = new Vector2(1f, 1f);
        image.rectTransform.pivot = new Vector2(1f, 1f);
        image.rectTransform.anchoredPosition = new Vector2(-4f, -4f);
        image.rectTransform.sizeDelta = new Vector2(8f, 8f);
        image.enabled = false;
        return image;
    }

    void BuildFilters(RectTransform parent)
    {
        string[] labels =
        {
            UiGlyphs.Pick("Все", "All"),
            UiGlyphs.Pick("Ресурс", "Resource"),
            UiGlyphs.Pick("Инстр.", "Tool"),
            UiGlyphs.Pick("Оружие", "Weapon"),
            UiGlyphs.Pick("Мебель", "Place")
        };

        _filters = new RectTransform[labels.Length];
        _filterFill = new Image[labels.Length];
        float width = (CraftWidth - Border * 2f - 24f) / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            var rect = CreateRect("Filter " + i, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(8f + i * width, -(TitleHeight + 4f));
            rect.sizeDelta = new Vector2(width - 4f, FilterHeight - 6f);
            var fill = CreateImage("Fill", rect, ButtonOff);
            Stretch(fill.rectTransform);
            TMP_Text label = CreatePlain(rect, "Label", labels[i], 14, TextAnchor.MiddleCenter, Ink, UiGlyphs.Wrap.Ellipsis, true);
            Stretch(label.rectTransform);
            _filters[i] = rect;
            _filterFill[i] = fill;
        }

        PaintFilters();
    }

    void PaintFilters()
    {
        if (_filterFill == null)
            return;

        for (int i = 0; i < _filterFill.Length; i++)
        {
            if (_filterFill[i] == null)
                continue;

            bool on = FilterCategories[i] == _filter;
            _filterFill[i].color = on ? new Color(0.46f, 0.34f, 0.16f, 1f) : ButtonOff;
        }
    }

    int CompareRecipes(RecipeDefinition left, RecipeDefinition right)
    {
        int rank = Rank(left).CompareTo(Rank(right));
        if (rank != 0)
            return rank;

        return string.CompareOrdinal(RecipeName(left), RecipeName(right));
    }

    int Rank(RecipeDefinition recipe)
    {
        CraftDeny deny = _crafting != null ? _crafting.DenyReason(recipe) : CraftDeny.Locked;
        int availability = deny == CraftDeny.None ? 0 : deny == CraftDeny.Ingredients ? 1 : deny == CraftDeny.Station ? 2 : 3;
        ItemDefinition result = ItemCatalog.Find(recipe.ResultId);
        int category = result != null ? (int)result.Category : 9;
        return availability * 10 + category;
    }

    static string RecipeName(RecipeDefinition recipe)
    {
        ItemDefinition result = ItemCatalog.Find(recipe.ResultId);
        if (result != null)
            return UiGlyphs.Pick(result.DisplayName, result.DisplayNameEnglish);

        return UiGlyphs.Pick(recipe.DisplayName, recipe.DisplayNameEnglish);
    }

    static string CraftCaption(CraftDeny deny)
    {
        switch (deny)
        {
            case CraftDeny.None: return UiGlyphs.Pick("Создать", "Craft");
            case CraftDeny.Station: return UiGlyphs.Pick("Нет станка", "No station");
            case CraftDeny.Space: return UiGlyphs.Pick("Нет места", "No room");
            case CraftDeny.Locked: return UiGlyphs.Pick("Закрыто", "Locked");
            default: return UiGlyphs.Pick("Не хватает", "Need more");
        }
    }

    static void CreateNumber(RectTransform slot, string label)
    {
        var rect = CreateRect("Number", slot);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(3f, -2f);
        rect.sizeDelta = new Vector2(24f, 16f);

        UiGlyphs.Place(rect.gameObject, label, NumberSize, TextAnchor.UpperLeft, NumberColor);
    }

    static TMP_Text CreateStackCount(RectTransform parent)
    {
        var rect = CreateRect("Count", parent);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-3f, 2f);
        rect.sizeDelta = new Vector2(48f, 20f);

        TMP_Text text = UiGlyphs.Place(rect.gameObject, string.Empty, 16f, TextAnchor.LowerRight, new Color(0.99f, 0.96f, 0.88f, 1f), UiGlyphs.Wrap.Single, true);
        text.enabled = false;
        return text;
    }

    static TMP_Text CreatePlain(RectTransform parent, string name, string value, int size, TextAnchor anchor, Color color, UiGlyphs.Wrap wrap = UiGlyphs.Wrap.Single, bool fit = false)
    {
        var rect = CreateRect(name, parent);
        return UiGlyphs.Place(rect.gameObject, value, size, anchor, color, wrap, false, fit);
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
