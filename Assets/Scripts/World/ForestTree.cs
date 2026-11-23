using UnityEngine;

/// <summary>
/// A forest tree. Chopping is data: wood material, three integrity, a wood drop, and a stump when it breaks.
/// The axe does not mention this class.
/// </summary>
public class ForestTree : MonoBehaviour, IBreakReaction
{
    const float AimSlop = 1.7f;

    static Sprite _stump;

    SpriteRenderer _renderer;
    float _height = 3.4f;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && _renderer.sprite != null)
        {
            float scale = Mathf.Abs(transform.lossyScale.y);
            _height = Mathf.Max(1.2f, _renderer.sprite.bounds.size.y * scale);
        }

        MaterialLibrary.Ensure();
        Destructible.Create(gameObject, MaterialLibrary.Wood, 3f, _height * 0.86f, AimSlop);
        LootDropper loot = GetComponent<LootDropper>();
        if (loot == null)
            loot = gameObject.AddComponent<LootDropper>();
        loot.Configure(ItemIds.Wood, 2, 4, 1.6f);
    }

    public void OnBroken(in Impact impact)
    {
        Transform parent = transform.parent;
        Vector3 at = transform.position;
        int layer = _renderer != null ? _renderer.sortingLayerID : 0;
        Material material = _renderer != null ? _renderer.sharedMaterial : null;
        LeaveStump(parent, at, layer, material);
        Destroy(gameObject);
    }

    static void LeaveStump(Transform parent, Vector3 at, int layer, Material material)
    {
        var go = new GameObject("Stump");
        if (parent != null)
            go.transform.SetParent(parent, true);
        go.transform.position = at;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = StumpSprite();
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = IsoSort.Order(at.y);
        BillboardSort sort = go.AddComponent<BillboardSort>();
        sort.Follow = true;
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static Sprite StumpSprite()
    {
        if (_stump != null)
            return _stump;

        const int width = 22;
        const int height = 14;
        var pixels = new Color32[width * height];
        Fill(pixels, width, 4, 2, 14, 6, new Color32(92, 56, 30, 255));
        Fill(pixels, width, 3, 7, 16, 5, new Color32(168, 124, 72, 255));
        Fill(pixels, width, 6, 8, 10, 3, new Color32(196, 154, 92, 255));
        Fill(pixels, width, 9, 9, 4, 2, new Color32(120, 78, 40, 255));

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;

        _stump = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.15f), 32f);
        _stump.hideFlags = HideFlags.HideAndDontSave;
        return _stump;
    }

    static void Fill(Color32[] pixels, int width, int x, int y, int w, int h, Color32 color)
    {
        for (int py = y; py < y + h; py++)
        {
            for (int px = x; px < x + w; px++)
            {
                if ((uint)px >= (uint)width || py < 0 || py * width >= pixels.Length)
                    continue;
                pixels[py * width + px] = color;
            }
        }
    }
}
