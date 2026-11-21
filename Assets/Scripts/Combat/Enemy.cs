using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared enemy body: health, reach, and being hit by the player.
/// A specific mob decides how it moves. Stats stay on <see cref="EnemyStats"/> so a new mob does not fork the numbers.
/// </summary>
public class Enemy : MonoBehaviour
{
    // Opaque ink of the player sheet. The pivot sits on the feet, so the body stands on the transform.
    const float PlayerWidthPixels = 63f;
    const float PlayerHeightPixels = 99f;

    const float BarWidth = 1.35f;
    const float BarHeight = 0.16f;
    const float BarGap = 0.18f;
    const float BarInset = 0.03f;

    static readonly Color BarTrack = new Color(0.08f, 0.07f, 0.06f, 1f);
    static readonly Color BarFillColor = new Color(0.74f, 0.15f, 0.10f, 1f);
    static readonly List<Enemy> Alive = new List<Enemy>();

    [SerializeField] EnemyStats stats = new EnemyStats();

    float _halfW = 0.4f;
    float _halfH = 0.3f;
    float _attackWait;
    int _swordSwing;

    LevitatingSword _sword;
    Transform _barRoot;
    Transform _barFill;

    protected PlayerController Player { get; private set; }
    protected PlayerVitals Vitals { get; private set; }
    protected PlayArea Area { get; private set; }

    public EnemyStats Stats => stats;
    public float Health { get; private set; }
    public bool Dead { get; private set; }

    protected Vector2 BodyCenter => (Vector2)transform.position + Vector2.up * _halfH;

    protected Vector2 PlayerCenter
    {
        get
        {
            if (Player == null)
                return transform.position;

            return (Vector2)Player.transform.position + Vector2.up * PlayerHalfHeight();
        }
    }

    protected virtual void Awake()
    {
        Health = stats.MaxHealth;
        Player = FindAnyObjectByType<PlayerController>();
        if (Player == null)
            return;

        Vitals = Player.GetComponent<PlayerVitals>();
        _sword = Player.GetComponentInChildren<LevitatingSword>(true);
    }

    protected virtual void OnEnable()
    {
        if (!Dead && !Alive.Contains(this))
            Alive.Add(this);
    }

    protected virtual void OnDisable()
    {
        Alive.Remove(this);
    }

    protected void BindArea(PlayArea area)
    {
        Area = area;
    }

    protected void SetBodyPixels(float widthPixels, float heightPixels)
    {
        _halfW = PixelUnits.ToWorld(widthPixels) * 0.5f;
        _halfH = PixelUnits.ToWorld(heightPixels) * 0.5f;
    }

    protected virtual void Update()
    {
        SortAgainstPlayer();
        if (Dead)
        {
            HideHealthBar();
            TickDead();
            return;
        }

        TickMotion();
        TickAttack();
        TickSword();
        if (Dead)
            HideHealthBar();
        else
            RefreshHealthBar();
    }

    protected virtual void TickMotion()
    {
    }

    protected virtual void TickDead()
    {
    }

    protected virtual void OnHurt()
    {
    }

    protected virtual void OnDied()
    {
    }

