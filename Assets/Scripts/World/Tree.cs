using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A forest tree that can be felled. Three chops with an axe drop wood and leave a stump.
/// Spawned with the level 1 forest, one component on each painted tree.
/// </summary>
public class Tree : MonoBehaviour
{
    const int ChopsToFell = 3;
    const float AimSlop = 1.7f;

    static readonly List<Tree> Alive = new List<Tree>();
    static Sprite _stump;

    SpriteRenderer _renderer;
    Vector3 _home;
    float _height = 3.4f;
    float _flash;
    float _shake;
    int _left = ChopsToFell;
    bool _felled;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _home = transform.localPosition;
        if (_renderer != null && _renderer.sprite != null)
        {
            float scale = Mathf.Abs(transform.lossyScale.y);
            _height = Mathf.Max(1.2f, _renderer.sprite.bounds.size.y * scale);
        }
    }

    void OnEnable()
    {
        if (!Alive.Contains(this))
            Alive.Add(this);
    }

    void OnDisable()
    {
        Alive.Remove(this);
    }

    void Update()
    {
        if (_flash <= 0f && _shake <= 0f)
            return;

        if (_flash > 0f)
        {
            _flash -= Time.deltaTime * 4.5f;
            if (_renderer != null)
            {
                float blend = Mathf.Clamp01(_flash);
                _renderer.color = Color.Lerp(Color.white, new Color(1f, 0.76f, 0.32f), blend);
            }
        }

        if (_shake > 0f)
        {
            _shake -= Time.deltaTime;
            if (_shake < 0f)
                _shake = 0f;
        }

        float kick = _shake > 0f ? Mathf.Sin(Time.time * 48f) * _shake * 0.16f : 0f;
        transform.localPosition = _home + new Vector3(kick, 0f, 0f);
    }

    /// <summary>
    /// The living tree whose trunk is in reach and whose crown is under the aim point.
    /// </summary>
    public static Tree FindAt(Vector2 feet, Vector2 aim, float reach)
    {
        Tree best = null;
        float bestScore = float.MaxValue;
        float reachSqr = reach * reach;

        for (int i = 0; i < Alive.Count; i++)
        {
            Tree tree = Alive[i];
            if (tree == null || tree._felled || !tree.isActiveAndEnabled)
                continue;

            Vector2 trunk = tree.transform.position;
            if ((feet - trunk).sqrMagnitude > reachSqr)
                continue;

            Vector2 crown = trunk + Vector2.up * (tree._height * 0.86f);
            float aimDist = DistanceToSegment(aim, trunk, crown);
            if (aimDist > AimSlop)
                continue;

            float score = aimDist + Vector2.Distance(feet, trunk) * 0.08f;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = tree;
        }

        return best;
    }

    public void Chop()
    {
        if (_felled)
            return;

        _left--;
        if (_left <= 0)
        {
            Fell();
            return;
        }

        _flash = 1f;
        _shake = 0.18f;
    }

    void Fell()
    {
        _felled = true;
        Alive.Remove(this);

        Transform parent = transform.parent;
        Vector3 at = transform.position;
        int order = _renderer != null ? _renderer.sortingOrder : 4;
        int layer = _renderer != null ? _renderer.sortingLayerID : 0;
        Material material = _renderer != null ? _renderer.sharedMaterial : null;

        ScatterWood(parent);
        LeaveStump(parent, at, order, layer, material);
        Destroy(gameObject);
    }

    void ScatterWood(Transform parent)
    {
        int pieces = Random.Range(2, 5);
        Vector2 origin = (Vector2)transform.position + Vector2.up * (_height * 0.45f);
        PlayArea area = parent != null ? parent.GetComponent<PlayArea>() : null;

        for (int i = 0; i < pieces; i++)
        {
            float turn = (i / (float)pieces) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
            float distance = Random.Range(0.75f, 1.6f);
            Vector2 land = (Vector2)transform.position + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn) * 0.62f) * distance;
            if (area != null)
                land = area.Clamp(land, 1.4f);

            WorldItem.Toss(ItemIds.Wood, 1, origin, land, parent, 0.08f);
        }
    }

    static void LeaveStump(Transform parent, Vector3 at, int order, int layer, Material material)
    {
        var go = new GameObject("Stump");
        if (parent != null)
            go.transform.SetParent(parent, true);
        go.transform.position = at;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = StumpSprite();
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = Mathf.Max(2, order - 1);
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

    static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 along = b - a;
        float length = along.sqrMagnitude;
        if (length < 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / length);
        return Vector2.Distance(point, a + along * t);
    }
}
