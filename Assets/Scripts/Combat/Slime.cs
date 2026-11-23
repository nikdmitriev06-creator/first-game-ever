using UnityEngine;

/// <summary>
/// Hops toward the player in whatever direction they stand, then waits.
/// The walk strip is a squash cycle, so it plays as the rest pose between hops.
/// The painted blob is about 15 pixels tall, and the whole cell is scaled by an integer so the pixels stay sharp.
/// </summary>
public class Slime : Enemy, IWorldSpawn
{
    const float BodyWidthPixels = 23f;
    const float BodyHeightPixels = 15f;
    const float IdleFramesPerSecond = 6f;
    const float HurtDuration = 0.3f;
    const float DeathDuration = 0.7f;

    [SerializeField] float jumpDistance = 15f;
    [SerializeField] float secondsBetweenJumps = 1f;
    [SerializeField] float jumpDuration = 0.45f;
    [SerializeField] float artScale = 3f;

    SpriteRenderer _renderer;
    SpriteRenderer _shadow;
    SlimeSprites.Set _sprites;
    Vector3 _hopFrom;
    Vector3 _hopTo;
    float _hopT;
    float _between;
    float _idleTime;
    float _hurtTime;
    float _deathTime;
    bool _hopping;
    Vector3 _home;
    bool _homeSet;
    bool _seen;

    public static Slime Spawn(Vector2 world, PlayArea area, Transform parent)
    {
        var go = new GameObject("Slime");
        go.AddComponent<SpriteRenderer>();
        var slime = go.AddComponent<Slime>();
        slime.Place(world, area, parent);
        return slime;
    }

    public void Place(Vector2 world, PlayArea area, Transform parent)
    {
        BindArea(area);
        if (parent != null)
            transform.SetParent(parent, false);

        transform.position = new Vector3(world.x, world.y, 0f);
        ClampToArea();
        RememberHome();
    }

    void Start()
    {
        RememberHome();
    }

    void RememberHome()
    {
        if (_homeSet)
            return;

        _home = transform.position;
        _homeSet = true;
    }

    public void PlaceInWorld(Vector2 world, PlayArea area, Transform parent)
    {
        Place(world, area, parent);
    }

    protected override void Awake()
    {
        base.Awake();
        _renderer = GetComponent<SpriteRenderer>();
        _sprites = SlimeSprites.Load();
        artScale = Mathf.Max(1f, artScale);
        transform.localScale = new Vector3(artScale, artScale, 1f);
        SetBodyPixels(BodyWidthPixels * artScale, BodyHeightPixels * artScale);
        // The first hop can start as soon as the player steps into range.
        _between = secondsBetweenJumps;
        MatchPlayerMaterial();
        CreateShadow();
        Show(_sprites.Idle, 0f);
    }

    protected override void Update()
    {
        base.Update();
        if (_shadow == null || _renderer == null)
            return;

        _shadow.sortingLayerID = _renderer.sortingLayerID;
        _shadow.sortingOrder = _renderer.sortingOrder - 1;
        if (!Dead)
            return;

        Color color = _shadow.color;
        color.a = 0.38f * (1f - Mathf.Clamp01(_deathTime / DeathDuration));
        _shadow.color = color;
    }

    protected override void TickMotion()
    {
        if (_hopping)
            AdvanceHop();
        else
            WaitAndMaybeHop();

        if (_hurtTime > 0f)
        {
            _hurtTime -= Time.deltaTime;
            float shown = 1f - Mathf.Clamp01(_hurtTime / HurtDuration);
            Show(_sprites.Hurt, shown);
            return;
        }

        // Keep the landing frame up for the moment the hop finishes, then return to the squash.
        bool showJump = _hopping || (_hopT > 0f && _between <= 0.0001f);
        if (showJump)
            Show(_sprites.Jump, _hopT);
        else
            ShowLoop(_sprites.Idle);
    }

    protected override void TickDead()
    {
        _deathTime += Time.deltaTime;
        Show(_sprites.Dead, _deathTime / DeathDuration);
        if (_deathTime >= DeathDuration)
            Destroy(gameObject);
    }

    protected override void OnHurt()
    {
        _hurtTime = HurtDuration;
    }

    protected override void OnDied()
    {
        _hopping = false;
        _hurtTime = 0f;
        _deathTime = 0f;
    }

    void WaitAndMaybeHop()
    {
        _between += Time.deltaTime;
        _idleTime += Time.deltaTime;
        if (_between < secondsBetweenJumps)
            return;

        TryBeginHop();
    }

