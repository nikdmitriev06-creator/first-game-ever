using UnityEngine;

public enum MapPinKind
{
    Gate,
    Exit,
    Chest,
    Mechanism,
    Mark
}

/// <summary>
/// One discovered point stored on the room that contains it.
/// The minimap reads this list. It does not search the scene.
/// </summary>
public struct MapInterest
{
    public Vector2 Point;
    public MapPinKind Kind;
}
