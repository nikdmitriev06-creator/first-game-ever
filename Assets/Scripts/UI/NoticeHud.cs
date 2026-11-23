using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One panel for every notice. The queue decides what is visible. This class only draws it.
/// </summary>
public class NoticeHud : MonoBehaviour
{
    const float Width = 420f;
    const float RowHeight = 40f;
    const float Gap = 8f;
    const float Margin = 24f;

    static readonly Color Panel = new Color(0.10f, 0.07f, 0.05f, 0.94f);
    static readonly Color Ink = new Color(0.97f, 0.93f, 0.84f, 1f);
    static readonly Color Good = new Color(0.62f, 0.82f, 0.46f, 1f);
    static readonly Color Warn = new Color(0.86f, 0.28f, 0.18f, 1f);
    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f, 1f);

    GameObject _canvasObject;
    RectTransform _list;
    readonly List<Row> _rows = new List<Row>();
    float _master = -1f;
    float _music = -1f;
    float _sfx = -1f;
    float _ambient = -1f;
    int _muted = -1;

    sealed class Row
    {
        public RectTransform Root;
        public Image Stripe;
        public TMP_Text Label;
    }

    void Awake()
    {
        Build();
    }

    void OnEnable()
    {
        Notices.Changed += Refresh;
        ItemInstance.Broke += OnBroke;
        ProgressionService.LocationAdvanced += OnLocation;
        CraftingSystem crafting = GetComponent<CraftingSystem>();
        if (crafting != null)
            crafting.RecipeOpened += OnRecipe;
        ProgressionService.PersistInterface = GameRoot.Active != null;
        Refresh();
    }

    void OnDisable()
    {
        Notices.Changed -= Refresh;
        ItemInstance.Broke -= OnBroke;
        ProgressionService.LocationAdvanced -= OnLocation;
        CraftingSystem crafting = GetComponent<CraftingSystem>();
        if (crafting != null)
            crafting.RecipeOpened -= OnRecipe;
        ProgressionService.PersistInterface = false;
    }

    void OnDestroy()
    {
        if (_canvasObject != null)
            Destroy(_canvasObject);
    }

    void Update()
    {
        Notices.Tick(Time.unscaledDeltaTime);
        WatchAudio();
    }

    void WatchAudio()
    {
        if (!ProgressionService.PersistInterface)
            return;

        int muted = SoundBus.Muted ? 1 : 0;
        if (_master == SoundBus.Master && _music == SoundBus.Music && _sfx == SoundBus.Sfx && _ambient == SoundBus.Ambient && _muted == muted)
            return;

        _master = SoundBus.Master;
        _music = SoundBus.Music;
        _sfx = SoundBus.Sfx;
        _ambient = SoundBus.Ambient;
        _muted = muted;
        ProgressionService.RememberAudio(_master, _music, _sfx, _ambient, muted == 1);
    }

    void OnBroke(string definitionId)
    {
        Notices.Post(NoticeKind.Broken, definitionId, 1, 1);
    }

    void OnLocation(string locationId, int level)
    {
        Notices.Post(NoticeKind.Location, locationId, level, 1);
    }

    void OnRecipe(string unlockId)
    {
        Notices.Post(NoticeKind.Recipe, unlockId, 1, 1);
    }

    void Refresh()
    {
        if (_list == null)
            return;

        int count = Notices.Board.VisibleCount;
        while (_rows.Count < count)
            _rows.Add(BuildRow(_rows.Count));

        for (int i = 0; i < _rows.Count; i++)
        {
            bool show = i < count;
            _rows[i].Root.gameObject.SetActive(show);
            if (!show)
                continue;

            NoticeEntry entry = Notices.Board.VisibleAt(i);
            _rows[i].Root.anchoredPosition = new Vector2(0f, -i * (RowHeight + Gap));
            _rows[i].Stripe.color = StripeFor(entry);
            string text = Format(entry);
            _rows[i].Label.text = text;
            _rows[i].Label.color = Ink;
            UiGlyphs.Prepare(_rows[i].Label, text);
        }

        _list.sizeDelta = new Vector2(Width, Mathf.Max(RowHeight, count * (RowHeight + Gap)));
    }

    public static string Format(NoticeEntry entry)
    {
        if (entry == null)
            return string.Empty;

        switch (entry.Kind)
        {
            case NoticeKind.Item:
                return Named(entry, "Получено: ", "Received: ");
            case NoticeKind.Crafted:
                return Named(entry, "Создано: ", "Crafted: ");
            case NoticeKind.Rare:
                return Named(entry, "Редкая находка: ", "Rare find: ");
            case NoticeKind.Chest:
                return UiGlyphs.Pick("Сундук открыт", "Chest opened");
            case NoticeKind.Recipe:
                return UiGlyphs.Pick("Новый рецепт", "New recipe");
            case NoticeKind.Location:
                return UiGlyphs.Pick("Уровень локации: ", "Location level: ") + Mathf.Max(1, entry.Amount);
            case NoticeKind.Mechanism:
                return UiGlyphs.Pick("Механизм: ", "Mechanism: ") + entry.Subject;
            case NoticeKind.Broken:
                return Named(entry, "Сломано: ", "Broken: ");
            case NoticeKind.Lack:
            case NoticeKind.Impossible:
                return Refusal(entry);
            default:
                return entry.Subject ?? string.Empty;
        }
    }

    static string Named(NoticeEntry entry, string russian, string english)
    {
        ItemDefinition item = ItemCatalog.Find(entry.Subject);
        string name = item != null
            ? UiGlyphs.Pick(item.DisplayName, item.DisplayNameEnglish)
            : entry.Subject;
        string line = UiGlyphs.Pick(russian, english) + name;
        if (entry.Amount > 1)
            line += " ×" + entry.Amount;
        return line;
    }

    static string Refusal(NoticeEntry entry)
    {
        string subject = entry.Subject ?? string.Empty;
        if (subject.StartsWith("station:"))
        {
            string stationId = subject.Substring("station:".Length);
            ItemDefinition station = ItemCatalog.Find(stationId);
            string name = station != null
                ? UiGlyphs.Pick(station.DisplayName, station.DisplayNameEnglish)
                : stationId;
            return UiGlyphs.Pick("Нет станка: ", "Need a station: ") + name;
        }

        if (subject.StartsWith("ingredients:"))
        {
            string recipeId = subject.Substring("ingredients:".Length);
            RecipeDefinition recipe = ItemCatalog.Active != null ? ItemCatalog.Active.FindRecipe(recipeId) : null;
            CraftingSystem crafting = GameRoot.Active != null && GameRoot.Active.Inventory != null
                ? GameRoot.Active.Inventory.GetComponent<CraftingSystem>()
                : null;
            if (recipe != null && crafting != null)
            {
                var gaps = new List<CraftGap>();
                crafting.CopyCosts(recipe, gaps);
                for (int i = 0; i < gaps.Count; i++)
                {
                    if (!gaps[i].Short)
                        continue;

                    ItemDefinition missing = ItemCatalog.Find(gaps[i].ItemId);
                    string name = missing != null
                        ? UiGlyphs.Pick(missing.DisplayName, missing.DisplayNameEnglish)
                        : gaps[i].ItemId;
                    return UiGlyphs.Pick("Не хватает: ", "Need: ") + name + " " + gaps[i].Have + "/" + gaps[i].Need;
                }
            }

            return UiGlyphs.Pick("Не хватает ресурсов", "Not enough materials");
        }

        if (subject == "space" || subject == "Space")
            return UiGlyphs.Pick("Нет места в сумке", "Inventory is full");
        if (subject == "unique" || subject == "Unique")
            return UiGlyphs.Pick("Эти предметы нельзя сложить", "These items cannot stack");
        if (subject == "Locked")
            return UiGlyphs.Pick("Рецепт ещё закрыт", "Recipe is locked");
        return UiGlyphs.Pick("Действие невозможно", "That cannot be done");
    }

    static Color StripeFor(NoticeEntry entry)
    {
        if (entry == null)
            return Gold;

        if (entry.Kind == NoticeKind.Rare || entry.Kind == NoticeKind.Item || entry.Kind == NoticeKind.Crafted || entry.Kind == NoticeKind.Broken)
        {
            ItemDefinition item = ItemCatalog.Find(entry.Subject);
            if (item != null && item.Rarity != ItemRarity.Common)
                return ItemRarityColors.For(item);
        }

        switch (entry.Kind)
        {
            case NoticeKind.Impossible:
            case NoticeKind.Lack:
            case NoticeKind.Broken:
                return Warn;
            case NoticeKind.Crafted:
            case NoticeKind.Item:
                return Good;
            default:
                return Gold;
        }
    }

    void Build()
    {
        _canvasObject = new GameObject("Notice Hud");
        UiScreen.Mount(_canvasObject, 180);

        _list = CreateRect("List", _canvasObject.transform);
        _list.anchorMin = new Vector2(0f, 0f);
        _list.anchorMax = new Vector2(0f, 0f);
        _list.pivot = new Vector2(0f, 0f);
        _list.anchoredPosition = new Vector2(Margin, Margin);
        _list.sizeDelta = new Vector2(Width, RowHeight);
    }

    Row BuildRow(int index)
    {
        var root = CreateRect("Notice " + index, _list);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(Width, RowHeight);

        var plate = CreateImage("Plate", root, Panel);
        Stretch(plate.rectTransform);

        var stripe = CreateImage("Stripe", root, Gold);
        stripe.rectTransform.anchorMin = new Vector2(0f, 0f);
        stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
        stripe.rectTransform.pivot = new Vector2(0f, 0.5f);
        stripe.rectTransform.sizeDelta = new Vector2(6f, 0f);
        stripe.rectTransform.anchoredPosition = Vector2.zero;

        var label = CreateRect("Label", root);
        Stretch(label);
        label.offsetMin = new Vector2(14f, 2f);
        label.offsetMax = new Vector2(-8f, -2f);
        TMP_Text text = UiGlyphs.Place(label.gameObject, string.Empty, 18f, TextAnchor.MiddleLeft, Ink, UiGlyphs.Wrap.Ellipsis);
        return new Row { Root = root, Stripe = stripe, Label = text };
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
        image.sprite = White();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Sprite _white;

    static Sprite White()
    {
        if (_white != null)
            return _white;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _white.hideFlags = HideFlags.HideAndDontSave;
        return _white;
    }
}
