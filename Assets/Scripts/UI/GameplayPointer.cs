using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One answer for "is the pointer over a gameplay widget". Combat does not name each button.
/// </summary>
public static class GameplayPointer
{
    public static bool IsOverUi()
    {
        return WorldMenu.PointerIsOver() || SlimeSpawnButton.PointerIsOver();
    }
}
