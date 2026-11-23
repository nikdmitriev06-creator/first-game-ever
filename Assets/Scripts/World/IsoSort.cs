using UnityEngine;

/// <summary>
/// One depth rule for the isometric ground: a smaller Y is closer to the camera and draws in front.
/// Props, creatures and the player share it, so they do not all collapse onto two orders.
/// </summary>
public static class IsoSort
{
    public const float UnitsPerOrder = 20f;

    // Actors stay inside this band. The floor is a full-room sprite, so it has to sit
    // underneath every one of them or it paints over the character and the props.
    public const int ActorMin = -30000;
    public const int ActorMax = 30000;
    public const int FloorOrder = -32000;

    public static int Order(float footY, int bias = 0)
    {
        int order = Mathf.RoundToInt(-footY * UnitsPerOrder) + bias;
        if (order < ActorMin)
            return ActorMin;
        if (order > ActorMax)
            return ActorMax;
        return order;
    }

    /// <summary>
    /// Same depth rule, with a stable one-step tie break so two props on the same line do not flicker.
    /// The player stays on <see cref="Order"/> and remains the reference.
    /// </summary>
    public static int Stable(float footY, int salt, int bias = 0)
    {
        int nudge = (salt & 1) == 0 ? 0 : salt < 0 ? -1 : 1;
        return Order(footY, bias + nudge);
    }

    /// <summary>
    /// Feet of a sprite. A pivot near the bottom uses the transform, so a walking cycle does not change depth.
    /// A centered pivot uses the bottom of the sprite instead.
    /// </summary>
    public static float SortY(Transform root, SpriteRenderer renderer)
    {
        if (root == null)
            return 0f;
        if (renderer == null || renderer.sprite == null)
            return root.position.y;

        Sprite sprite = renderer.sprite;
        float pivot = sprite.rect.height > 0f ? sprite.pivot.y / sprite.rect.height : 0f;
        if (pivot <= 0.25f)
            return root.position.y;

        return root.position.y + sprite.bounds.min.y * Mathf.Abs(root.lossyScale.y);
    }
}

/// <summary>
/// Keeps the player on the same depth rule as the world.
/// </summary>
public class IsoSortBody : MonoBehaviour
{
    public int Bias;

    SpriteRenderer _renderer;

    void LateUpdate()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            return;

        _renderer.sortingOrder = IsoSort.Order(transform.position.y, Bias);
    }
}
