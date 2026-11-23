using UnityEngine;

/// <summary>
/// An actor that knows how to stand on a room. The spawn director does not know the actor's class.
/// </summary>
public interface IWorldSpawn
{
    void PlaceInWorld(Vector2 world, PlayArea area, Transform parent);
}
