using UnityEngine;

/// <summary>
/// Cell math for the isometric grid. Cell (x, y) is the unit square
/// [x, x+1) by [y, y+1) in <see cref="IsometricCoordinates"/>.
/// A Unity Grid with isometric layout and cell size (2, 1) uses the same centers.
/// </summary>
public static class TerrainCoordinates
{
    public const float CellWidth = 2f;
    public const float CellHeight = 1f;

    public static Vector2 CellCenterWorld(int x, int y)
    {
        return IsometricCoordinates.IsometricToWorld(new Vector2(x + 0.5f, y + 0.5f));
    }

    public static Vector2 CornerWorld(int x, int y)
    {
        return IsometricCoordinates.IsometricToWorld(new Vector2(x, y));
    }

    public static Vector2Int WorldToCell(Vector2 world)
    {
        Vector2 iso = IsometricCoordinates.WorldToIsometric(world);
        return new Vector2Int(Mathf.FloorToInt(iso.x), Mathf.FloorToInt(iso.y));
    }

    public static void WriteDiamond(int x, int y, Vector3[] corners)
    {
        Vector2 south = CornerWorld(x, y);
        Vector2 east = CornerWorld(x + 1, y);
        Vector2 north = CornerWorld(x + 1, y + 1);
        Vector2 west = CornerWorld(x, y + 1);
        corners[0] = south;
        corners[1] = east;
        corners[2] = north;
        corners[3] = west;
    }

    public static void Apply(Grid grid)
    {
        if (grid == null)
            return;

        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        grid.cellSize = new Vector3(CellWidth, CellHeight, 1f);
        grid.cellGap = Vector3.zero;
    }
}
