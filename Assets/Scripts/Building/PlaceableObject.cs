using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A placed bench, chair, or later piece of furniture.
/// Strength and the dropped item come from the item definition.
/// </summary>
public class PlaceableObject : MonoBehaviour, IBreakReaction
{
    static readonly List<PlaceableObject> Alive = new List<PlaceableObject>();

    [SerializeField] string itemId;

    ItemDefinition _definition;
    Destructible _body;
    SpriteRenderer _renderer;
    BillboardSort _sort;
    float _footprint = 0.6f;
    bool _indoor;
    bool _registered;
    string _recordId;

    public int Health => _body != null ? Mathf.CeilToInt(_body.Integrity) : 0;
    public string ItemId => itemId;
    bool IsBroken => _body != null && _body.Integrity <= 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Alive.Clear();
    }

    public static PlaceableObject Create(
        ItemDefinition definition,
        Vector2 position,
        Transform parent,
        bool indoor,
        string savedId,
        int savedHealth)
    {
        if (definition == null)
            return null;

        GameObject go;
        if (definition.Prefab != null)
        {
            go = Instantiate(definition.Prefab, parent);
            go.name = definition.DisplayNameEnglish;
        }
        else
        {
            go = new GameObject(string.IsNullOrEmpty(definition.DisplayNameEnglish) ? definition.Id : definition.DisplayNameEnglish);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<PlaceableObject>();
            go.AddComponent<BillboardSort>();
        }

        PlaceableObject placeable = go.GetComponent<PlaceableObject>();
        if (placeable == null)
            placeable = go.AddComponent<PlaceableObject>();

        placeable.Activate(definition, position, indoor, savedId, savedHealth);
        return placeable;
    }

    public void BindId(string id)
    {
        itemId = id;
    }

    public static bool Overlaps(Vector2 point, float radius, PlaceableObject ignore = null)
    {
        float reach = Mathf.Max(0.2f, radius);
        for (int i = 0; i < Alive.Count; i++)
        {
            PlaceableObject other = Alive[i];
            if (other == null || other == ignore || other.IsBroken || !other.isActiveAndEnabled)
                continue;

            float need = (other._footprint + reach) * 0.72f;
            if (((Vector2)other.transform.position - point).sqrMagnitude < need * need)
                return true;
        }

        return false;
    }

    public static bool BlocksFeet(Vector2 feet)
    {
        for (int i = 0; i < Alive.Count; i++)
        {
            PlaceableObject other = Alive[i];
            if (other == null || other.IsBroken || !other.isActiveAndEnabled)
                continue;

            float need = other._footprint * 0.62f;
            if (((Vector2)other.transform.position - feet).sqrMagnitude < need * need)
                return true;
        }

        return false;
    }

    public void Activate(ItemDefinition definition, Vector2 position, bool indoor, string savedId, int savedHealth)
    {
        _definition = definition;
        if (definition != null)
            itemId = definition.Id;

        _footprint = definition != null ? definition.Footprint : 0.6f;
        _indoor = indoor;
        transform.position = new Vector3(position.x, position.y, 0f);

        ApplyVisual(definition);
        ApplyStation(definition);
        EnsureCollider();
        EnsureBody(definition, savedHealth);
        if (_sort != null)
        {
            _sort.Follow = true;
            _sort.LineY = position.y;
        }

        int shown = Health;
        if (_indoor)
        {
            if (string.IsNullOrEmpty(savedId))
                _recordId = PlacedFurniture.Remember(itemId, position, shown);
            else
            {
                _recordId = savedId;
                PlacedFurniture.Touch(_recordId, position, shown);
            }

            PlacedFurniture.MarkLive(_recordId);
            if (_body != null)
                _body.AssignId(_recordId);
        }

        Register();
    }

    void EnsureBody(ItemDefinition definition, int savedHealth)
    {
        if (definition != null && definition.Indestructible)
        {
            Destructible existing = GetComponent<Destructible>();
            if (existing != null)
                existing.enabled = false;
            return;
        }

        MaterialLibrary.Ensure();
        float hits = definition != null ? definition.Durability : 1f;
        _body = Destructible.Create(gameObject, MaterialLibrary.ForPlaceable(definition), hits, Mathf.Max(0.55f, _footprint), 0.95f);
        if (savedHealth > 0)
            _body.ApplySavedIntegrity(savedHealth);
        _body.IntegrityChanged -= OnIntegrityChanged;
        _body.IntegrityChanged += OnIntegrityChanged;

        LootDropper loot = GetComponent<LootDropper>();
        if (loot == null)
            loot = gameObject.AddComponent<LootDropper>();
        loot.Configure(itemId, 1, 1, 0.35f);
    }

    void OnIntegrityChanged()
    {
        if (!_indoor || string.IsNullOrEmpty(_recordId) || _body == null || _body.Integrity <= 0f)
            return;

        PlacedFurniture.Touch(_recordId, transform.position, Mathf.CeilToInt(_body.Integrity));
    }

    public void OnBroken(in Impact impact)
    {
        CraftingStation station = GetComponent<CraftingStation>();
        if (station != null)
            station.enabled = false;

        Collider2D body = GetComponent<Collider2D>();
        if (body != null)
            body.enabled = false;

        if (_indoor)
            PlacedFurniture.Forget(_recordId);

        if (Application.isPlaying && isActiveAndEnabled)
            StartCoroutine(BreakApart());
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _sort = GetComponent<BillboardSort>();
        if (!string.IsNullOrEmpty(itemId))
            ApplyVisual(ItemCatalog.Find(itemId));
    }

    void OnEnable()
    {
        Register();
    }

    void OnDisable()
    {
        Unregister();
        if (_indoor)
            PlacedFurniture.MarkGone(_recordId);
    }

    void OnDestroy()
    {
        Unregister();
        if (!_indoor || string.IsNullOrEmpty(_recordId))
            return;

        if (_body != null && _body.Integrity <= 0f)
            return;

        PlacedFurniture.MarkGone(_recordId);
        PlacedFurniture.Touch(_recordId, transform.position, Mathf.Max(1, Health));
    }

    void Register()
    {
        if (_registered)
            return;

        _registered = true;
        if (!Alive.Contains(this))
            Alive.Add(this);
    }

    void Unregister()
    {
        if (!_registered)
            return;

        _registered = false;
        Alive.Remove(this);
    }

    void ApplyVisual(ItemDefinition definition)
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<SpriteRenderer>();

        if (_sort == null)
            _sort = GetComponent<BillboardSort>();
        if (_sort == null)
            _sort = gameObject.AddComponent<BillboardSort>();

        Sprite sprite = definition != null ? definition.PlacedSprite : ItemArt.WorldSprite(itemId);
        if (sprite != null)
            _renderer.sprite = sprite;

        _renderer.sortingOrder = IsoSort.Order(transform.position.y);
        if (_sort != null)
            _sort.Follow = true;
        SpriteRenderer ground = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        if (ground != null)
        {
            _renderer.sortingLayerID = ground.sortingLayerID;
            if (ground.sharedMaterial != null)
                _renderer.sharedMaterial = ground.sharedMaterial;
        }

        EnsureShadow();
    }

    void EnsureShadow()
    {
        float width = Mathf.Max(0.8f, _footprint * 1.6f);
        Transform existing = transform.Find("Shadow");
        if (existing != null)
        {
            existing.localScale = new Vector3(width, width * 0.45f, 1f);
            return;
        }

        var shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        var shadow = shadowObject.AddComponent<SpriteRenderer>();
        shadow.sprite = ItemArt.Shadow();
        shadow.sortingOrder = IsoSort.FloorOrder + 1;
        shadow.color = new Color(1f, 1f, 1f, 0.8f);
        if (_renderer != null)
        {
            shadow.sortingLayerID = _renderer.sortingLayerID;
            if (_renderer.sharedMaterial != null)
                shadow.sharedMaterial = _renderer.sharedMaterial;
        }

        shadowObject.transform.localScale = new Vector3(width, width * 0.45f, 1f);
    }

    void EnsureCollider()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();

        box.isTrigger = true;
        box.size = new Vector2(_footprint * 1.35f, _footprint * 0.7f);
        box.offset = new Vector2(0f, _footprint * 0.28f);
    }

    void ApplyStation(ItemDefinition definition)
    {
        CraftingStation station = GetComponent<CraftingStation>();
        if (definition == null || !definition.IsStation)
        {
            if (station != null)
                station.enabled = false;
            return;
        }

        if (station == null)
            station = gameObject.AddComponent<CraftingStation>();

        station.enabled = true;
        station.Configure(definition.StationId, definition.StationRadius);
    }

    IEnumerator BreakApart()
    {
        Vector3 scale = transform.localScale;
        float t = 0f;
        const float life = 0.12f;
        while (t < life)
        {
            t += Time.deltaTime;
            float along = Mathf.Clamp01(t / life);
            transform.localScale = scale * Mathf.Lerp(1.08f, 0.15f, along);
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = 1f - along;
                _renderer.color = color;
            }

            yield return null;
        }

        Despawn();
    }

    void Despawn()
    {
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }
}
