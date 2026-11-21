using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One tool: an axe. It rests beside the body and points at the cursor.
/// Use fells a tree that the cursor is on, when the trunk is within reach.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Axe : Tool
{
    const float AimFollow = 16f;
    const float Reach = 3.6f;
    const float ChopStamina = 6f;
    const float ChopCooldown = 0.46f;
    const float MissCooldown = 0.22f;
    const float SwingTime = 0.2f;

    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    PlayerController _player;
    PlayerVitals _vitals;
    float _bodyWidth = 1.6f;
    float _bodyHeight = 3f;
    float _aim;
    bool _hasAim;
    bool _held;
    float _next;
    float _chop;

    public override string ItemId => ItemIds.Axe;

    public override bool IsHeld => _held;

    public override Sprite IconSprite => ItemArt.Axe();

    public override void SetHeld(bool held)
    {
        if (_held == held)
        {
            if (!_held)
                Hide();
            return;
        }

        _held = held;
        _chop = 0f;
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

    public override void Use()
    {
        if (!_held || Time.time < _next)
            return;

        Vector2 feet = Feet();
        Vector2 aim = AimPoint(feet);
        Tree tree = Tree.FindAt(feet, aim, Reach);
        if (tree != null && !TrySpendStamina())
            return;

        _aim = AimAngle(feet);
        _hasAim = true;
        _chop = 1f;
        _next = Time.time + (tree != null ? ChopCooldown : MissCooldown);
        if (tree != null)
            tree.Chop();
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

        if (_player == null)
            _player = FindAnyObjectByType<PlayerController>();
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

        if (_player == null)
            return;

        if (_bodyHeight < 0.01f)
            MeasureBody();

        FitScale();
        float cursor = AimAngle(Feet());
        if (!_hasAim)
        {
            _aim = cursor;
            _hasAim = true;
        }
        else if (_chop <= 0f)
        {
            float follow = 1f - Mathf.Exp(-AimFollow * Time.deltaTime);
            _aim = Mathf.LerpAngle(_aim, cursor, follow);
        }

        float swing = 0f;
        if (_chop > 0f)
        {
            _chop -= Time.deltaTime / SwingTime;
            float along = 1f - Mathf.Clamp01(_chop);
            swing = Mathf.Sin(along * Mathf.PI) * -68f;
        }

        Vector2 offset = Pose(_aim);
        float bob = Mathf.Sin(Time.time * 3.4f) * 0.03f * _bodyHeight;
        Vector3 anchor = _player.transform.position;
        transform.position = new Vector3(anchor.x + offset.x, anchor.y + offset.y + bob, anchor.z);
        transform.rotation = Quaternion.Euler(0f, 0f, _aim + swing);
        ApplySorting(offset.y > _bodyHeight * 0.72f);
    }

    bool TrySpendStamina()
    {
        if (_vitals == null || ChopStamina <= 0f)
            return true;

        return _vitals.TrySpendStaminaAmount(ChopStamina);
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
        if (_renderer != null)
            _renderer.enabled = false;
    }
}
