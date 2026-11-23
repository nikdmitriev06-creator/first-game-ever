using UnityEngine;

/// <summary>
/// One stack lying on the ground. It hops out of a toss, then waits.
/// Walking onto it puts the stack into the hotbar. A full hotbar leaves it where it fell.
/// </summary>
public class WorldItem : MonoBehaviour
{
    const float FallTime = 0.32f;
    const float PickupRadius = 1.15f;
    const float WorldHeight = 0.62f;

    string _id;
    int _count;
    ItemInstance _payload;
    float _born;
    float _ready;
    Vector2 _from;
    Vector2 _land;
    SpriteRenderer _renderer;
    Transform _shadow;
    PlayerInventory _inventory;

    public void Relocate(Vector2 land, Transform parent)
    {
        _from = land;
        _land = land;
        _born = Time.time - FallTime - 0.05f;
        _ready = Time.time;
        transform.SetParent(parent, true);
        transform.position = new Vector3(land.x, land.y, 0f);
        if (_shadow != null)
            _shadow.position = new Vector3(land.x, land.y, 0f);
    }

    public string StackId => _id;
    public int StackCount => _count;

    public static WorldItem Toss(string id, int count, Vector2 from, Vector2 land, Transform parent, float pickupDelay)
    {
        if (string.IsNullOrEmpty(id) || count <= 0)
            return null;

        var go = new GameObject("Dropped " + id);
        if (parent != null)
            go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(from.x, from.y, 0f);

        var item = go.AddComponent<WorldItem>();
        item.Begin(id, count, from, land, parent, pickupDelay);
        return item;
    }

    public void Attach(ItemInstance instance)
    {
        if (instance == null || instance.Quantity <= 0)
            return;

        _payload = instance;
        _id = instance.DefinitionId;
        _count = instance.Quantity;
    }

    /// <summary>
    /// Moves as much of the stack as the bag can hold. A full bag leaves the rest on the ground.
    /// </summary>
    public void CollectFrom(PlayerInventory inventory)
    {
        if (inventory == null || _count <= 0 || string.IsNullOrEmpty(_id))
            return;

        if (_payload != null)
        {
            int before = _payload.Quantity;
            int left = inventory.TryAccept(_payload);
            int gained = before - left;
            if (gained <= 0)
            {
                Notices.Post(NoticeKind.Impossible, "space", 1, 0);
                return;
            }

            if (left <= 0)
            {
                _payload = null;
                _count = 0;
                NotePickup(gained);
                Despawn();
                return;
            }

            _count = _payload.Quantity;
            NotePickup(gained);
            return;
        }

        int took = inventory.TryAdd(_id, _count);
        if (took <= 0)
        {
            Notices.Post(NoticeKind.Impossible, "space", 1, 0);
            return;
        }

        _count -= took;
        NotePickup(took);
        if (_count <= 0)
            Despawn();
    }

    void Begin(string id, int count, Vector2 from, Vector2 land, Transform parent, float pickupDelay)
    {
        _id = id;
        _count = count;
        _from = from;
        _land = land;
        _born = Time.time;
        _ready = Time.time + Mathf.Max(FallTime, pickupDelay);

        ItemDefinition defined = ItemCatalog.Find(id);
        Sprite sprite = defined != null ? defined.IconSprite : ItemArt.Icon(id);
        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = sprite;
        _renderer.sortingOrder = IsoSort.Order(land.y);
        Fit(sprite);

        SpriteRenderer ground = parent != null ? parent.GetComponent<SpriteRenderer>() : null;
        if (ground != null)
        {
            _renderer.sortingLayerID = ground.sortingLayerID;
            if (ground.sharedMaterial != null)
                _renderer.sharedMaterial = ground.sharedMaterial;
        }

        var shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform, true);
        shadowObject.transform.position = new Vector3(land.x, land.y, 0f);
        var shadow = shadowObject.AddComponent<SpriteRenderer>();
        shadow.sprite = ItemArt.Shadow();
        shadow.sortingLayerID = _renderer.sortingLayerID;
        shadow.sortingOrder = IsoSort.Order(land.y, -1);
        if (ground != null && ground.sharedMaterial != null)
            shadow.sharedMaterial = ground.sharedMaterial;
        _shadow = shadowObject.transform;
        PlaceShadow(0.45f);
    }

    void Update()
    {
        float along = Mathf.Clamp01((Time.time - _born) / FallTime);
        float eased = along * along * (3f - 2f * along);
        float hop = Mathf.Sin(along * Mathf.PI) * 0.85f;
        Vector2 flat = Vector2.Lerp(_from, _land, eased);
        transform.position = new Vector3(flat.x, flat.y + hop, 0f);
        bool falling = along < 1f || Time.time < _ready;
        if (!falling)
        {
            float bob = Mathf.Sin(Time.time * 3.2f) * 0.045f;
            transform.position = new Vector3(_land.x, _land.y + bob, 0f);
        }

        PlaceShadow(Mathf.Lerp(0.4f, 1f, along));
        if (!falling)
            TryPickup();
    }

    void PlaceShadow(float spread)
    {
        if (_shadow == null)
            return;

        _shadow.position = new Vector3(_land.x, _land.y, 0f);
        float parent = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        float want = 1.05f * Mathf.Clamp(spread, 0.25f, 1f);
        float size = want / (parent * 0.5f);
        _shadow.localScale = new Vector3(size, size * 0.55f, 1f);
    }

    void TryPickup()
    {
        if (_inventory == null && GameRoot.Active != null)
            _inventory = GameRoot.Active.Inventory;
        if (_inventory == null || _count <= 0)
            return;

        Vector2 feet = _inventory.transform.position;
        if ((feet - _land).sqrMagnitude > PickupRadius * PickupRadius)
            return;

        CollectFrom(_inventory);
    }

    void Despawn()
    {
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }

    void NotePickup(int gained)
    {
        ItemDefinition defined = ItemCatalog.Find(_id);
        bool rare = defined != null && defined.Rarity >= ItemRarity.Rare;
        Fx.Play(rare ? FxEvent.Rare : FxEvent.Pickup, transform.position);
        Notices.Post(rare ? NoticeKind.Rare : NoticeKind.Item, _id, gained, rare ? 1 : 0);
    }

    void Fit(Sprite sprite)
    {
        if (sprite == null)
        {
            transform.localScale = Vector3.one;
            return;
        }

        float pixels = sprite.rect.height / Mathf.Max(1f, sprite.pixelsPerUnit);
        if (pixels < 0.01f)
        {
            transform.localScale = Vector3.one;
            return;
        }

        float scale = WorldHeight / pixels;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
