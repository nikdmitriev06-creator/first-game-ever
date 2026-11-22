using System;
using System.Collections.Generic;

/// <summary>
/// Why gameplay actions are blocked. Each opener pushes its own reason and pops only that reason.
/// </summary>
public sealed class ControlState
{
    readonly List<string> _reasons = new List<string>();

    public event Action Changed;

    public int ReasonCount => _reasons.Count;

    public bool Has(string reason)
    {
        return !string.IsNullOrEmpty(reason) && _reasons.Contains(reason);
    }

    public void Push(string reason)
    {
        if (string.IsNullOrEmpty(reason) || _reasons.Contains(reason))
            return;

        _reasons.Add(reason);
        Changed?.Invoke();
    }

    public void Pop(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return;

        if (_reasons.Remove(reason))
            Changed?.Invoke();
    }

    public void Clear()
    {
        if (_reasons.Count == 0)
            return;

        _reasons.Clear();
        Changed?.Invoke();
    }

    public bool Allows(PlayerAction action)
    {
        if (_reasons.Count == 0)
            return true;

        // Drop and the inventory key stay available so a menu can be closed and a held stack thrown.
        return action == PlayerAction.DropItem || action == PlayerAction.OpenInventory;
    }
}

public enum PlayerAction
{
    Move,
    Sprint,
    UseHand,
    Interact,
    OpenInventory,
    DropItem,
    SelectSlot,
    Place
}
