using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared attack input. The left mouse button calls Attack on the weapon in the hand.
/// Each weapon decides what that attack does.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] float attackSpeedMultiplier = 1f;

    IWeapon[] _weapons;
    PlayerInventory _inventory;
    InputAction _attackAction;

    public float AttackSpeedMultiplier
    {
        get => attackSpeedMultiplier;
        set => attackSpeedMultiplier = Mathf.Max(0.05f, value);
    }

    void Awake()
    {
        _weapons = GetComponentsInChildren<IWeapon>(true);
        _inventory = GetComponent<PlayerInventory>();

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
        if (_inventory != null && _inventory.IsOpen)
            return;
        if (!AttackPressed())
            return;

        IWeapon weapon = HeldWeapon();
        if (weapon == null)
            return;

        weapon.Attack();
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

    bool AttackPressed()
    {
        if (_attackAction != null)
            return _attackAction.WasPressedThisFrame();

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }
}
