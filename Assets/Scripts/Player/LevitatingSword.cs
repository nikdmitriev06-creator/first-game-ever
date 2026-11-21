using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Rests beside the character with the blade pointed at the cursor.
/// A strike cocks the blade, then spins it through the cursor. Recovery is short so the next swing can start.
/// After each swing the sprite mirrors, so the next swing still leads with the edge.
/// Hidden unless the sword item is in the selected hotbar slot.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LevitatingSword : MonoBehaviour, IWeapon
{
    const float AimFollow = 16f;

    // Portion of the swing spent rotating the blade back before it accelerates.
    const float WindupPortion = 0.26f;
    // How far past the end of the arc the sweep carries, as a fraction of the slash.
    const float Overshoot = 0.04f;
    // Clock time when the sweep is done. The rest of the swing is a short park into the other side.
    const float StrikeEnd = 0.92f;
    // Where inside the sweep the blade crosses the cursor. Higher leaves less time standing after the hit.
    const float ImpactAt = 0.56f;
    // Degrees off the cursor where a resting sword sits. Positive is the windup side.
    const float SweepStart = 90f;
    // How many degrees the sword shifts further backward during the windup.
    const float WindupKick = 24f;
    // Sprite angle at the start of the slash, cocked back, and at the end, spun through.
    const float BladeStart = 145f;
    const float BladeEnd = -95f;
    // Extra degrees the blade runs ahead as it crosses the cursor.
    const float MidTwist = 34f;
    // Added reach of the wide arc, as a fraction of body height, on top of the mid-swing bulge.
    const float ArcWidth = 0.36f;
    // How much the sprite grows at the moment it crosses the cursor.
    const float HitScale = 0.24f;

    // Seconds for one slash. The character's attack speed multiplier shortens this.
    [SerializeField] float swingDuration = 0.52f;
    [SerializeField] float damage = 12f;
    // Blade length compared with the character. Above 1 is a larger, heavier sword.
    [SerializeField] float sizeMatch = 1.55f;
    // How far beside the body the sword rests, as a fraction of body height, past the body's edge.
    [SerializeField] float sideGap = 0.28f;
    // Extra reach at mid-swing, as a fraction of body height. The arc is tight at the windup and the settle.
    [SerializeField] float arcReach = 0.26f;
    [SerializeField] float bobAmount = 0.045f;
    [SerializeField] float bobSpeed = 3.6f;
    [SerializeField] int followFrames = 2;
    // The artwork points up and to the right when the transform is unrotated.
    [SerializeField] float spriteTipAngle = 45f;
    [SerializeField] Sprite[] windFrames = System.Array.Empty<Sprite>();
    // World width of the gust compared with the character's height.
    [SerializeField] float windSize = 1.25f;

    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    SpriteRenderer _wind;
    PlayerController _player;
    PlayerCombat _combat;
    Vector3[] _history;
    bool _historyReady;
    float _bodyWidth = 1.6f;
    float _bodyHeight = 3f;
    float _restAngle = -90f;
    bool _hasAim;
    float _swingAim;
    float _swingSide = 1f;
    float _side = 1f;
    float _swingTime;
    bool _swinging;
    bool _edgeFlip;
    bool _queued;
    float _bobWeight = 1f;
    float _spin;
    bool _hasSpin;
    bool _held = true;

    public bool IsSwinging => _swinging;

    public string ItemId => ItemIds.Sword;

    public bool IsHeld => _held;

    public Sprite IconSprite
    {
        get
        {
            SpriteRenderer renderer = _renderer != null ? _renderer : GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }

    public void SetHeld(bool held)
    {
        if (_held == held)
        {
            if (!_held)
                HideVisual();
            return;
        }

        _held = held;
        if (_held)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null)
                _renderer.enabled = true;
            return;
        }

        _swinging = false;
        _queued = false;
        _swingTime = 0f;
        HideVisual();
    }

    public void Attack()
    {
        if (!_held)
            return;

        RequestSwing();
    }

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }

    public float SwingDuration
    {
        get => swingDuration;
        set => swingDuration = Mathf.Max(0.05f, value);
    }

    public void RequestSwing()
    {
        if (_swinging)
        {
            _queued = true;
            return;
        }

        BeginSwing();
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        Transform parent = transform.parent;
        if (parent != null)
        {
            _playerRenderer = parent.GetComponent<SpriteRenderer>();
            _player = parent.GetComponent<PlayerController>();
            _combat = parent.GetComponent<PlayerCombat>();
        }

        if (_player == null)
            _player = FindAnyObjectByType<PlayerController>();
        if (_combat == null && _player != null)
            _combat = _player.GetComponent<PlayerCombat>();
        if (_playerRenderer == null && _player != null)
            _playerRenderer = _player.GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        MeasureBody();
        FitScale();
        EnsureWind();
    }

    void LateUpdate()
    {
        if (!_held)
        {
            HideVisual();
            return;
        }

        if (_player == null)
            return;

        if (_bodyHeight < 0.01f)
            MeasureBody();

        FitScale();
        RecordAnchor(_player.transform.position);
        Vector3 anchor = _history[_history.Length - 1];

        float cursor = CursorAngle(anchor);
        if (!_hasAim)
        {
            _restAngle = cursor;
            _hasAim = true;
        }
        else if (!_swinging)
        {
            float follow = 1f - Mathf.Exp(-AimFollow * Time.deltaTime);
            _restAngle = Mathf.LerpAngle(_restAngle, cursor, follow);
        }

        if (_swinging)
            AdvanceSwing();

        float bobTarget = _swinging ? 0.15f : 1f;
        _bobWeight = Mathf.MoveTowards(_bobWeight, bobTarget, Time.deltaTime * 8f);

        float aimAngle = _swinging ? _swingAim : _restAngle;
        EvaluatePose(aimAngle, out Vector2 offset, out Vector2 facing, out Vector2 travel, out float windAlong);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount * _bodyHeight * _bobWeight;
        Vector3 world = new Vector3(anchor.x + offset.x, anchor.y + offset.y + bob, anchor.z);
        transform.position = world;
        if (_renderer != null)
            _renderer.flipX = _edgeFlip;
        ApplyHitScale(windAlong);
        transform.rotation = Quaternion.Euler(0f, 0f, BladeAngle(facing));
        ApplySorting(offset.y > _bodyHeight * 0.7f);
        UpdateWind(world, travel, windAlong);
    }

    void BeginSwing()
    {
        Vector3 feet = _player != null ? _player.transform.position : transform.position;
        _swingAim = CursorAngle(feet);
        _restAngle = _swingAim;
        _swingSide = _side;
        _swingTime = 0f;
        _swinging = true;
    }

    void AdvanceSwing()
    {
        float duration = Mathf.Max(0.05f, swingDuration) / AttackScale();
        _swingTime += Time.deltaTime / duration;
        if (_swingTime < 1f)
            return;

        _side = -_swingSide;
        _edgeFlip = !_edgeFlip;
        _swinging = false;
        _swingTime = 0f;
        if (!_queued)
            return;

        _queued = false;
        BeginSwing();
    }

    float AttackScale()
    {
        if (_combat == null)
            return 1f;

        return Mathf.Max(0.05f, _combat.AttackSpeedMultiplier);
    }

    float CursorAngle(Vector3 feet)
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return _restAngle;

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane plane = new Plane(-cam.transform.forward, feet);
        if (!plane.Raycast(ray, out float enter))
            return _restAngle;

        Vector3 world = ray.GetPoint(enter);
        Vector2 center = new Vector2(feet.x, feet.y + _bodyHeight * 0.5f);
        Vector2 delta = new Vector2(world.x - center.x, world.y - center.y);
        if (delta.sqrMagnitude < 0.09f)
            return _restAngle;

        return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
    }

    void EvaluatePose(float aimAngle, out Vector2 offset, out Vector2 facing, out Vector2 travel, out float windAlong)
    {
        Vector2 chest = new Vector2(0f, _bodyHeight * 0.5f);
        float hoverRadius = _bodyWidth * 0.5f + _bodyHeight * sideGap;

        if (!_swinging)
        {
            Pose(chest, aimAngle, _side, 90f, 0f, hoverRadius, Vector2.zero, out offset, out facing, out travel);
            windAlong = 0f;
            return;
        }

        float time = Mathf.Clamp01(_swingTime);
        if (time <= WindupPortion)
        {
            float k = Smooth(time / WindupPortion);
            Pose(chest, aimAngle, _swingSide, 90f, 0f, hoverRadius, Vector2.zero, out Vector2 from, out Vector2 fromFacing, out _);
            Slash(chest, aimAngle, hoverRadius, 0f, out Vector2 to, out Vector2 toFacing, out travel);
            offset = Vector2.Lerp(from, to, k);
            facing = LerpDir(fromFacing, toFacing, k);
            windAlong = 0f;
            return;
        }

        if (time <= StrikeEnd)
        {
            float k = (time - WindupPortion) / (StrikeEnd - WindupPortion);
            float s = Mathf.Lerp(0f, 1f + Overshoot, HeavyStrike(k));
            Slash(chest, aimAngle, hoverRadius, s, out offset, out facing, out travel);
            windAlong = Mathf.Clamp01(s);
            return;
        }

        float settle = Smooth((time - StrikeEnd) / (1f - StrikeEnd));
        Slash(chest, aimAngle, hoverRadius, 1f + Overshoot, out Vector2 swept, out Vector2 sweptFacing, out travel);
        Pose(chest, aimAngle, -_swingSide, 90f, 0f, hoverRadius, Vector2.zero, out Vector2 rest, out Vector2 restFacing, out _);
        offset = Vector2.Lerp(swept, rest, settle);
        facing = LerpDir(sweptFacing, restFacing, settle);
        windAlong = 0f;
    }

    // s = 0 is the leaned-back pose. s = 0.5 crosses the cursor. s = 1 is the other side.
    void Slash(Vector2 chest, float aimAngle, float hoverRadius, float s, out Vector2 offset, out Vector2 facing, out Vector2 travel)
    {
        float u = Mathf.Max(0f, s);
        float clamped = Mathf.Clamp01(s);
        // The arc is symmetric, so halfway is straight at the cursor rather than off to one side.
        float cocked = SweepStart + WindupKick;
        float place = Mathf.LerpUnclamped(cocked, -cocked, u);
        float bulge = Mathf.Sin(clamped * Mathf.PI);
        // The sprite itself turns through a wide angle. At the hit the twist brings it onto the cursor.
        float blade = Mathf.LerpUnclamped(BladeStart, BladeEnd, u) - bulge * MidTwist;
        float wide = hoverRadius + _bodyHeight * (ArcWidth + arcReach);
        float radius = Mathf.Lerp(hoverRadius * 0.82f, wide, bulge);
        // The lean is gone by the time the blade reaches the cursor.
        Vector2 extra = BackShift(aimAngle) * Mathf.Clamp01(1f - clamped * 2f);
        Pose(chest, aimAngle, _swingSide, place, blade, radius, extra, out offset, out facing, out travel);
        offset += facing * (_bodyHeight * Mathf.Max(0.05f, sizeMatch) * 0.34f * bulge);
    }

    Vector2 BackShift(float aimAngle)
    {
        Vector2 behind = AngleDir(aimAngle + 90f * _swingSide);
        Vector2 away = -AngleDir(aimAngle);
        return (behind * 0.08f + away * 0.11f) * _bodyHeight;
    }

    void Pose(
        Vector2 chest,
        float aimAngle,
        float sideSign,
        float placeDeg,
        float bladeDeg,
        float radius,
        Vector2 extra,
        out Vector2 offset,
        out Vector2 facing,
        out Vector2 travel)
    {
        float placeAngle = aimAngle + sideSign * placeDeg;
        Vector2 radial = AngleDir(placeAngle);
        offset = chest + radial * radius + extra;
        facing = AngleDir(aimAngle + sideSign * bladeDeg);
        travel = new Vector2(-radial.y, radial.x) * -sideSign;
    }

    static float Smooth(float k)
    {
        k = Mathf.Clamp01(k);
        return k * k * (3f - 2f * k);
    }

    // Most of the sweep is the load into the cursor. The exit after the hit is the short tail.
    static float HeavyStrike(float k)
    {
        k = Mathf.Clamp01(k);
        if (k <= ImpactAt)
        {
            float t = k / ImpactAt;
            float eased = t * t * t;
            return Mathf.Lerp(0f, 0.5f, eased);
        }

        float exit = (k - ImpactAt) / (1f - ImpactAt);
        float easedOut = 1f - (1f - exit) * (1f - exit);
        return Mathf.Lerp(0.5f, 1f, easedOut);
    }

    void ApplyHitScale(float strikeAlong)
    {
        float punch = Mathf.Sin(Mathf.Clamp01(strikeAlong) * Mathf.PI);
        float heavy = 1f + punch * HitScale;
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(scale.x * heavy, Mathf.Abs(scale.y) * heavy, scale.z);
    }

    static Vector2 LerpDir(Vector2 from, Vector2 to, float k)
    {
        Vector2 blended = Vector2.Lerp(from, to, k);
        if (blended.sqrMagnitude < 0.0001f)
            return to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.down;

        return blended.normalized;
    }

    float BladeAngle(Vector2 aim)
    {
        if (aim.sqrMagnitude < 0.0001f)
            aim = Vector2.down;

        float wobble = _swinging ? 0f : Mathf.Sin(Time.time * bobSpeed * 0.8f) * 2.5f * _bobWeight;
        // flipX mirrors local X, so the painted tip moves from +spriteTipAngle to its supplement.
        float tip = _edgeFlip ? 180f - spriteTipAngle : spriteTipAngle;
        float target = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - tip + wobble;
        if (!_hasSpin)
        {
            _spin = target;
            _hasSpin = true;
            return _spin;
        }

        _spin += Mathf.DeltaAngle(_spin, target);
        return _spin;
    }

    static Vector2 AngleDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void EnsureWind()
    {
        if (_wind != null || windFrames == null || windFrames.Length == 0 || windFrames[0] == null)
            return;

        var gust = new GameObject("SwingWind");
        Transform holder = transform.parent != null ? transform.parent : transform;
        gust.transform.SetParent(holder, false);
        _wind = gust.AddComponent<SpriteRenderer>();
        _wind.sprite = windFrames[0];
        _wind.enabled = false;
    }

    void UpdateWind(Vector3 world, Vector2 travel, float along)
    {
        EnsureWind();
        if (_wind == null)
            return;

        bool show = _swinging && along > 0.03f && along < 0.98f && travel.sqrMagnitude > 0.0001f;
        _wind.enabled = show;
        if (!show)
            return;

        int frame = Mathf.Min(windFrames.Length - 1, Mathf.FloorToInt(along * windFrames.Length));
        Sprite sprite = windFrames[frame];
        if (sprite != null)
            _wind.sprite = sprite;

        float alpha = Mathf.Sin(along * Mathf.PI) * 0.9f;
        _wind.color = new Color(1f, 1f, 1f, alpha);
        _wind.transform.position = world;
        _wind.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg);

        if (sprite == null)
            return;

        float spriteUnits = sprite.rect.width / Mathf.Max(1f, sprite.pixelsPerUnit);
        float parent = _wind.transform.parent != null ? Mathf.Abs(_wind.transform.parent.lossyScale.x) : 1f;
        if (parent < 0.0001f)
            parent = 1f;

        float pulse = Mathf.Lerp(0.8f, 1.15f, Mathf.Sin(along * Mathf.PI));
        float local = _bodyHeight * windSize * pulse / spriteUnits / parent;
        _wind.transform.localScale = new Vector3(local, local, 1f);
    }

    void HideVisual()
    {
        if (_renderer != null)
            _renderer.enabled = false;
        if (_wind != null)
            _wind.enabled = false;
    }

    void ApplySorting(bool behind)
    {
        if (_renderer == null || _playerRenderer == null)
            return;

        _renderer.sortingLayerID = _playerRenderer.sortingLayerID;
        int swordOrder = _playerRenderer.sortingOrder + (behind ? -2 : 1);
        _renderer.sortingOrder = swordOrder;
        if (_wind == null)
            return;

        _wind.sortingLayerID = _playerRenderer.sortingLayerID;
        _wind.sortingOrder = swordOrder + 1;
    }

    void RecordAnchor(Vector3 current)
    {
        EnsureHistory();
        if (!_historyReady)
        {
            for (int i = 0; i < _history.Length; i++)
                _history[i] = current;
            _historyReady = true;
            return;
        }

        float snap = Mathf.Max(2f, _bodyHeight);
        if ((current - _history[0]).sqrMagnitude > snap * snap)
        {
            for (int i = 0; i < _history.Length; i++)
                _history[i] = current;
            return;
        }

        for (int i = _history.Length - 1; i > 0; i--)
            _history[i] = _history[i - 1];
        _history[0] = current;
    }

    void EnsureHistory()
    {
        int size = Mathf.Max(0, followFrames) + 1;
        if (_history != null && _history.Length == size)
            return;

        _history = new Vector3[size];
        _historyReady = false;
    }

    void MeasureBody()
    {
        _bodyWidth = 1.6f;
        _bodyHeight = 3f;
        if (_playerRenderer == null || _playerRenderer.sprite == null)
            return;

        Sprite sprite = _playerRenderer.sprite;
        Vector2 visible;
        if (!TryVisibleSize(sprite, out visible))
        {
            visible = sprite.bounds.size;
            visible.x *= 0.66f;
            visible.y *= 0.77f;
        }

        Vector3 lossy = _playerRenderer.transform.lossyScale;
        _bodyWidth = Mathf.Max(0.2f, visible.x * Mathf.Abs(lossy.x));
        _bodyHeight = Mathf.Max(0.2f, visible.y * Mathf.Abs(lossy.y));
    }

    static bool TryVisibleSize(Sprite sprite, out Vector2 size)
    {
        size = default;
        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
            return false;

        Rect rect = sprite.textureRect;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(rect.x), 0, texture.width - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(rect.y), 0, texture.height - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(rect.xMax), x0 + 1, texture.width);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(rect.yMax), y0 + 1, texture.height);

        Color32[] pixels;
        try
        {
            pixels = texture.GetPixels32();
        }
        catch (UnityException)
        {
            return false;
        }

        int minX = x1;
        int minY = y1;
        int maxX = x0 - 1;
        int maxY = y0 - 1;
        int stride = texture.width;
        for (int y = y0; y < y1; y++)
        {
            int row = y * stride;
            for (int x = x0; x < x1; x++)
            {
                if (pixels[row + x].a <= 16)
                    continue;

                if (x < minX)
                    minX = x;
                if (y < minY)
                    minY = y;
                if (x > maxX)
                    maxX = x;
                if (y > maxY)
                    maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return false;

        float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
        size = new Vector2((maxX - minX + 1) / ppu, (maxY - minY + 1) / ppu);
        return true;
    }

    void FitScale()
    {
        if (_renderer == null || _renderer.sprite == null || _bodyHeight < 0.01f)
            return;

        float spriteLength = _renderer.sprite.rect.size.magnitude / Mathf.Max(1f, _renderer.sprite.pixelsPerUnit);
        if (spriteLength < 0.01f)
            return;

        float parent = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
        if (parent < 0.0001f)
            parent = 1f;

        float local = _bodyHeight * Mathf.Max(0.05f, sizeMatch) / spriteLength / parent;
        transform.localScale = new Vector3(local, local, 1f);
    }
}
