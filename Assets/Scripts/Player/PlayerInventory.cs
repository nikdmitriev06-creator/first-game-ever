using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One row of ten slots, the hand included. Keys 1-9 and 0 select a slot.
/// I opens the row so a click can lift an item onto the cursor.
/// Q throws one of the held stack onto the ground, toward the cursor.
/// The weapon or tool in the selected slot is the one in the hand.
/// The slots themselves live in ItemContainer. This component owns input and the hand.
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

    readonly ItemContainer _bag = new ItemContainer(SlotCount);
    IWeapon[] _weapons;
    Tool[] _tools;
    int _selected;
    bool _open;
    string _cursor;
    int _cursorCount;

    public ItemContainer Container => _bag;

    public int Selected => _selected;

    public bool IsOpen => _open;

    public string CursorItem => _cursorCount > 0 ? _cursor : null;

    public int CursorCount => _cursorCount > 0 ? _cursorCount : 0;

    public string SlotAt(int index)
    {
        return _bag.IdAt(index);
    }

    public int CountAt(int index)
    {
        return _bag.CountAt(index);
    }

    /// <summary>
    /// Puts as many as the row can hold. Returns how many were accepted.
    /// </summary>
    public int TryAdd(string id, int count)
    {
        if (string.IsNullOrEmpty(id) || count <= 0)
            return 0;

        int left = _bag.Add(id, count);
        int accepted = count - left;
        if (accepted > 0)
            Notify();

        return accepted;
    }

    public int CountOf(string id)
    {
        return _bag.CountOf(id, _cursor, _cursorCount);
    }

    public bool TryConsume(string id, int count)
    {
        if (count <= 0)
            return true;

        if (!_bag.Remove(id, count, ref _cursor, ref _cursorCount))
            return false;

        Notify();
        return true;
    }

    public bool CanFitAfterSpend(string resultId, int resultCount, Ingredient[] spend)
    {
        return _bag.CanFitAfterSpend(resultId, resultCount, spend, _cursor, _cursorCount);
    }

    void Awake()
    {
        _bag.Set(0, ItemIds.Sword, 1);
        _bag.Set(1, ItemIds.Bow, 1);
        _bag.Set(2, ItemIds.Axe, 1);
        _weapons = GetComponentsInChildren<IWeapon>(true);
        _tools = GetComponentsInChildren<Tool>(true);
        Notify();
    }

    void OnDestroy()
    {
        if (_open)
            GameplayGate.EnsureState().Pop("inventory");
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (Pressed(keyboard, Key.I) && GameplayGate.EnsureState().Allows(PlayerAction.OpenInventory))
            SetOpen(!_open);

        if (Pressed(keyboard, Key.Q) && GameplayGate.EnsureState().Allows(PlayerAction.DropItem))
            ThrowHeld();

        if (!GameplayGate.EnsureState().Allows(PlayerAction.SelectSlot))
            return;

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

        string sitting = SlotAt(index);
        int sittingCount = CountAt(index);
        bool cursor = _cursorCount > 0 && !string.IsNullOrEmpty(_cursor);

        if (!cursor)
        {
            if (sittingCount <= 0)
                return;

            _cursor = sitting;
            _cursorCount = sittingCount;
            _bag.Clear(index);
        }
        else if (sittingCount <= 0)
        {
            _bag.Set(index, _cursor, _cursorCount);
            ClearCursor();
        }
        else if (sitting == _cursor && ItemIds.Stacks(sitting))
        {
            int room = ItemIds.MaxCount(sitting) - sittingCount;
            if (room <= 0)
            {
                _bag.Set(index, _cursor, _cursorCount);
                _cursor = sitting;
                _cursorCount = sittingCount;
            }
            else
            {
                int move = Mathf.Min(room, _cursorCount);
                _bag.Set(index, sitting, sittingCount + move);
                _cursorCount -= move;
                if (_cursorCount <= 0)
                    ClearCursor();
            }
        }
        else
        {
            _bag.Set(index, _cursor, _cursorCount);
            _cursor = sitting;
            _cursorCount = sittingCount;
        }

        Notify();
    }

    public void ClickOutside()
    {
        if (!_open || _cursorCount <= 0)
            return;

        if (!PlaceCursorInFirstEmpty())
            return;

        Notify();
    }

    public void SetOpen(bool open)
    {
        if (_open == open)
            return;

        if (!open && !PlaceCursorInFirstEmpty())
            return;

        _open = open;
        if (_open)
            GameplayGate.EnsureState().Push("inventory");
        else
            GameplayGate.EnsureState().Pop("inventory");

        if (_open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!_open)
            PlaceCursorInFirstEmpty();

        Notify();
    }

    void ThrowHeld()
    {
        bool fromCursor = _open && _cursorCount > 0 && !string.IsNullOrEmpty(_cursor);
        string id;
        int have;
        if (fromCursor)
        {
            id = _cursor;
            have = _cursorCount;
        }
        else
        {
            id = SlotAt(_selected);
            have = CountAt(_selected);
        }

        if (have <= 0 || string.IsNullOrEmpty(id))
            return;

        int drop = ItemIds.Stacks(id) ? 1 : have;
        if (fromCursor)
        {
            _cursorCount -= drop;
            if (_cursorCount <= 0)
                ClearCursor();
        }
        else
        {
            _bag.Set(_selected, id, have - drop);
        }

        SpawnDrop(id, drop);
        Notify();
    }

    void SpawnDrop(string id, int count)
    {
        Vector2 origin = transform.position;
        Vector2 direction = ThrowDirection(origin);
        Vector2 side = new Vector2(-direction.y, direction.x);
        Vector2 land = origin + direction * 2.05f + side * UnityEngine.Random.Range(-0.35f, 0.35f);
        WorldDropper.Spawn(id, count, origin + Vector2.up * 0.55f, land, 0.45f);
    }

    Vector2 ThrowDirection(Vector2 origin)
    {
        Camera cam = Camera.main;
        Mouse mouse = Mouse.current;
        if (cam != null && mouse != null)
        {
            Vector2 screen = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            Plane plane = new Plane(-cam.transform.forward, new Vector3(origin.x, origin.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 world = ray.GetPoint(enter);
                Vector2 delta = new Vector2(world.x - origin.x, world.y - origin.y);
                if (delta.sqrMagnitude > 0.2f)
                    return delta.normalized;
            }
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null && controller.FacingDirection.sqrMagnitude > 0.01f)
            return controller.FacingDirection.normalized;

        return Vector2.down;
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
        if (_cursorCount <= 0 || string.IsNullOrEmpty(_cursor))
            return true;

        if (_bag.SpaceFor(_cursor) < _cursorCount)
            return false;

        _bag.Add(_cursor, _cursorCount);
        ClearCursor();
        return true;
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
        if (_tools == null || _tools.Length == 0)
            _tools = GetComponentsInChildren<Tool>(true);

        string held = SlotAt(_selected);
        ItemDefinition selected = ItemCatalog.Find(held);
        bool swing = selected != null && selected.HasTool && selected.ResolvedTool.Gesture == ToolGesture.Swing;

        for (int i = 0; i < _weapons.Length; i++)
        {
            IWeapon weapon = _weapons[i];
            if (weapon == null)
                continue;

            weapon.SetHeld(!swing && weapon.ItemId == held);
        }

        if (_tools == null)
            return;

        for (int i = 0; i < _tools.Length; i++)
        {
            Tool tool = _tools[i];
            if (tool == null)
                continue;

            Axe axe = tool as Axe;
            if (axe != null)
            {
                axe.Bind(swing ? selected : null);
                continue;
            }

            tool.SetHeld(tool.ItemId == held);
        }
    }

    void ClearCursor()
    {
        _cursor = null;
        _cursorCount = 0;
    }

    static bool Pressed(Keyboard keyboard, Key key)
    {
        var control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }
}
