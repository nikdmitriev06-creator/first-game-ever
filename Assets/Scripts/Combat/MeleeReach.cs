using UnityEngine;

/// <summary>
/// Shared melee volume. Weapons differ by range and arc, not by a separate hit class.
/// Arc is the full angle. A point straight ahead is inside. A point past half the arc is outside.
/// </summary>
public static class MeleeReach
{
    public static bool Contains(Vector2 origin, Vector2 aim, Vector2 point, float range, float arcDegrees)
    {
        if (range <= 0f)
            return false;

        Vector2 delta = point - origin;
        float distance = delta.magnitude;
        if (distance > range)
            return false;

        if (distance < 0.0001f)
            return true;

        if (aim.sqrMagnitude < 0.0001f)
            aim = Vector2.right;

        float limit = Mathf.Clamp(arcDegrees, 1f, 360f) * 0.5f;
        return Vector2.Angle(aim, delta) <= limit;
    }
}
