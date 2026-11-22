using UnityEngine;

/// <summary>
/// Something a non-damaging tool can touch. It does not have to accept impacts or store items.
/// </summary>
public interface IToolActionReceiver
{
    bool CanReceiveAction { get; }
    Vector2 Origin { get; }
    bool Apply(in ToolAction action);
}
