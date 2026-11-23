using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One presenter for every weapon that is not the painted sword or bow.
/// The profile chooses melee or a shot, the arc, the mana cost and the reload.
/// The marker is a placeholder. It is not the character sprite.
/// </summary>
public class ProfileWeapon : Weapon
{
    string _boundId;
    bool _held;
    float _ready;
    bool _swinging;
    bool _delivered;
    float _swingTime;
    float _swingDuration = 0.4f;
    float _windupAt = 0.22f;
    Vector2 _aim = Vector2.right;
    AttackPlan _plan;
    CombatStrike _strike;
    Transform _marker;
    SpriteRenderer _markerRenderer;
    Sprite _bolt;

    public override string ItemId => _boundId ?? string.Empty;

    public override bool Wants(string itemId)
    {
        return WeaponLoadout.UsesProfileHand(itemId);
    }

    public override bool IsHeld => _held;

    public override Sprite IconSprite => ItemArt.Icon(_boundId);

    public override void SetHeld(bool held)
    {
        _held = held;
        if (!held)
        {
            _swinging = false;
            _boundId = null;
            Hide();
            return;
        }

        PlayerInventory bag = GetComponent<PlayerInventory>();
        if (bag == null)
            bag = GetComponentInParent<PlayerInventory>();
        _boundId = bag != null ? bag.SlotAt(bag.Selected) : null;
        EnsureMarker();
        if (_markerRenderer != null)
            _markerRenderer.enabled = true;
    }

    public override void Attack()
    {
        if (!_held || string.IsNullOrEmpty(_boundId) || _swinging || Time.time < _ready)
            return;

        if (!TryResolvePlan(out AttackPlan plan))
            return;
        if (!HasMana(plan.ManaCost) || !TrySpendStamina())
            return;
        if (!TrySpendMana(plan.ManaCost))
            return;

        StatusHost host = GetComponent<StatusHost>();
        float scale = host != null ? host.OutgoingPhysicalMultiplier : 1f;
        CombatStrike strike = CombatStrike.Build(plan, Random.value, scale);
        strike.Owner = this;
        _aim = Aim();
        CueAttack(plan);

        if (plan.Kind == AttackKind.Projectile || plan.Kind == AttackKind.Magic)
        {
            Shoot(plan, strike);
            float interval = ScaledInterval(plan.AttackSpeed);
            _ready = Time.time + Mathf.Max(interval, plan.Reload);
            return;
        }

        _plan = plan;
        _strike = strike;
        _swingDuration = ScaledInterval(plan.AttackSpeed);
        _windupAt = plan.Windup > 0f ? plan.Windup : 0.2f;
        _swingTime = 0f;
        _delivered = false;
        _swinging = true;
    }

    void Awake()
    {
        BindOwner();
        EnsureMarker();
        Hide();
    }

    void Update()
    {
        if (!_held)
        {
            Hide();
            return;
        }

        _aim = Aim();
        PlaceMarker();
        if (!_swinging)
            return;

        _swingTime += Time.deltaTime / Mathf.Max(0.05f, _swingDuration);
        if (!_delivered && _swingTime >= _windupAt)
        {
            _delivered = true;
            DeliverMelee();
        }

        if (_swingTime >= 1f)
            _swinging = false;
    }

    void DeliverMelee()
    {
        ImpactLibrary.Ensure();
        float range = Mathf.Max(0.2f, _plan.Range);
        float arc = _plan.ArcDegrees > 1f ? _plan.ArcDegrees : 70f;
        Vector2 origin = transform.position;
        Vector2 chest = origin + Vector2.up * 0.9f;
        var hits = new List<IImpactReceiver>();
        float reach = _plan.HitHalfLength > 0.05f
            ? _plan.HitAlong + _plan.HitHalfLength + _plan.HitHalfWidth + 1.25f
            : range + 1.5f;
        ReceiverRegistry.CollectNear(origin, Mathf.Max(0.5f, reach), hits);
        float power = _strike != null ? _strike.Total : 0f;
        var impact = new Impact(ImpactLibrary.Slash, power, chest, _aim, this, _strike);
        for (int i = 0; i < hits.Count; i++)
        {
            IImpactReceiver target = hits[i];
            if (target == null || !target.CanReceive)
                continue;

            Vector2 center = target.Origin + Vector2.up * (target.Height * 0.5f);
            if (!Reaches(chest, center, range, arc, target.Slop))
                continue;

            target.Receive(impact);
        }
    }

