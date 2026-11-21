using UnityEngine;

public enum ImpactOutcome
{
    Ignored,
    Damaged,
    Broken
}

public enum TargetShape
{
    Single,
    Radius
}

/// <summary>
/// One hit. The tool fills it in. The target decides whether it applies.
/// </summary>
public readonly struct Impact
{
    public readonly ImpactType Type;
    public readonly float Power;
    public readonly Vector2 Point;
    public readonly Vector2 Direction;
    public readonly Object Source;

    public Impact(ImpactType type, float power, Vector2 point, Vector2 direction, Object source)
    {
        Type = type;
        Power = Mathf.Max(0f, power);
        Point = point;
        Direction = direction;
        Source = source;
    }
}

public struct ImpactResult
{
    public ImpactOutcome Outcome;
    public float Applied;

    public static ImpactResult Ignore()
    {
        return new ImpactResult { Outcome = ImpactOutcome.Ignored, Applied = 0f };
    }

    public static ImpactResult Damage(float applied)
    {
        return new ImpactResult { Outcome = ImpactOutcome.Damaged, Applied = applied };
    }

    public static ImpactResult Break(float applied)
    {
        return new ImpactResult { Outcome = ImpactOutcome.Broken, Applied = applied };
    }
}
