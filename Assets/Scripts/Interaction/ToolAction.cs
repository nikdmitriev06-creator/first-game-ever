using UnityEngine;

/// <summary>
/// A tool use that is not damage. Watering and tilling send this. They do not send <see cref="Impact"/>.
/// </summary>
public readonly struct ToolAction
{
    public readonly string ActionId;
    public readonly float Power;
    public readonly Vector2 Point;
    public readonly Vector2 Direction;
    public readonly Object Source;

    public ToolAction(string actionId, float power, Vector2 point, Vector2 direction, Object source)
    {
        ActionId = actionId;
        Power = Mathf.Max(0f, power);
        Point = point;
        Direction = direction;
        Source = source;
    }
}