    bool Reaches(Vector2 chest, Vector2 point, float range, float arc, float padding)
    {
        if (_plan.HitHalfLength > 0.05f)
        {
            Vector2 axis = _aim.sqrMagnitude > 0.0001f ? _aim.normalized : Vector2.right;
            Vector2 side = new Vector2(-axis.y, axis.x);
            Vector2 boxCenter = chest + axis * _plan.HitAlong + side * _plan.HitSide;
            float halfWidth = _plan.HitHalfWidth > 0.02f ? _plan.HitHalfWidth : 0.35f;
            return WeaponHit.Box(boxCenter, axis, point, _plan.HitHalfLength, halfWidth, padding);
        }

        return MeleeReach.Contains(chest, _aim, point, range, arc);
    }

    void Shoot(AttackPlan plan, CombatStrike strike)
    {
        int count = Mathf.Max(1, plan.ProjectileCount);
        float spread = count <= 1 ? 0f : 7f;
        float start = -spread * (count - 1) * 0.5f;
        float speed = plan.ProjectileSpeed > 0f ? plan.ProjectileSpeed : 16f;
        bool magic = plan.Kind == AttackKind.Magic;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg + start + spread * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var go = new GameObject(magic ? "Spell Bolt" : "Quarrel");
            go.transform.position = transform.position + Vector3.up * 0.9f + (Vector3)(direction * 0.55f);
            go.transform.localScale = Vector3.one * (magic ? 0.55f : 0.32f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Bolt();
            renderer.color = magic ? new Color(1f, 0.42f, 0.14f, 1f) : new Color(0.82f, 0.86f, 0.9f, 1f);
            SpriteRenderer body = GetComponent<SpriteRenderer>();
            if (body != null)
            {
                renderer.sortingLayerID = body.sortingLayerID;
                renderer.sortingOrder = body.sortingOrder + 3;
            }

            var shot = go.AddComponent<ArrowShot>();
            shot.Launch(direction, speed, Mathf.Max(1f, plan.Range), strike != null ? strike.Total : 0f, strike);
            if (magic)
            {
                LocalLight glow = go.AddComponent<LocalLight>();
                glow.Configure(LocalLight.Role.Magic, renderer.color, 2.2f, 0.7f, false);
            }
        }
    }

    void CueAttack(AttackPlan plan)
    {
        if (plan.Kind == AttackKind.Magic)
        {
            Fx.Play(FxEvent.Magic, transform.position + Vector3.up, plan.PrimaryType);
            return;
        }

        if (plan.Kind == AttackKind.Projectile)
        {
            SoundBus.Play(_boundId == ItemIds.Crossbow ? FxEvent.Crossbow : FxEvent.Bow, transform.position);
            return;
        }

        SoundBus.Play(FxEvent.Hit, transform.position);
    }

    void EnsureMarker()
    {
        if (_marker != null)
            return;

        var go = new GameObject("Weapon Marker");
        go.transform.SetParent(transform, false);
        _marker = go.transform;
        _markerRenderer = go.AddComponent<SpriteRenderer>();
        _markerRenderer.enabled = false;
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        if (body != null)
        {
            _markerRenderer.sortingLayerID = body.sortingLayerID;
            _markerRenderer.sortingOrder = body.sortingOrder + 2;
        }
    }

    void PlaceMarker()
    {
        EnsureMarker();
        if (_markerRenderer == null)
            return;

        _markerRenderer.enabled = true;
        _markerRenderer.sprite = ItemArt.Icon(_boundId);
        Vector3 chest = transform.position + Vector3.up * 0.95f + (Vector3)(_aim.normalized * 0.7f);
        _marker.position = chest;
        float angle = Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg;
        _marker.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        float reach = _swinging ? 1.15f : 1f;
        _marker.localScale = new Vector3(reach, reach, 1f);
    }

    void Hide()
    {
        if (_markerRenderer != null)
            _markerRenderer.enabled = false;
    }

    Vector2 Aim()
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return _aim.sqrMagnitude > 0.0001f ? _aim : Vector2.right;

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Vector3 feet = transform.position;
        Plane plane = new Plane(-cam.transform.forward, feet);
        if (!plane.Raycast(ray, out float enter))
            return _aim;

        Vector3 world = ray.GetPoint(enter);
        Vector2 delta = new Vector2(world.x - feet.x, world.y - (feet.y + 0.9f));
        if (delta.sqrMagnitude < 0.04f)
            return _aim;

        return delta.normalized;
    }

    Sprite Bolt()
    {
        if (_bolt != null)
            return _bolt;

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        _bolt = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 16f);
        _bolt.hideFlags = HideFlags.HideAndDontSave;
        return _bolt;
    }
}
