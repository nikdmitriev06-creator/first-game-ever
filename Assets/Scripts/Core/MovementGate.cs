using UnityEngine;

/// <summary>
/// One slot where the world reports whether a step is legal.
/// Player and enemies call this. They do not know tile types.
/// When nothing is registered, every step is allowed, so the existing meadow is unchanged.
/// </summary>
public static class MovementGate
{
    public delegate bool TravelCheck(Vector2 from, Vector2 to);
    public delegate float SpeedCheck(Vector2 world);

    static TravelCheck _blocks;
    static SpeedCheck _speed;

    public static void Bind(TravelCheck blocks, SpeedCheck speed)
    {
        _blocks = blocks;
        _speed = speed;
    }

    public static void Clear()
    {
        _blocks = null;
        _speed = null;
    }

    public static bool Blocked(Vector2 from, Vector2 to)
    {
        return _blocks != null && _blocks(from, to);
    }

    public static float Scale(Vector2 world)
    {
        if (_speed == null)
            return 1f;

        float scale = _speed(world);
        return scale > 0f ? scale : 1f;
    }
}
