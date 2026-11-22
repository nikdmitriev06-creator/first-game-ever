using UnityEngine;

/// <summary>
/// Removes a broken body. In play the destroy waits until the end of the frame, so loot on the same object still spawns.
/// </summary>
public class VanishOnBreak : MonoBehaviour, IBreakReaction
{
    public void OnBroken(in Impact impact)
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
