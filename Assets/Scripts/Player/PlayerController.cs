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

    [SerializeField] float moveSpeed = 6f;
    [SerializeField] PlayArea playArea = null;
    [SerializeField] float walkFramesPerSecond = 8f;
    [SerializeField] Sprite[] downFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] upFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] leftFrames = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] rightFrames = System.Array.Empty<Sprite>();

    Vector2 _isometricPosition;
    InputAction _moveAction;
    SpriteRenderer _spriteRenderer;
    Facing _facing = Facing.Down;
    float _walkTime;

    public Vector2 IsometricPosition => _isometricPosition;

    void Awake()
    {
        _isometricPosition = IsometricCoordinates.WorldToIsometric(transform.position);
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (InputSystem.actions != null)
            _moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: false);

        ShowFrame(FramesFor(_facing), 0);
    }

    void OnEnable()
    {
        _moveAction?.Enable();
    }

    void Update()
    {
        Vector2 screenInput = ReadScreenInput();
        ApplyScreenMovement(screenInput, Time.deltaTime);
        UpdateSprite(screenInput, Time.deltaTime);
    }

    void ApplyScreenMovement(Vector2 screenInput, float deltaTime)
    {
        if (screenInput.sqrMagnitude > 1f)
            screenInput.Normalize();

        if (screenInput.sqrMagnitude > 0f && deltaTime > 0f)
        {
            Vector2 screenDelta = screenInput * (moveSpeed * deltaTime);
            _isometricPosition += IsometricCoordinates.WorldToIsometric(screenDelta);
        }

        Vector2 world = IsometricCoordinates.IsometricToWorld(_isometricPosition);
        if (playArea != null)
            world = playArea.Clamp(world, GetBodyInset());

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

    void UpdateSprite(Vector2 screenInput, float deltaTime)
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

            _walkTime += deltaTime;
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
