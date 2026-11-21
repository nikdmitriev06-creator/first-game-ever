using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player from screen-space input (WASD and arrows) and plays the
/// matching idle or walk cycle. Screen movement is converted into isometric
/// coordinates and projected back, so speed stays even on screen.
/// </summary>
public class PlayerController : MonoBehaviour
{
    enum Facing
    {
        Down,
        Up,
        Left,
        Right
    }

    [SerializeField] float moveSpeed = 16f;
    // Added to move speed while sprinting. 0.25 is +25%. Buffs and debuffs
    // change this at runtime through SprintSpeedBonus; it is not a fixed multiplier.
    [SerializeField] float sprintSpeedBonus = 4f;
    [SerializeField] PlayArea playArea = null;
    [SerializeField] float walkFramesPerSecond = 8f;
    [SerializeField] Sprite[] downFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] upFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] leftFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] rightFrames = System.Array.Empty<Sprite>();

    Vector2 _isometricPosition;
    InputAction _moveAction;
    InputAction _sprintAction;
    SpriteRenderer _spriteRenderer;
    PlayerVitals _vitals;
    Facing _facing = Facing.Down;
    float _walkTime;
    bool _sprintBlocked;

    public Vector2 IsometricPosition => _isometricPosition;

    public Vector2 FacingDirection
    {
        get
        {
            switch (_facing)
            {
                case Facing.Up:
                    return Vector2.up;
                case Facing.Left:
                    return Vector2.left;
                case Facing.Right:
                    return Vector2.right;
                default:
                    return Vector2.down;
            }
        }
    }

    public float SprintSpeedBonus
    {
        get => sprintSpeedBonus;
        set => sprintSpeedBonus = Mathf.Max(0f, value);
    }

    public void SetPlayArea(PlayArea area)
    {
        playArea = area;
    }

    public float BodyInset()
    {
        return GetBodyInset();
    }

    public void Place(Vector2 worldPosition)
    {
        if (playArea != null)
            worldPosition = playArea.Clamp(worldPosition, GetBodyInset());

        _isometricPosition = IsometricCoordinates.WorldToIsometric(worldPosition);
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    void Awake()
    {
        _isometricPosition = IsometricCoordinates.WorldToIsometric(transform.position);
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _vitals = GetComponent<PlayerVitals>();

        if (InputSystem.actions != null)
        {
            _moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: false);
            _sprintAction = InputSystem.actions.FindAction("Player/Sprint", throwIfNotFound: false);
        }

        ShowFrame(FramesFor(_facing), 0);
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _sprintAction?.Enable();
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        Vector2 screenInput = ReadScreenInput();
        bool moving = screenInput.sqrMagnitude > 0.0001f;
        bool sprinting = TickSprint(moving, deltaTime);
        float speedScale = sprinting ? 1f + SprintSpeedBonus : 1f;

        ApplyScreenMovement(screenInput, deltaTime, moveSpeed * speedScale);
        UpdateSprite(screenInput, deltaTime, speedScale);
    }

    bool TickSprint(bool moving, float deltaTime)
    {
        bool shiftHeld = SprintHeld();
        if (!shiftHeld)
            _sprintBlocked = false;

        bool sprinting = moving && shiftHeld && !_sprintBlocked && (_vitals == null || _vitals.Stamina > 0f);
        if (_vitals == null)
            return sprinting;

        if (sprinting)
        {
            _vitals.TrySpendStamina(deltaTime);
            if (_vitals.Stamina <= 0f)
                _sprintBlocked = true;
            return true;
        }

        _vitals.RecoverStamina(deltaTime);
        return false;
    }

    bool SprintHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            return true;

        return _sprintAction != null && _sprintAction.IsPressed();
    }

    void ApplyScreenMovement(Vector2 screenInput, float deltaTime, float speed)
    {
        if (screenInput.sqrMagnitude > 1f)
            screenInput.Normalize();

        if (screenInput.sqrMagnitude > 0f && deltaTime > 0f)
        {
            Vector2 screenDelta = screenInput * (speed * deltaTime);
            _isometricPosition += IsometricCoordinates.WorldToIsometric(screenDelta);
        }

        Vector2 world = IsometricCoordinates.IsometricToWorld(_isometricPosition);
        if (playArea != null)
            world = playArea.Clamp(world, GetBodyInset());
        if (PlaceableObject.BlocksFeet(world))
            world = transform.position;

        _isometricPosition = IsometricCoordinates.WorldToIsometric(world);
        transform.position = new Vector3(world.x, world.y, transform.position.z);
    }

    float GetBodyInset()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return 0f;

        return Mathf.Max(spriteRenderer.bounds.extents.x, spriteRenderer.bounds.extents.y);
    }

    Vector2 ReadScreenInput()
    {
        if (GameplayGate.MenuOpen)
            return Vector2.zero;

        if (_moveAction != null)
            return _moveAction.ReadValue<Vector2>();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            y += 1f;

        return new Vector2(x, y);
    }

    void UpdateSprite(Vector2 screenInput, float deltaTime, float animationScale)
    {
        bool moving = screenInput.sqrMagnitude > 0.0001f;
        if (moving)
        {
            Facing facing = FacingFrom(screenInput);
            if (facing != _facing)
            {
                _facing = facing;
                _walkTime = 0f;
            }

            _walkTime += deltaTime * Mathf.Max(0.01f, animationScale);
        }
        else
        {
            _walkTime = 0f;
        }

        Sprite[] frames = FramesFor(_facing);
        if (frames == null || frames.Length == 0)
            return;

        if (!moving || frames.Length == 1 || walkFramesPerSecond <= 0f)
        {
            ShowFrame(frames, 0);
            return;
        }

        // Frame 0 is the idle pose. Frames 1 and 9 sit almost on that same stance,
        // so the stride uses only the in-between steps and loops without a pause.
        int firstStep = frames.Length > 3 ? 2 : 1;
        int lastStep = frames.Length > 3 ? frames.Length - 2 : frames.Length - 1;
        int walkCount = lastStep - firstStep + 1;
        int frame = firstStep + Mathf.FloorToInt(_walkTime * walkFramesPerSecond) % walkCount;
        ShowFrame(frames, frame);
    }

    static Facing FacingFrom(Vector2 screenInput)
    {
        if (Mathf.Abs(screenInput.x) > Mathf.Abs(screenInput.y))
            return screenInput.x > 0f ? Facing.Right : Facing.Left;

        return screenInput.y > 0f ? Facing.Up : Facing.Down;
    }

    Sprite[] FramesFor(Facing facing)
    {
        switch (facing)
        {
            case Facing.Up:
                return upFrames;
            case Facing.Left:
                return leftFrames;
            case Facing.Right:
                return rightFrames;
            default:
                return downFrames;
        }
    }

    void ShowFrame(Sprite[] frames, int index)
    {
        if (_spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, frames.Length - 1);
        Sprite sprite = frames[index];
        if (sprite != null)
            _spriteRenderer.sprite = sprite;
    }
}
