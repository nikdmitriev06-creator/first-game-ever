using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Paints a room into isometric tilemaps. Ground, details, and terrain are separate maps.
/// </summary>
public static class RoomMapBuilder
{
    public const int GroundOrder = IsoSort.FloorOrder;
    public const int DetailOrder = IsoSort.FloorOrder + 1;
    public const int TerrainOrder = IsoSort.FloorOrder + 2;

    public static Grid EnsureGrid(Transform parent)
    {
        Transform existing = parent.Find("Map");
        GameObject host = existing != null ? existing.gameObject : new GameObject("Map");
        if (existing == null)
            host.transform.SetParent(parent, false);

        Grid grid = host.GetComponent<Grid>();
        if (grid == null)
            grid = host.AddComponent<Grid>();

        TerrainCoordinates.Apply(grid);
        return grid;
    }

    public static Tilemap Layer(Transform grid, string name, int order)
    {
        Transform child = grid.Find(name);
        GameObject host = child != null ? child.gameObject : new GameObject(name);
        if (child == null)
            host.transform.SetParent(grid, false);

        Tilemap map = host.GetComponent<Tilemap>();
        if (map == null)
            map = host.AddComponent<Tilemap>();

        TilemapRenderer renderer = host.GetComponent<TilemapRenderer>();
        if (renderer == null)
            renderer = host.AddComponent<TilemapRenderer>();

        renderer.sortingOrder = order;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return map;
    }

    public static void PaintRoom(RoomDefinition room, Vector2Int origin, Tilemap ground, Tilemap details, Tilemap terrain)
    {
        if (room == null)
            return;

        for (int y = 0; y < room.Height; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                Vector3Int cell = new Vector3Int(origin.x + x, origin.y + y, 0);
                Set(ground, cell, room.Overlay(x, y, RoomLayer.Ground) ?? room.DefaultGround);
                Set(details, cell, room.Overlay(x, y, RoomLayer.Details));
                Set(terrain, cell, room.Overlay(x, y, RoomLayer.Terrain));
            }
        }

        Refresh(ground);
        Refresh(details);
        Refresh(terrain);
    }

    public static void Set(Tilemap map, Vector3Int cell, TerrainDefinition terrain)
    {
        if (map == null)
            return;

        map.SetTile(cell, terrain != null ? terrain.Tile : null);
    }

    public static void Refresh(Tilemap map)
    {
        if (map != null)
            map.RefreshAllTiles();
    }
}
