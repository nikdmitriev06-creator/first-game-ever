using UnityEngine;

/// <summary>
/// Anything a tool or a weapon arc can strike. Trees, furniture, and creatures do not share a base class.
/// </summary>
public interface IImpactReceiver
{
    bool CanReceive { get; }
    Vector2 Origin { get; }
    float Height { get; }
    float Slop { get; }
    ImpactResult Receive(in Impact impact);
}
