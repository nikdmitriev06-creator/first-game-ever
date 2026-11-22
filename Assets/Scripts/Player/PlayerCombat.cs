using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared attack input. Holding the left mouse button keeps calling Attack on the weapon in the hand.
/// A tool in the hand is used the same way. Each one decides what that press does and when it can happen again.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] float attackSpeedMultiplier = 1f;

    IWeapon[] _weapons;
    Tool[] _tools;
    PlayerInventory _inventory;
    PlacementSystem _placement;
    InputAction _attackAction;

    public float AttackSpeedMultiplier
    {
        get => attackSpeedMultiplier;
        set => attackSpeedMultiplier = Mathf.Max(0.05f, value);
    }

    void Awake()
    {
        _weapons = GetComponentsInChildren<IWeapon>(true);
        _tools = GetComponentsInChildren<Tool>(true);
        _inventory = GetComponent<PlayerInventory>();
        _placement = GetComponent<PlacementSystem>();

        if (InputSystem.actions != null)
            _attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: false);
    }

    void OnEnable()
    {
        _attackAction?.Enable();
    }

    void OnDisable()
    {
        _attackAction?.Disable();
    }

    void Update()
    {
        if (GameplayGate.MenuOpen || (_inventory != null && _inventory.IsOpen))
            return;
        if (GameplayPointer.IsOverUi())
            return;
        if (_placement == null)
            _placement = GetComponent<PlacementSystem>();
        if ((_placement != null && _placement.HoldsPlaceable) || GameplayGate.UseClaimed)
            return;
        if (!AttackHeld())
            return;

        IWeapon weapon = HeldWeapon();
        if (weapon != null)
        {
            weapon.Attack();
            return;
        }

        Tool tool = HeldTool();
        if (tool != null)
            tool.Use();
    }

    IWeapon HeldWeapon()
    {
        if (_weapons == null)
            return null;

        for (int i = 0; i < _weapons.Length; i++)
        {
            IWeapon weapon = _weapons[i];
            if (weapon != null && weapon.IsHeld)
                return weapon;
        }

        return null;
    }

    Tool HeldTool()
    {
        if (_tools == null || _tools.Length == 0)
            _tools = GetComponentsInChildren<Tool>(true);
        if (_tools == null)
            return null;

        for (int i = 0; i < _tools.Length; i++)
        {
            Tool tool = _tools[i];
            if (tool != null && tool.IsHeld)
                return tool;
        }

        return null;
    }

    bool AttackHeld()
    {
        if (_attackAction != null)
            return _attackAction.IsPressed();

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }
}
