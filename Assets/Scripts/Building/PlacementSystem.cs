using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// While a placeable is selected, the cursor drags a ghost and a click builds it.
/// That click is claimed so the weapon in the other hand does not swing as well.
/// </summary>
[DefaultExecutionOrder(30)]
public class PlacementSystem : MonoBehaviour
{
    const float Clearance = 0.95f;

    PlayerInventory _inventory;
    PlayerController _player;
    SpriteRenderer _ghost;
    BillboardSort _ghostSort;
    GameObject _hintRoot;
    TMP_Text _hint;
    ItemDefinition _shown;

    public bool HoldsPlaceable
    {
        get
        {
            PlayerInventory bag = Bag();
            if (GameplayGate.MenuOpen || bag == null)
                return false;

            ItemDefinition item = ItemCatalog.Find(bag.SlotAt(bag.Selected));
            return item != null && item.IsPlaceable && bag.CountAt(bag.Selected) > 0;
        }
    }

    PlayerInventory Bag()
    {
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
        return _inventory;
    }

    /// <summary>
    /// Places the selected item when the rules allow it. A refusal spends nothing.
    /// </summary>
    public bool TryPlaceAt(Vector2 point)
    {
        PlayerInventory bag = Bag();
        if (bag == null)
            return false;

        ItemDefinition item = ItemCatalog.Find(bag.SlotAt(bag.Selected));
        if (item == null || !item.IsPlaceable || bag.CountAt(bag.Selected) <= 0)
            return false;

        if (PlacementRules.Evaluate(item, point, transform.position, Clearance) != PlaceReject.None)
            return false;

        if (!bag.TryConsume(item.Id, 1))
            return false;

        Transform parent = LocationContext.Active != null ? LocationContext.Active.transform : null;
        bool indoor = LocationContext.Active != null && LocationContext.Active.Kind == LocationKind.Indoor;
        PlaceableObject created = PlaceableObject.Create(item, point, parent, indoor, null, 0);
        if (created != null)
            return true;

        bag.TryAdd(item.Id, 1);
        return false;
    }

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _player = GetComponent<PlayerController>();
        BuildGhost();
        BuildHint();
    }

    void OnDestroy()
    {
        Retire(_ghost != null ? _ghost.gameObject : null);
        Retire(_hintRoot);
    }

    static void Retire(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }

    void Update()
    {
        if (!HoldsPlaceable)
        {
            Hide();
            return;
        }

        PlayerInventory bag = Bag();
        ItemDefinition item = ItemCatalog.Find(bag.SlotAt(bag.Selected));
        Vector2 feet = transform.position;
        Vector2 point = PlacementRules.Snap(CursorWorld(feet));
        PlaceReject reject = PlacementRules.Evaluate(item, point, feet, Clearance);
        ShowGhost(item, point, reject == PlaceReject.None);
        ShowHint(reject, item);

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        if (GameplayPointer.IsOverUi())
            return;

        GameplayGate.ClaimUse();
        if (reject != PlaceReject.None)
            return;

        TryPlaceAt(point);
    }

    void Hide()
    {
        if (_ghost != null)
            _ghost.enabled = false;
        if (_hintRoot != null)
            _hintRoot.SetActive(false);
        _shown = null;
    }

    void ShowGhost(ItemDefinition item, Vector2 point, bool allowed)
    {
        if (_ghost == null)
            return;

        if (_shown != item)
        {
            _ghost.sprite = item.PlacedSprite;
            _shown = item;
            Fit(item.PlacedSprite);
        }

        _ghost.enabled = _ghost.sprite != null;
        _ghost.transform.position = new Vector3(point.x, point.y, 0f);
        _ghost.color = allowed
            ? new Color(1f, 1f, 1f, 0.62f)
            : new Color(1f, 0.42f, 0.34f, 0.7f);
        if (_ghostSort != null)
            _ghostSort.LineY = point.y;
    }

    void ShowHint(PlaceReject reject, ItemDefinition item)
    {
        if (_hintRoot == null || _hint == null)
            return;

        if (reject == PlaceReject.None)
        {
            _hintRoot.SetActive(false);
            return;
        }

        _hint.text = Message(reject, item);
        _hintRoot.SetActive(true);
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Canvas canvas = _hint.canvas;
            float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
            Vector2 size = _hint.rectTransform.rect.size * scale;
            Vector2 topLeft = UiScreen.TooltipTopLeft(mouse.position.ReadValue(), size.x, size.y, Screen.width, Screen.height, 8f, 16f);
            _hint.rectTransform.position = topLeft;
        }
    }

    static string Message(PlaceReject reject, ItemDefinition item)
    {
        if (reject == PlaceReject.Zone && item != null && item.Placement == PlacementZone.Indoor)
            return UiGlyphs.Pick("Только внутри дома", "Indoors only");

        if (reject == PlaceReject.Zone)
            return UiGlyphs.Pick("Только у дома", "Only at home");

        return UiGlyphs.Pick("Нельзя поставить здесь", "Can't place that here");
    }

    void Fit(Sprite sprite)
    {
        if (sprite == null)
        {
            _ghost.transform.localScale = Vector3.one;
            return;
        }

        _ghost.transform.localScale = Vector3.one;
    }

    Vector2 CursorWorld(Vector2 feet)
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return feet;

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane plane = new Plane(-cam.transform.forward, new Vector3(feet.x, feet.y, 0f));
        if (!plane.Raycast(ray, out float enter))
            return feet;

        Vector3 world = ray.GetPoint(enter);
        return new Vector2(world.x, world.y);
    }

    void BuildGhost()
    {
        var go = new GameObject("Placement Ghost");
        _ghost = go.AddComponent<SpriteRenderer>();
        _ghost.sortingOrder = 12;
        _ghost.enabled = false;
        _ghostSort = go.AddComponent<BillboardSort>();

        if (_player != null)
        {
            SpriteRenderer body = _player.GetComponent<SpriteRenderer>();
            if (body != null)
            {
                _ghost.sortingLayerID = body.sortingLayerID;
                if (body.sharedMaterial != null)
                    _ghost.sharedMaterial = body.sharedMaterial;
            }
        }
    }

    void BuildHint()
    {
        _hintRoot = new GameObject("Placement Hint");
        UiScreen.Mount(_hintRoot, 210);

        var rect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(_hintRoot.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(460f, 40f);

        var back = new GameObject("Back", typeof(RectTransform));
        back.transform.SetParent(rect, false);
        var backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.offsetMin = new Vector2(-8f, -4f);
        backRect.offsetMax = new Vector2(8f, 4f);
        var backImage = back.AddComponent<Image>();
        backImage.color = new Color(0.08f, 0.05f, 0.03f, 0.88f);
        backImage.raycastTarget = false;

        _hint = UiGlyphs.Place(rect.gameObject, string.Empty, 22f, TextAnchor.MiddleLeft, new Color(0.98f, 0.9f, 0.78f, 1f), UiGlyphs.Wrap.Ellipsis, true);
        _hintRoot.SetActive(false);
    }
}