    public void TakeDamage(float amount)
    {
        if (Dead || amount <= 0f)
            return;

        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f)
            Die();
        else
            OnHurt();
    }

    public static bool TryDamageSegment(Vector2 from, Vector2 to, float damage)
    {
        const float arrowPadding = 8f;
        for (int i = Alive.Count - 1; i >= 0; i--)
        {
            Enemy enemy = Alive[i];
            if (enemy == null || enemy.Dead)
                continue;

            if (!SegmentHitsRect(from, to, enemy.ExpandedBody(arrowPadding)))
                continue;

            enemy.TakeDamage(damage);
            return true;
        }

        return false;
    }

    protected float GapToPlayer()
    {
        if (!TryPlayerBody(out Rect player))
            return float.PositiveInfinity;

        return Gap(BodyRect(), player);
    }

    protected void ClampToArea()
    {
        Vector2 clamped = ClampPoint(transform.position);
        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }

    protected Vector2 ClampPoint(Vector2 world)
    {
        if (Area == null)
            return world;

        return Area.Clamp(world, _halfW);
    }

    void TickAttack()
    {
        _attackWait -= Time.deltaTime;
        if (Vitals == null || Vitals.Health <= 0f)
            return;

        if (GapToPlayer() > PixelUnits.ToWorld(stats.AttackPadding))
            return;

        if (_attackWait > 0f)
            return;

        _attackWait = stats.AttackInterval;
        Vitals.TakeDamage(stats.Damage);
    }

    void TickSword()
    {
        if (_sword == null || _sword.SwingSerial == 0 || _sword.SwingSerial == _swordSwing)
            return;

        if (!_sword.StrikeReaches(BodyCenter, Mathf.Max(_halfW, _halfH)))
            return;

        _swordSwing = _sword.SwingSerial;
        TakeDamage(_sword.StrikeDamage);
    }

    void Die()
    {
        if (Dead)
            return;

        Dead = true;
        Alive.Remove(this);
        HideHealthBar();
        OnDied();
    }

    void RefreshHealthBar()
    {
        if (Health >= stats.MaxHealth - 0.05f)
        {
            HideHealthBar();
            return;
        }

        EnsureHealthBar();
        if (_barRoot == null || _barFill == null)
            return;

        _barRoot.gameObject.SetActive(true);
        PlaceHealthBar();

        float ratio = stats.MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / stats.MaxHealth);
        float inner = BarWidth - BarInset * 2f;
        _barFill.localScale = new Vector3(inner * ratio, BarHeight - BarInset * 2f, 1f);

        SpriteRenderer body = GetComponent<SpriteRenderer>();
        if (body == null)
            return;

        SpriteRenderer[] parts = _barRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].sortingLayerID = body.sortingLayerID;
            parts[i].sortingOrder = body.sortingOrder + (parts[i].gameObject.name == "Fill" ? 5 : 4);
        }
    }

    void HideHealthBar()
    {
        if (_barRoot != null)
            _barRoot.gameObject.SetActive(false);
    }

    void EnsureHealthBar()
    {
        if (_barRoot != null)
            return;

        var root = new GameObject("Health Bar");
        root.transform.SetParent(transform, false);
        _barRoot = root.transform;

        SpriteRenderer body = GetComponent<SpriteRenderer>();
        Material material = body != null ? body.sharedMaterial : null;

        var track = new GameObject("Track");
        track.transform.SetParent(_barRoot, false);
        track.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
        var trackRenderer = track.AddComponent<SpriteRenderer>();
        trackRenderer.sprite = WhiteSprite(new Vector2(0.5f, 0.5f));
        trackRenderer.color = BarTrack;
        if (material != null)
            trackRenderer.sharedMaterial = material;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(_barRoot, false);
        fill.transform.localPosition = new Vector3(-BarWidth * 0.5f + BarInset, 0f, 0f);
        _barFill = fill.transform;
        var fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = WhiteSprite(new Vector2(0f, 0.5f));
        fillRenderer.color = BarFillColor;
        if (material != null)
            fillRenderer.sharedMaterial = material;
    }

    void PlaceHealthBar()
    {
        float parentScale = Mathf.Abs(transform.lossyScale.x);
        if (parentScale < 0.0001f)
            parentScale = 1f;

        float inv = 1f / parentScale;
        _barRoot.localScale = new Vector3(inv, inv, 1f);
        float aboveFeet = _halfH * 2f + BarGap + BarHeight * 0.5f;
        _barRoot.localPosition = new Vector3(0f, aboveFeet * inv, 0f);
    }

    static Sprite _whiteCenter;
    static Sprite _whiteLeft;

    static Sprite WhiteSprite(Vector2 pivot)
    {
        bool left = pivot.x < 0.25f;
        if (left && _whiteLeft != null)
            return _whiteLeft;
        if (!left && _whiteCenter != null)
            return _whiteCenter;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        if (left)
            _whiteLeft = sprite;
        else
            _whiteCenter = sprite;
        return sprite;
    }

    Rect BodyRect()
    {
        Vector2 center = BodyCenter;
        return new Rect(center.x - _halfW, center.y - _halfH, _halfW * 2f, _halfH * 2f);
    }

    Rect ExpandedBody(float paddingPixels)
    {
        float pad = PixelUnits.ToWorld(paddingPixels);
        Rect rect = BodyRect();
        rect.xMin -= pad;
        rect.yMin -= pad;
        rect.xMax += pad;
        rect.yMax += pad;
        return rect;
    }

    bool TryPlayerBody(out Rect rect)
    {
        rect = default;
        if (Player == null)
            return false;

        Vector3 scale = Player.transform.lossyScale;
        float halfW = PixelUnits.ToWorld(PlayerWidthPixels) * 0.5f * Mathf.Abs(scale.x);
        float halfH = PlayerHalfHeight();
        Vector2 center = (Vector2)Player.transform.position + Vector2.up * halfH;
        rect = new Rect(center.x - halfW, center.y - halfH, halfW * 2f, halfH * 2f);
        return true;
    }

    float PlayerHalfHeight()
    {
        if (Player == null)
            return 0f;

        float scale = Mathf.Abs(Player.transform.lossyScale.y);
        return PixelUnits.ToWorld(PlayerHeightPixels) * 0.5f * scale;
    }

    void SortAgainstPlayer()
    {
        if (Player == null)
            return;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = Player.GetComponent<SpriteRenderer>();
        if (renderer == null || playerRenderer == null)
            return;

        renderer.sortingLayerID = playerRenderer.sortingLayerID;
        bool inFront = transform.position.y <= Player.transform.position.y;
        renderer.sortingOrder = playerRenderer.sortingOrder + (inFront ? 2 : -3);
    }

    static float Gap(Rect a, Rect b)
    {
        float dx = 0f;
        if (a.xMax < b.xMin)
            dx = b.xMin - a.xMax;
        else if (b.xMax < a.xMin)
            dx = a.xMin - b.xMax;

        float dy = 0f;
        if (a.yMax < b.yMin)
            dy = b.yMin - a.yMax;
        else if (b.yMax < a.yMin)
            dy = a.yMin - b.yMax;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
    {
        Vector2 delta = b - a;
        float t0 = 0f;
        float t1 = 1f;
        if (!Clip(a.x, delta.x, rect.xMin, rect.xMax, ref t0, ref t1))
            return false;

        return Clip(a.y, delta.y, rect.yMin, rect.yMax, ref t0, ref t1);
    }

    static bool Clip(float origin, float delta, float min, float max, ref float t0, ref float t1)
    {
        if (Mathf.Abs(delta) < 0.000001f)
            return origin >= min && origin <= max;

        float inv = 1f / delta;
        float near = (min - origin) * inv;
        float far = (max - origin) * inv;
        if (near > far)
        {
            float swap = near;
            near = far;
            far = swap;
        }

        t0 = Mathf.Max(t0, near);
        t1 = Mathf.Min(t1, far);
        return t1 >= t0;
    }
}
