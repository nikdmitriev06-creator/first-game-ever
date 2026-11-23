using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum SlotMove
{
    None,
    Taken,
    Placed,
    Merged,
    Swapped,
    Split,
    RefusedSpace,
    RefusedUnique
}

/// <summary>
/// Four rows. The first stays on screen as the hotbar. The other three show only while the bag is open.
/// Keys 1-9 and 0 select a hotbar slot.
/// I opens the bag so a click can lift an item onto the cursor.
/// Q throws one of the held stack onto the ground, toward the cursor.
/// The weapon or tool in the selected slot is the one in the hand.
/// The slots themselves live in ItemContainer. This component owns input and the hand.
/// </summary>
[DefaultExecutionOrder(-20)]
public class PlayerInventory : MonoBehaviour, IItemStorage
{
    public const int Columns = 10;
    public const int Rows = 4;
    public const int HotbarCount = Columns;
    public const int SlotCount = Columns * Rows;

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
    ItemInstance _cursorStack;

    public SlotMove LastMove { get; private set; }
    public int MoveSerial { get; private set; }

    public ItemContainer Container => _bag;

    public ItemContainer Contents => _bag;

    public int Selected => _selected;

    public bool IsOpen => _open;

    public string CursorItem => _cursorStack != null && _cursorStack.Quantity > 0 ? _cursorStack.DefinitionId : null;

    public int CursorCount => _cursorStack != null && _cursorStack.Quantity > 0 ? _cursorStack.Quantity : 0;

    public ItemInstance SelectedInstance => _bag.InstanceAt(_selected);

    public ItemInstance InstanceAt(int index)
    {
        return _bag.InstanceAt(index);
    }

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

    public int TryAccept(ItemInstance stack)
    {
        if (stack == null || stack.Quantity <= 0)
            return 0;

        int before = stack.Quantity;
        int left = _bag.Add(stack);
        if (left < before)
            Notify();

        return left;
    }

    public int CountOf(string id)
    {
        return _bag.CountOf(id, CursorItem, CursorCount);
    }

    public bool TryConsume(string id, int count)
    {
        if (count <= 0)
            return true;

        if (!_bag.Remove(id, count, ref _cursorStack))
            return false;

        Notify();
        return true;
    }

    public bool CanFitAfterSpend(string resultId, int resultCount, Ingredient[] spend)
    {
        return _bag.CanFitAfterSpend(resultId, resultCount, spend, CursorItem, CursorCount);
    }

    void Awake()
    {
        if (GetComponent<ProfileWeapon>() == null)
            gameObject.AddComponent<ProfileWeapon>();
        _weapons = GetComponentsInChildren<IWeapon>(true);
        _tools = GetComponentsInChildren<Tool>(true);
        GiveStartingItems();
    }

    /// <summary>
    /// The new-game hotbar. Calling it again replaces those slots.
    /// </summary>
    public void GiveStartingItems()
    {
        _bag.Set(0, ItemIds.Sword, 1);
        _bag.Set(1, ItemIds.Bow, 1);
        _bag.Set(2, ItemIds.Spear, 1);
        _bag.Set(3, ItemIds.Glaive, 1);
        _bag.Set(4, ItemIds.ShortSword, 1);
        _bag.Set(5, ItemIds.Greatsword, 1);
        _bag.Set(6, ItemIds.Staff, 1);
        _bag.Set(7, ItemIds.Crossbow, 1);
        _bag.Set(8, ItemIds.Axe, 1);
        _bag.Set(9, ItemIds.Pickaxe, 1);
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

        Mouse mouse = Mouse.current;
        if (!_open && mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel > 0.01f)
                Cycle(1);
            else if (wheel < -0.01f)
                Cycle(-1);
        }

