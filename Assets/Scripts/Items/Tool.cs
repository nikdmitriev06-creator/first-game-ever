using UnityEngine;

/// <summary>
/// A held tool. A weapon strikes a creature. A tool works on the world, and each kind decides what Use does.
/// </summary>
public abstract class Tool : MonoBehaviour
{
    public abstract string ItemId { get; }
    public abstract Sprite IconSprite { get; }
    public abstract bool IsHeld { get; }
    public abstract void SetHeld(bool held);
    public abstract void Use();
}
