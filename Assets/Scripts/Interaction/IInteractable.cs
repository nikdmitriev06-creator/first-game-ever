using UnityEngine;

public sealed class InteractionContext
{
    public Transform Actor;
    public Vector2 Feet;
}

/// <summary>
/// A door, a chest, a lever. Separate from impacts: opening is not damage.
/// </summary>
public interface IInteractable
{
    bool CanInteract { get; }
    Vector2 Origin { get; }
    float Radius { get; }
    void Interact(InteractionContext context);
}
