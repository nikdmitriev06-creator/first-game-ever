using System;
using UnityEngine;

/// <summary>
/// Numbers for a held tool. Empty means the item is not a tool. Several items can describe a swing without a new class.
/// </summary>
[Serializable]
public class ToolProfile
{
    public ImpactType Impact;
    public float Power = 1f;
    public float Range = 3.6f;
    public float Stamina = 6f;
    public TargetShape Shape = TargetShape.Single;
    public ToolGesture Gesture = ToolGesture.Swing;

    public static ToolProfile Swing(ImpactType type, float power, float range, float stamina)
    {
        return new ToolProfile
        {
            Impact = type,
            Power = Mathf.Max(0f, power),
            Range = Mathf.Max(0.2f, range),
            Stamina = Mathf.Max(0f, stamina),
            Shape = TargetShape.Single,
            Gesture = ToolGesture.Swing
        };
    }
}

public enum ToolGesture
{
    None,
    Swing
}
