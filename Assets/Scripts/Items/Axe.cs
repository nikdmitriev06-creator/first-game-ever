using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A swing tool. It rests beside the body and points at the cursor.
/// Use plays the swing from the bound item profile and sends one impact. The target decides the result.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Axe : Tool
{
    const float AimFollow = 16f;
    const float ChopCooldown = 0.46f;
    const float MissCooldown = 0.22f;
    const float AnticipateTime = 0.15f;
    const float SwingTime = 0.11f;
    const float ImpactTime = 0.07f;
    const float RecoverTime = 0.13f;

    enum Phase
    {
        None,
        Anticipation,
        Swing,
        Impact,
        Recovery
    }

    struct SwingSample
    {
        public float Angle;
        public float Radius;
        public float Lift;
        public float Spin;
    }

    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    PlayerController _player;
    PlayerVitals _vitals;
    float _bodyWidth = 1.6f;
    float _bodyHeight = 3f;
    float _aim;
    float _lockedAim;
    Vector2 _aimPoint;
    float _swingSign = 1f;
    float _windupSweep = 100f;
    bool _hasAim;
    bool _held;
    bool _struck;
    float _next;
    float _phaseTime;
    Phase _phase;
    IImpactReceiver _target;
    ItemDefinition _bound;

    public override string ItemId => _bound != null ? _bound.Id : ItemIds.Axe;

    public override bool IsHeld => _held;

    public override Sprite IconSprite
    {
        get
        {
            if (_bound != null && _bound.IconSprite != null)
                return _bound.IconSprite;

            return ItemArt.Axe();
        }
    }

    public void Bind(ItemDefinition item)
    {
        _bound = item;
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && item != null && item.IconSprite != null)
            _renderer.sprite = item.IconSprite;

        SetHeld(item != null);
    }

    ToolProfile Profile
    {
        get
        {
            if (_bound != null && _bound.ResolvedTool != null)
                return _bound.ResolvedTool;

            ItemDefinition axe = ItemCatalog.Find(ItemIds.Axe);
            if (axe != null && axe.ResolvedTool != null)
                return axe.ResolvedTool;

            ImpactLibrary.Ensure();
            return ToolProfile.Swing(ImpactLibrary.Chop, 1f, 3.6f, 6f);
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
        _phase = Phase.None;
        _target = null;
        _struck = true;
        if (_held)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null)
            {
                _renderer.enabled = true;
                _renderer.color = Color.white;
            }

            return;
        }

        Hide();
    }

    public override void Use()
    {
        if (!_held || _phase != Phase.None || Time.time < _next || GameplayGate.MenuOpen)
            return;

        ToolProfile profile = Profile;
        Vector2 feet = Feet();
        Vector2 aim = AimPoint(feet);
        IImpactReceiver target = profile.Shape == TargetShape.Single
            ? ReceiverRegistry.FindBest(feet, aim, profile.Range)
            : null;
        bool connects = target != null;
        if (profile.Shape == TargetShape.Radius)
        {
            var nearby = new System.Collections.Generic.List<IImpactReceiver>();
            ReceiverRegistry.CollectNear(aim, profile.Range, nearby);
            connects = nearby.Count > 0;
        }

        if (connects && !TrySpendStamina(profile.Stamina))
            return;

        _aimPoint = aim;
        _lockedAim = AimAngle(feet);
        Vector2 facing = Direction(_lockedAim);
        _swingSign = PickSign(facing);
        _windupSweep = WindupSweep(facing, _swingSign);
        _target = target;
        _struck = false;
        _phase = Phase.Anticipation;
        _phaseTime = 0f;
        _next = Time.time + (connects ? ChopCooldown : MissCooldown);
        SoundBus.Play(FxEvent.Hit, transform.position);
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sprite = ItemArt.Axe();

        Transform parent = transform.parent;
        if (parent != null)
        {
            _playerRenderer = parent.GetComponent<SpriteRenderer>();
            _player = parent.GetComponent<PlayerController>();
            _vitals = parent.GetComponent<PlayerVitals>();
        }

        if (_player == null && GameRoot.Active != null)
            _player = GameRoot.Active.Player;
        if (_playerRenderer == null && _player != null)
            _playerRenderer = _player.GetComponent<SpriteRenderer>();
        if (_vitals == null && _player != null)
            _vitals = _player.GetComponent<PlayerVitals>();

        if (_playerRenderer != null && _playerRenderer.sharedMaterial != null)
            _renderer.sharedMaterial = _playerRenderer.sharedMaterial;

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

        if (GameplayGate.MenuOpen && _phase != Phase.None)
        {
            _phase = Phase.None;
            _target = null;
            _struck = true;
        }

        if (_player == null)
            return;

        if (_bodyHeight < 0.01f)
            MeasureBody();

        FitScale();
        float cursor = AimAngle(Feet());
        if (!_hasAim)
        {
            _aim = cursor;
            _lockedAim = cursor;
            _hasAim = true;
        }
        else if (_phase == Phase.None)
        {
            float follow = 1f - Mathf.Exp(-AimFollow * Time.deltaTime);
            _aim = Mathf.LerpAngle(_aim, cursor, follow);
        }

        if (_phase != Phase.None)
        {
            _phaseTime += Time.deltaTime;
            while (_phase != Phase.None && _phaseTime >= PhaseDuration(_phase))
            {
                _phaseTime -= PhaseDuration(_phase);
                StepPhase();
            }
        }

        float shown = ShownAim();
        Vector2 grip;
        float rotation;
        if (_phase == Phase.None)
        {
            grip = Pose(shown);
            rotation = shown;
        }
        else
        {
            SwingSample sample = Evaluate(shown);
            Vector2 aimDir = Direction(shown);
            Vector2 shoulder = Shoulder(aimDir);
            grip = Grip(shoulder, aimDir, sample);
            rotation = shown + sample.Angle + sample.Spin;
        }

        float bob = _phase == Phase.None ? Mathf.Sin(Time.time * 3.4f) * 0.03f * _bodyHeight : 0f;
        Vector3 anchor = _player.transform.position;
        transform.position = new Vector3(anchor.x + grip.x, anchor.y + grip.y + bob, anchor.z);
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        ApplySorting(grip.y > _bodyHeight * 0.72f);
        ApplyImpactTint();
    }

    void StepPhase()
    {
        switch (_phase)
        {
            case Phase.Anticipation:
                _phase = Phase.Swing;
                break;
            case Phase.Swing:
                _phase = Phase.Impact;
                Connect();
                break;
            case Phase.Impact:
                _phase = Phase.Recovery;
                break;
            default:
                _aim = _lockedAim;
                _phase = Phase.None;
                _phaseTime = 0f;
                _target = null;
                break;
        }
    }

    void Connect()
    {
        if (_struck)
            return;

        _struck = true;
        ToolProfile profile = Profile;
        Vector2 tip = ContactPoint();
        Vector2 dir = Direction(_lockedAim);
        if (profile.Use == ToolUse.Action)
        {
            var action = new ToolAction(profile.ActionId, profile.Power, tip, dir, this);
            ActionReceiverRegistry.Deliver(action, profile.Range);
            _target = null;
            return;
        }

        var impact = new Impact(profile.Impact, profile.Power, tip, dir, this);
        if (profile.Shape == TargetShape.Radius)
        {
            var hits = new System.Collections.Generic.List<IImpactReceiver>();
            ReceiverRegistry.CollectNear(_aimPoint, profile.Range, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] != null)
                    hits[i].Receive(impact);
            }

            _target = null;
            return;
        }

        IImpactReceiver target = _target;
        _target = null;
        if (target == null)
            return;

        if (!ReferenceEquals(ReceiverRegistry.FindBest(Feet(), _aimPoint, profile.Range), target))
            return;

        target.Receive(impact);
    }

    /// <summary>
    /// Grip position for one moment of the chop.
    /// The swing itself is an arc: pivot + direction(angle) * radius, plus a vertical lift.
    /// Anticipation eases back and up, the swing accelerates into the tree,
    /// impact kicks the head back, and recovery settles into the hand.
    /// </summary>
    SwingSample Evaluate(float aim)
    {
        Vector2 aimDir = Direction(aim);
        Vector2 shoulder = Shoulder(aimDir);
        SwingSample rest = RestSample(aim, shoulder);
        SwingSample windup = new SwingSample
        {
            Angle = rest.Angle + _windupSweep * _swingSign,
            Radius = Mathf.Max(rest.Radius * 1.08f, _bodyHeight * 0.5f),
            Lift = _bodyHeight * 0.18f,
            Spin = 34f * _swingSign
        };
        SwingSample strike = StrikeSample();
        SwingSample recoil = new SwingSample
        {
            Angle = rest.Angle + 22f * _swingSign,
            Radius = Mathf.Lerp(rest.Radius, strike.Radius, 0.32f),
            Lift = _bodyHeight * 0.035f,
            Spin = 14f * _swingSign
        };

        float u = PhaseDuration(_phase) <= 0.0001f ? 1f : Mathf.Clamp01(_phaseTime / PhaseDuration(_phase));
        switch (_phase)
        {
            case Phase.Anticipation:
                return LerpSample(rest, windup, Mathf.SmoothStep(0f, 1f, u));
            case Phase.Swing:
                return LerpSample(windup, strike, u * u * u);
            case Phase.Impact:
                return LerpSample(strike, recoil, 1f - Mathf.Pow(1f - u, 3f));
            default:
                return LerpSample(recoil, rest, Mathf.SmoothStep(0f, 1f, u));
        }
    }

    SwingSample RestSample(float aim, Vector2 shoulder)
    {
        Vector2 from = Pose(aim) - shoulder;
        float radius = Mathf.Max(0.08f, from.magnitude);
        float heading = Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg;
        float angle = Mathf.DeltaAngle(aim, heading);
        return new SwingSample
        {
            Angle = angle,
            Radius = radius,
            Lift = 0f,
            Spin = -angle
        };
    }

    SwingSample StrikeSample()
    {
        return new SwingSample
        {
            Angle = -16f * _swingSign,
            Radius = _bodyHeight * 0.66f,
            Lift = -_bodyHeight * 0.06f,
            Spin = -12f * _swingSign
        };
    }

    Vector2 ContactPoint()
    {
        Vector2 aimDir = Direction(_lockedAim);
        Vector2 shoulder = Shoulder(aimDir);
        SwingSample strike = StrikeSample();
        Vector2 grip = Grip(shoulder, aimDir, strike);
        float rotation = _lockedAim + strike.Angle + strike.Spin;
        Vector2 blade = Rotate(Vector2.right, rotation);
        return (Vector2)_player.transform.position + grip + blade * BladeLength();
    }

    Vector2 Grip(Vector2 shoulder, Vector2 aimDir, SwingSample sample)
    {
        Vector2 grip = shoulder + Rotate(aimDir, sample.Angle) * sample.Radius + Vector2.up * sample.Lift;
        float limit = Mathf.Max(2.6f, _bodyHeight * 1.35f);
        if (grip.sqrMagnitude > limit * limit)
            grip = grip.normalized * limit;
        return grip;
    }

    float ShownAim()
    {
        if (_phase == Phase.None)
            return _aim;

        float blend = _phase == Phase.Anticipation
            ? Mathf.SmoothStep(0f, 1f, _phaseTime / Mathf.Max(0.0001f, AnticipateTime))
            : 1f;
        return Mathf.LerpAngle(_aim, _lockedAim, blend);
    }

    static float PhaseDuration(Phase phase)
    {
        switch (phase)
        {
            case Phase.Anticipation:
                return AnticipateTime;
            case Phase.Swing:
                return SwingTime;
            case Phase.Impact:
                return ImpactTime;
            case Phase.Recovery:
                return RecoverTime;
            default:
                return 0f;
        }
    }

    static SwingSample LerpSample(SwingSample a, SwingSample b, float t)
    {
        return new SwingSample
        {
            Angle = Mathf.Lerp(a.Angle, b.Angle, t),
            Radius = Mathf.Lerp(a.Radius, b.Radius, t),
            Lift = Mathf.Lerp(a.Lift, b.Lift, t),
            Spin = Mathf.Lerp(a.Spin, b.Spin, t)
        };
    }

    static float PickSign(Vector2 aimDir)
    {
        Vector2 ccw = Rotate(aimDir, 80f);
        Vector2 cw = Rotate(aimDir, -80f);
        if (ccw.y > cw.y + 0.02f)
            return 1f;
        if (cw.y > ccw.y + 0.02f)
            return -1f;
        return aimDir.x >= 0f ? 1f : -1f;
    }

    static float WindupSweep(Vector2 aimDir, float sign)
    {
        float lift = Mathf.Clamp01(Rotate(aimDir, 90f * sign).y);
        return Mathf.Lerp(136f, 98f, lift);
    }

    static Vector2 Rotate(Vector2 value, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(r);
        float s = Mathf.Sin(r);
        return new Vector2(value.x * c - value.y * s, value.x * s + value.y * c);
    }

    static Vector2 Direction(float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    float BladeLength()
    {
        if (_renderer == null || _renderer.sprite == null)
            return _bodyHeight * 0.45f;

        float width = _renderer.sprite.rect.width / Mathf.Max(1f, _renderer.sprite.pixelsPerUnit);
        float pivot = _renderer.sprite.pivot.x / Mathf.Max(1f, _renderer.sprite.rect.width);
        return width * Mathf.Abs(transform.lossyScale.x) * Mathf.Clamp01(1f - pivot);
    }

    void ApplyImpactTint()
    {
        if (_renderer == null)
            return;

        if (_phase != Phase.Impact)
        {
            _renderer.color = Color.white;
            return;
        }

        float hot = 1f - Mathf.Clamp01(_phaseTime / Mathf.Max(0.0001f, ImpactTime) / 0.4f);
        _renderer.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.7f), hot);
    }

    bool TrySpendStamina(float amount)
    {
        if (_vitals == null || amount <= 0f)
            return true;

        return _vitals.TrySpendStaminaAmount(amount);
    }

    Vector2 Feet()
    {
        return _player != null ? (Vector2)_player.transform.position : (Vector2)transform.position;
    }

    Vector2 Pose(float aimAngle)
    {
        float rad = aimAngle * Mathf.Deg2Rad;
        Vector2 aimDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float side = aimDir.x >= 0f ? 1f : -1f;
        Vector2 flank = new Vector2(-aimDir.y, aimDir.x) * side;
        Vector2 chest = new Vector2(0f, _bodyHeight * 0.48f);
        float reach = _bodyWidth * 0.22f + _bodyHeight * 0.1f;
        return chest + aimDir * reach + flank * (_bodyHeight * 0.2f);
    }

    Vector2 Shoulder(Vector2 aimDir)
    {
        float side = aimDir.x >= 0f ? 1f : -1f;
        return new Vector2(side * _bodyWidth * 0.1f, _bodyHeight * 0.56f);
    }

    Vector2 AimPoint(Vector2 feet)
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return feet + Vector2.up;

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane plane = new Plane(-cam.transform.forward, new Vector3(feet.x, feet.y, 0f));
        if (!plane.Raycast(ray, out float enter))
            return feet + Vector2.up;

        Vector3 world = ray.GetPoint(enter);
        return new Vector2(world.x, world.y);
    }

    float AimAngle(Vector2 feet)
    {
        Vector2 delta = AimPoint(feet) - new Vector2(feet.x, feet.y + _bodyHeight * 0.45f);
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

        float spriteLength = _renderer.sprite.rect.width / Mathf.Max(1f, _renderer.sprite.pixelsPerUnit);
        if (spriteLength < 0.01f)
            return;

        float parent = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
        if (parent < 0.0001f)
            parent = 1f;

        float local = _bodyHeight * 0.72f / spriteLength / parent;
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
        if (_renderer == null)
            return;

        _renderer.enabled = false;
        _renderer.color = Color.white;
    }
}
