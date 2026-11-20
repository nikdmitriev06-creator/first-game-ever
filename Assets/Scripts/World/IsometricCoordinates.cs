using UnityEngine;

/// <summary>
/// 2:1 isometric projection used by the world and the player.
/// The camera stays axis-aligned. Screen up is world +Y and screen right is world +X,
/// so WASD matches the screen. Isometric X/Y are the diagonal ground axes.
/// </summary>
public static class IsometricCoordinates
{
    public const float HorizontalUnit = 1f;
    public const float VerticalUnit = 0.5f;

    public static Vector2 IsometricToWorld(Vector2 isometric)
    {
        return new Vector2(
            (isometric.x - isometric.y) * HorizontalUnit,
            (isometric.x + isometric.y) * VerticalUnit);
    }

    public static Vector2 WorldToIsometric(Vector2 world)
    {
        float alongX = world.x / HorizontalUnit;
        float alongY = world.y / VerticalUnit;
        return new Vector2(
            (alongX + alongY) * 0.5f,
            (alongY - alongX) * 0.5f);
    }
}
