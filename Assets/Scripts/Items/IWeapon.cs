using UnityEngine;

/// <summary>
/// One held attack. Pressing the left mouse button calls Attack on whichever weapon is in the hand.
/// </summary>
public interface IWeapon
{
    string ItemId { get; }
    Sprite IconSprite { get; }
    bool IsHeld { get; }
    void SetHeld(bool held);
    void Attack();
}
