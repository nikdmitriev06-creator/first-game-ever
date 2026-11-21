using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One row of ten slots, the hand included. Keys 1-9 and 0 select a slot.
/// I opens the row so a click can lift an item onto the cursor.
/// The weapon in the selected slot is the one in the hand.
/// </summary>
[DefaultExecutionOrder(-20)]
public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 10;

    static readonly Key[] NumberKeys =
    {
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4,
        Key.Digit5,
        Key.Digit6,
        Key.Digit7,
        Key.Digit8,
        Key.Digit9,
        Key.Digit0
    };

    static readonly Key[] NumpadKeys =
    {
        Key.Numpad1,
        Key.Numpad2,
        Key.Numpad3,
        Key.Numpad4,
        Key.Numpad5,
        Key.Numpad6,
        Key.Numpad7,
        Key.Numpad8,
        Key.Numpad9,
        Key.Numpad0
    };

    public event Action Changed;

    readonly string[] _slots = new string[SlotCount];
    IWeapon[] _weapons;
    int _selected;
    bool _open;
    string _cursor;

    public int Selected => _selected;

    public bool IsOpen => _open;

    public string CursorItem => _cursor;

    public string SlotAt(int index)
    {
        if ((uint)index >= SlotCount)
            return null;

        return _slots[index];
    }

    void Awake()
    {
        _slots[0] = ItemIds.Sword;
        _slots[1] = ItemIds.Bow;
        _weapons = GetComponentsInChildren<IWeapon>(true);
        if (GetComponent<InventoryHud>() == null)
            gameObject.AddComponent<InventoryHud>();

        Notify();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (Pressed(keyboard, Key.I))
            SetOpen(!_open);

        for (int i = 0; i < SlotCount; i++)
        {
            if (!Pressed(keyboard, NumberKeys[i]) && !Pressed(keyboard, NumpadKeys[i]))
                continue;

            Select(i);
            break;
        }
    }

    public void ClickSlot(int index)
    {
        if (!_open || (uint)index >= SlotCount)
            return;

        string sitting = _slots[index];
        if (string.IsNullOrEmpty(_cursor))
        {
            if (string.IsNullOrEmpty(sitting))
                return;

            _cursor = sitting;
            _slots[index] = null;
        }
        else if (string.IsNullOrEmpty(sitting))
        {
            _slots[index] = _cursor;
            _cursor = null;
        }
        else
        {
            string swapped = sitting;
            _slots[index] = _cursor;
            _cursor = swapped;
        }

        Notify();
    }

    public void ClickOutside()
    {
        if (!_open || string.IsNullOrEmpty(_cursor))
            return;

        if (!PlaceCursorInFirstEmpty())
            return;

        Notify();
    }

    public void SetOpen(bool open)
    {
        if (_open == open)
            return;

        if (!open && !string.IsNullOrEmpty(_cursor) && FirstEmpty() < 0)
            return;

        _open = open;
        if (!_open)
            PlaceCursorInFirstEmpty();

        Notify();
    }

    void Select(int index)
    {
        if (_selected == index)
            return;

        _selected = index;
        Notify();
    }

    bool PlaceCursorInFirstEmpty()
    {
        if (string.IsNullOrEmpty(_cursor))
            return true;

        int slot = FirstEmpty();
        if (slot < 0)
            return false;

        _slots[slot] = _cursor;
        _cursor = null;
        return true;
    }

    int FirstEmpty()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(_slots[i]))
                return i;
        }

        return -1;
    }

    void Notify()
    {
        ApplyHeld();
        Changed?.Invoke();
    }

    void ApplyHeld()
    {
        if (_weapons == null)
            _weapons = GetComponentsInChildren<IWeapon>(true);

        string held = _slots[_selected];
        for (int i = 0; i < _weapons.Length; i++)
        {
            IWeapon weapon = _weapons[i];
            if (weapon == null)
                continue;

            weapon.SetHeld(weapon.ItemId == held);
        }
    }

    static bool Pressed(Keyboard keyboard, Key key)
    {
        var control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }
}
