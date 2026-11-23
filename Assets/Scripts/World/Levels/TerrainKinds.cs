using UnityEngine;

/// <summary>
/// Isometric grid directions. +X is screen north-east, +Y is screen north-west.
/// These match <see cref="IsometricCoordinates"/>, not the screen WASD axes.
/// </summary>
public enum IsoDirection
{
    NorthEast = 0,
    NorthWest = 1,
    SouthWest = 2,
    SouthEast = 3
}

public enum PassageKind
{
    Ground = 0,
    Blocked = 1,
    Ramp = 2,
    Bridge = 3
}

public enum RoomLayer
{
    Ground = 0,
    Details = 1,
    Terrain = 2,
    Objects = 3,
    Spawns = 4,
    Decorations = 5,
    Special = 6
}

public enum RoomCategory
{
    Any = 0,
    Clearing = 1,
    Corridor = 2,
    Cave = 3,
    River = 4,
    Resource = 5,
    Elite = 6,
    Event = 7,
    Boss = 8
}

public enum MarkerKind
{
    Object = 0,
    Spawn = 1,
    Socket = 2,
    Decor = 3,
    Special = 4
}

public enum SpawnCategory
{
    Monster = 0,
    Elite = 1,
    Special = 2,
    Boss = 3,
    Resource = 4,
    Container = 5,
    Event = 6,
    Prop = 7
}

public enum SpawnRepeat
{
    OncePerVisit = 0,
    ManualOnly = 1
}

public static class IsoSides
{
    public static Vector2Int Step(IsoDirection direction)
    {
        switch (direction)
        {
            case IsoDirection.NorthEast:
                return new Vector2Int(1, 0);
            case IsoDirection.NorthWest:
                return new Vector2Int(0, 1);
            case IsoDirection.SouthWest:
                return new Vector2Int(-1, 0);
            default:
                return new Vector2Int(0, -1);
        }
    }

    public static IsoDirection Opposite(IsoDirection direction)
    {
        return (IsoDirection)(((int)direction + 2) & 3);
    }

    public static string Label(IsoDirection direction)
    {
        switch (direction)
        {
            case IsoDirection.NorthEast:
                return "Северо-восток (+X)";
            case IsoDirection.NorthWest:
                return "Северо-запад (+Y)";
            case IsoDirection.SouthWest:
                return "Юго-запад (-X)";
            default:
                return "Юго-восток (-Y)";
        }
    }

    public static bool OnBorder(int x, int y, int width, int height, IsoDirection direction)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return false;

        switch (direction)
        {
            case IsoDirection.NorthEast:
                return x == width - 1;
            case IsoDirection.SouthWest:
                return x == 0;
            case IsoDirection.NorthWest:
                return y == height - 1;
            default:
                return y == 0;
        }
    }
}
