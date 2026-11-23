using UnityEngine;

public sealed class InteractionContext
{
    public Transform Actor;
    public Vector2 Feet;
}

/// <summary>
/// A chest, a lever, a campfire. Opening is not a weapon impact.
/// </summary>
public interface IInteractable
{
    bool CanInteract { get; }
    Vector2 Origin { get; }
    float Radius { get; }
    string Label { get; }
    string Verb { get; }
    string Refusal { get; }
    void Interact(InteractionContext context);
}
