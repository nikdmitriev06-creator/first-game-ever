using UnityEngine;

/// <summary>
/// Removes a body that has no other break reaction, such as a rock with no drop.
/// </summary>
public class VanishOnBreak : MonoBehaviour, IBreakReaction
{
    public void OnBroken(in Impact impact)
    {
        Destroy(gameObject);
    }
}
