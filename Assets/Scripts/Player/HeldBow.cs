using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Floats beside the character and points at the cursor. A shot sends an arrow along that aim.
/// Scaled to the body the same way as the sword; the source art is left as it is.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HeldBow : Weapon
{
    const float AimFollow = 16f;

    // Arrow length compared with the character's height.
    [SerializeField] float arrowLength = 0.58f;
    [SerializeField] float sizeMatch = 1.35f;
    [SerializeField] float sideGap = 0.16f;
    [SerializeField] float forwardGap = 0.18f;
    [SerializeField] float bobAmount = 0.035f;
    [SerializeField] float bobSpeed = 3.2f;
    // Direction the arrow leaves the unrotated artwork, in degrees. 0 is to the right.
    [SerializeField] float spriteAimAngle = 16f;
    [SerializeField] Sprite arrowSprite;

    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    PlayerController _player;
    float _bodyWidth = 1.6f;
    float _bodyHeight = 3f;
    float _aim;
    bool _hasAim;
    bool _held;
    float _nextShot;

    public override string ItemId => ItemIds.Bow;

    public override bool IsHeld => _held;

    public override Sprite IconSprite
    {
        get
        {
            SpriteRenderer renderer = _renderer != null ? _renderer : GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }

    public override void SetHeld(bool held)
    {
        if (_held == held)
        {
            if (!_held)
                Hide();
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

        Hide();
    }

    public override void Attack()
    {
        if (!_held || Time.time < _nextShot)
            return;

        if (arrowSprite == null)
            return;

        _aim = CursorAngle();
        _hasAim = true;
        float speed = Stats.ProjectileSpeed;
        float range = Stats.Range;
        int shots = 1;
        CombatStrike strike = null;
        float damage;
        float interval = ScaledInterval(Stats.AttackSpeed);
        if (TryPrepareAttack(Random.value, out AttackPlan plan, out strike))
        {
            damage = strike.Total;
            if (plan.ProjectileSpeed > 0f)
                speed = plan.ProjectileSpeed;
            if (plan.Range > 0f)
                range = plan.Range;
            shots = Mathf.Max(1, plan.ProjectileCount);
            interval = ScaledInterval(plan.AttackSpeed);
        }
        else
        {
            damage = RollDamage();
            strike = CombatStrike.Flat(DamageType.Piercing, damage, this);
        }

        if (speed <= 0f || !TrySpendStamina())
            return;

        if (!Shoot(speed, range, damage, shots, strike))
            return;

        _nextShot = Time.time + interval;
    }

    void Awake()
    {
        BindOwner();
        _renderer = GetComponent<SpriteRenderer>();
        Transform parent = transform.parent;
        if (parent != null)
        {
            _playerRenderer = parent.GetComponent<SpriteRenderer>();
            _player = parent.GetComponent<PlayerController>();
        }

        if (_player == null && GameRoot.Active != null)
            _player = GameRoot.Active.Player;
        if (_playerRenderer == null && _player != null)
            _playerRenderer = _player.GetComponent<SpriteRenderer>();

        if (!_held)
            Hide();
    }

    void LateUpdate()
    {
        if (!_held)
        {
            Hide();
            return;
        }

        if (_player == null)
            return;

        if (_bodyHeight < 0.01f)
            MeasureBody();

        FitScale();
        float cursor = CursorAngle();
        if (!_hasAim)
        {
            _aim = cursor;
            _hasAim = true;
        }
        else
        {
            float follow = 1f - Mathf.Exp(-AimFollow * Time.deltaTime);
            _aim = Mathf.LerpAngle(_aim, cursor, follow);
        }

        Vector2 offset = Pose(_aim);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount * _bodyHeight;
        Vector3 anchor = _player.transform.position;
        transform.position = new Vector3(anchor.x + offset.x, anchor.y + offset.y + bob, anchor.z);

        bool flip = Mathf.Cos(_aim * Mathf.Deg2Rad) < 0f;
        if (_renderer != null)
            _renderer.flipX = flip;

        float forward = flip ? 180f - spriteAimAngle : spriteAimAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, _aim - forward);
        ApplySorting(offset.y > _bodyHeight * 0.72f);
    }

    bool Shoot(float speed, float range, float damage, int shots, CombatStrike strike)
    {
        if (arrowSprite == null)
            return false;

        SoundBus.Play(FxEvent.Bow, transform.position);

        if (_bodyHeight < 0.01f)
            MeasureBody();

        int count = Mathf.Max(1, shots);
        float spread = count <= 1 ? 0f : 8f;
        float start = -spread * (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float angle = _aim + start + spread * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector3 origin = transform.position + (Vector3)(direction * (_bodyHeight * 0.42f));

            var go = new GameObject("Arrow");
            go.transform.position = origin;
            go.transform.localScale = ArrowScale();

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite;
            renderer.color = Color.white;
            if (_playerRenderer != null)
            {
                renderer.sortingLayerID = _playerRenderer.sortingLayerID;
                renderer.sortingOrder = _playerRenderer.sortingOrder + 2;
            }

            var shot = go.AddComponent<ArrowShot>();
            if (strike != null)
                strike.Owner = this;
            shot.Launch(direction, speed, range, damage, strike);
        }

        return true;
    }

    Vector3 ArrowScale()
    {
        float pixels = arrowSprite.rect.height / Mathf.Max(1f, arrowSprite.pixelsPerUnit);
        if (pixels < 0.01f)
            return Vector3.one;

        float world = _bodyHeight * Mathf.Max(0.05f, arrowLength) / pixels;
        return new Vector3(world, world, 1f);
    }

    Vector2 Pose(float aimAngle)
    {
        float rad = aimAngle * Mathf.Deg2Rad;
        Vector2 aimDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float side = aimDir.x >= 0f ? 1f : -1f;
        Vector2 flank = new Vector2(-aimDir.y, aimDir.x) * side;
        Vector2 chest = new Vector2(0f, _bodyHeight * 0.5f);
        float reach = _bodyWidth * 0.28f + _bodyHeight * forwardGap;
        return chest + aimDir * reach + flank * (_bodyHeight * sideGap);
    }

    float CursorAngle()
    {
        Vector3 feet = _player != null ? _player.transform.position : transform.position;
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return _aim;

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane plane = new Plane(-cam.transform.forward, feet);
        if (!plane.Raycast(ray, out float enter))
            return _aim;

        Vector3 world = ray.GetPoint(enter);
        Vector2 center = new Vector2(feet.x, feet.y + _bodyHeight * 0.5f);
        Vector2 delta = new Vector2(world.x - center.x, world.y - center.y);
        if (delta.sqrMagnitude < 0.09f)
            return _aim;

        return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
    }

    void MeasureBody()
    {
        _bodyWidth = 1.6f;
        _bodyHeight = 3f;
        if (_playerRenderer == null || _playerRenderer.sprite == null)
            return;

        Vector2 size = _playerRenderer.sprite.bounds.size;
        size.x *= 0.66f;
        size.y *= 0.77f;
        Vector3 lossy = _playerRenderer.transform.lossyScale;
        _bodyWidth = Mathf.Max(0.2f, size.x * Mathf.Abs(lossy.x));
        _bodyHeight = Mathf.Max(0.2f, size.y * Mathf.Abs(lossy.y));
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

    void ApplySorting(bool behind)
    {
        if (_renderer == null || _playerRenderer == null)
            return;

        _renderer.sortingLayerID = _playerRenderer.sortingLayerID;
        _renderer.sortingOrder = _playerRenderer.sortingOrder + (behind ? -2 : 1);
    }

    void Hide()
    {
        if (_renderer != null)
            _renderer.enabled = false;
    }
}