        for (int i = 0; i < HotbarCount; i++)
        {
            if (!Pressed(keyboard, NumberKeys[i]) && !Pressed(keyboard, NumpadKeys[i]))
                continue;

            Select(i);
            break;
        }
    }

    public SlotMove ClickSlot(int index)
    {
        if ((uint)index >= SlotCount || (!_open && index >= HotbarCount))
            return Finish(SlotMove.None);

        ItemInstance sitting = _bag.InstanceAt(index);
        bool cursor = _cursorStack != null && _cursorStack.Quantity > 0;
        SlotMove move;

        if (!cursor)
        {
            if (sitting == null || sitting.Quantity <= 0)
                return Finish(SlotMove.None);

            _cursorStack = _bag.Take(index);
            move = SlotMove.Taken;
        }
        else if (sitting == null || sitting.Quantity <= 0)
        {
            _bag.SetInstance(index, _cursorStack);
            ClearCursor();
            move = SlotMove.Placed;
        }
        else if (ItemInstance.CanMerge(sitting, _cursorStack))
        {
            int room = ItemIds.MaxCount(sitting.DefinitionId) - sitting.Quantity;
            if (room <= 0)
            {
                _bag.SetInstance(index, _cursorStack);
                _cursorStack = sitting;
                move = SlotMove.Swapped;
            }
            else
            {
                int shift = Mathf.Min(room, _cursorStack.Quantity);
                sitting.SetQuantity(sitting.Quantity + shift);
                _bag.SetInstance(index, sitting);
                _cursorStack.SetQuantity(_cursorStack.Quantity - shift);
                if (_cursorStack.Quantity <= 0)
                    ClearCursor();
                move = SlotMove.Merged;
            }
        }
        else
        {
            _bag.SetInstance(index, _cursorStack);
            _cursorStack = sitting;
            move = SlotMove.Swapped;
        }

        Notify();
        return Finish(move);
    }

    /// <summary>
    /// Right click. A free cursor lifts half a plain stack. A held stack drops one into a matching slot.
    /// Weapons, worn tools and runed stacks stay whole.
    /// </summary>
    public SlotMove SplitSlot(int index)
    {
        if ((uint)index >= SlotCount || (!_open && index >= HotbarCount))
            return Finish(SlotMove.None);

        ItemInstance sitting = _bag.InstanceAt(index);
        bool cursor = _cursorStack != null && _cursorStack.Quantity > 0;
        if (!cursor)
        {
            if (sitting == null || sitting.Quantity <= 0)
                return Finish(SlotMove.None);

            if (!ItemInstance.CanSplit(sitting))
                return Finish(SlotMove.RefusedUnique);

            int take = sitting.Quantity / 2;
            if (take < 1)
                take = 1;

            sitting.SetQuantity(sitting.Quantity - take);
            _bag.SetInstance(index, sitting);
            _cursorStack = ItemInstance.Create(sitting.DefinitionId, take, -1, null);
            Notify();
            return Finish(SlotMove.Split);
        }

        if (sitting == null || sitting.Quantity <= 0)
        {
            if (!ItemInstance.CanSplit(_cursorStack))
            {
                _bag.SetInstance(index, _cursorStack);
                ClearCursor();
                Notify();
                return Finish(SlotMove.Placed);
            }

            _bag.SetInstance(index, TakeOne());
            Notify();
            return Finish(SlotMove.Split);
        }

        if (!ItemInstance.CanMerge(sitting, _cursorStack))
            return Finish(SlotMove.RefusedUnique);

        int space = ItemIds.MaxCount(sitting.DefinitionId) - sitting.Quantity;
        if (space <= 0)
            return Finish(SlotMove.RefusedSpace);

        sitting.SetQuantity(sitting.Quantity + 1);
        _bag.SetInstance(index, sitting);
        _cursorStack.SetQuantity(_cursorStack.Quantity - 1);
        if (_cursorStack.Quantity <= 0)
            ClearCursor();
        Notify();
        return Finish(SlotMove.Merged);
    }

    public SlotMove ClickOutside()
    {
        if (CursorCount <= 0)
            return Finish(SlotMove.None);

        if (!PlaceCursorInFirstEmpty())
            return Finish(SlotMove.RefusedSpace);

        Notify();
        return Finish(SlotMove.Placed);
    }

    public void SelectSlot(int index)
    {
        if ((uint)index >= HotbarCount)
            return;

        Select(index);
    }

    public void Cycle(int direction)
    {
        if (direction == 0)
            return;

        int next = _selected + (direction > 0 ? 1 : -1);
        if (next < 0)
            next = HotbarCount - 1;
        if (next >= HotbarCount)
            next = 0;

        Select(next);
    }

    public void SetOpen(bool open)
    {
        if (_open == open)
            return;

        if (!open && !PlaceCursorInFirstEmpty())
        {
            Finish(SlotMove.RefusedSpace);
            return;
        }

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
        bool fromCursor = _open && _cursorStack != null && _cursorStack.Quantity > 0;
        ItemInstance stack = fromCursor ? _cursorStack : _bag.InstanceAt(_selected);
        if (stack == null || stack.Quantity <= 0 || string.IsNullOrEmpty(stack.DefinitionId))
            return;

        bool plainStack = ItemIds.Stacks(stack.DefinitionId) && !stack.TracksDurability && !stack.HasModifiers;
        if (plainStack)
        {
            string id = stack.DefinitionId;
            stack.SetQuantity(stack.Quantity - 1);
            if (stack.Quantity <= 0)
            {
                if (fromCursor)
                    ClearCursor();
                else
                    _bag.Clear(_selected);
            }
            else if (!fromCursor)
            {
                _bag.SetInstance(_selected, stack);
            }

            SpawnDrop(id, 1);
        }
        else if (fromCursor)
        {
            _cursorStack = null;
            SpawnInstance(stack);
        }
        else
        {
            _bag.Take(_selected);
            SpawnInstance(stack);
        }

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

    void SpawnInstance(ItemInstance stack)
    {
        if (stack == null || stack.Quantity <= 0)
            return;

        Vector2 origin = transform.position;
        Vector2 direction = ThrowDirection(origin);
        Vector2 side = new Vector2(-direction.y, direction.x);
        Vector2 land = origin + direction * 2.05f + side * UnityEngine.Random.Range(-0.35f, 0.35f);
        WorldItem drop = WorldDropper.Spawn(stack.DefinitionId, stack.Quantity, origin + Vector2.up * 0.55f, land, 0.45f);
        if (drop != null)
            drop.Attach(stack);
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
        if (_cursorStack == null || _cursorStack.Quantity <= 0)
            return true;

        if (_bag.SpaceFor(_cursorStack.DefinitionId) < _cursorStack.Quantity)
            return false;

        int left = _bag.Add(_cursorStack);
        if (left > 0)
            return false;

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

            weapon.SetHeld(!swing && weapon.Wants(held));
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
        _cursorStack = null;
    }

    ItemInstance TakeOne()
    {
        if (_cursorStack == null || _cursorStack.Quantity <= 0)
            return null;

        if (_cursorStack.Quantity == 1)
        {
            ItemInstance whole = _cursorStack;
            ClearCursor();
            return whole;
        }

        ItemInstance one = ItemInstance.Create(_cursorStack.DefinitionId, 1, -1, null);
        _cursorStack.SetQuantity(_cursorStack.Quantity - 1);
        return one;
    }

    SlotMove Finish(SlotMove move)
    {
        LastMove = move;
        if (move != SlotMove.None)
            MoveSerial++;
        return move;
    }

    static bool Pressed(Keyboard keyboard, Key key)
    {
        var control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }
}