    void TryBeginHop()
    {
        RememberHome();
        float gap = GapToPlayer();
        float notice = PixelUnits.ToWorld(Stats.NoticeRange);
        float lose = PixelUnits.ToWorld(Stats.LoseRange);
        float attack = PixelUnits.ToWorld(Stats.AttackPadding);
        float leash = PixelUnits.ToWorld(Stats.LeashRange);
        float homeDistance = Vector2.Distance(transform.position, _home);
        if (gap <= notice)
            _seen = true;

        MindOrder order = CreatureMind.Choose(gap, homeDistance, notice, lose, attack, leash, _seen);
        if (order == MindOrder.Attack)
            return;

        Vector2 goal = (Vector2)_home;
        float travel = PixelUnits.ToWorld(jumpDistance);
        if (order == MindOrder.Chase || order == MindOrder.Detect)
        {
            Vector2 to = PlayerCenter - BodyCenter;
            if (to.sqrMagnitude < 0.0001f)
                return;

            travel = Mathf.Min(travel, Mathf.Max(0f, gap - attack));
            goal = (Vector2)transform.position + to.normalized * travel;
        }
        else if (order == MindOrder.Idle)
        {
            if (_between < secondsBetweenJumps * 2f)
                return;

            Vector2 ring = new Vector2(Mathf.Cos(_idleTime), Mathf.Sin(_idleTime * 0.7f));
            goal = (Vector2)_home + ring.normalized * PixelUnits.ToWorld(18f);
        }

        if (travel < PixelUnits.ToWorld(1f) && order != MindOrder.Return && order != MindOrder.Idle)
            return;

        _hopFrom = transform.position;
        _hopTo = ClampPoint(goal);
        if (((Vector2)_hopTo - (Vector2)_hopFrom).sqrMagnitude < 0.0004f)
        {
            _between = 0f;
            return;
        }

        _hopT = 0f;
        _hopping = true;
    }

    void AdvanceHop()
    {
        float duration = Mathf.Max(0.05f, jumpDuration);
        _hopT += Time.deltaTime / duration;
        float t = Mathf.Clamp01(_hopT);
        // The first frames squash in place. The body then leaves the ground and covers the hop.
        float air = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.9f, t));
        Vector3 pos = Vector3.Lerp(_hopFrom, _hopTo, air);
        transform.position = new Vector3(pos.x, pos.y, _hopFrom.z);
        ClampToArea();

        if (t < 1f)
            return;

        _hopping = false;
        _between = 0f;
    }

    void ShowLoop(Sprite[] frames)
    {
        if (_renderer == null || frames == null || frames.Length == 0)
            return;

        int index = Mathf.FloorToInt(_idleTime * IdleFramesPerSecond) % frames.Length;
        if (index < 0)
            index += frames.Length;

        Sprite sprite = frames[index];
        if (sprite != null)
            _renderer.sprite = sprite;
    }

    void Show(Sprite[] frames, float t)
    {
        if (_renderer == null || frames == null || frames.Length == 0)
            return;

        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(t) * frames.Length), 0, frames.Length - 1);
        Sprite sprite = frames[index];
        if (sprite != null)
            _renderer.sprite = sprite;
    }

    void CreateShadow()
    {
        if (_renderer == null)
            return;

        var go = new GameObject("Shadow");
        go.transform.SetParent(transform, false);
        _shadow = go.AddComponent<SpriteRenderer>();
        _shadow.sprite = ShadowBlob();
        _shadow.color = new Color(0f, 0.05f, 0f, 0.38f);
        _shadow.sharedMaterial = _renderer.sharedMaterial;

        // Parent scale already enlarges the slime. The blob below is 1 unit wide before this local scale.
        float width = BodyWidthPixels / PixelUnits.PerUnit * 1.35f;
        _shadow.transform.localScale = new Vector3(width, width * 0.42f, 1f);
        _shadow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
    }

    static Sprite _shadowBlob;

    static Sprite ShadowBlob()
    {
        if (_shadowBlob != null)
            return _shadowBlob;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float falloff = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                byte alpha = (byte)Mathf.RoundToInt(falloff * falloff * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _shadowBlob = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        _shadowBlob.hideFlags = HideFlags.HideAndDontSave;
        return _shadowBlob;
    }

    void MatchPlayerMaterial()
    {
        if (_renderer == null || Player == null)
            return;

        SpriteRenderer playerRenderer = Player.GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
            return;

        if (playerRenderer.sharedMaterial != null)
            _renderer.sharedMaterial = playerRenderer.sharedMaterial;

        _renderer.sortingLayerID = playerRenderer.sortingLayerID;
    }
}
