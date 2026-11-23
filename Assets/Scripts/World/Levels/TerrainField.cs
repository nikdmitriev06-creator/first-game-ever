using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Walk data for one placed room. Built once from the room asset. Not a scene search.
/// </summary>
public sealed class TerrainField
{
    public struct Cell
    {
        public bool Has;
        public bool Walkable;
        public bool Ramp;
        public int Height;
        public float Speed;
        public string TerrainId;
        public string HazardId;
        public PassageKind Passage;
    }

    readonly Cell[] _cells;

    public int OriginX { get; }
    public int OriginY { get; }
    public int Width { get; }
    public int Height { get; }

    public TerrainField(int originX, int originY, int width, int height)
    {
        OriginX = originX;
        OriginY = originY;
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        _cells = new Cell[Width * Height];
    }

    public static TerrainField FromRoom(RoomDefinition room, Vector2Int origin)
    {
        if (room == null)
            return new TerrainField(origin.x, origin.y, 1, 1);

        var field = new TerrainField(origin.x, origin.y, room.Width, room.Height);
        for (int y = 0; y < field.Height; y++)
        {
            for (int x = 0; x < field.Width; x++)
                field.SetLocal(x, y, CellFrom(room.SurfaceAt(x, y)));
        }

        return field;
    }

    public static Cell CellFrom(TerrainDefinition terrain)
    {
        if (terrain == null)
        {
            return new Cell
            {
                Has = false,
                Walkable = true,
                Speed = 1f,
                Passage = PassageKind.Ground
            };
        }

        return new Cell
        {
            Has = true,
            Walkable = terrain.Walkable,
            Ramp = terrain.Ramp,
            Height = terrain.Height,
            Speed = terrain.Speed,
            TerrainId = terrain.Id,
            HazardId = terrain.HazardId,
            Passage = terrain.Passage
        };
    }

    /// <summary>
    /// A step onto a walkable cell. Height may change by one only through a ramp.
    /// Leaving a blocked cell is allowed so a bad spawn is not a trap.
    /// A missing destination is not this field's decision.
    /// </summary>
    public static bool CanStep(in Cell from, bool hasFrom, in Cell to, bool hasTo)
    {
        if (!hasTo)
            return true;

        if (!to.Walkable)
            return false;

        if (!hasFrom || !from.Walkable)
            return true;

        int delta = to.Height - from.Height;
        if (delta == 0)
            return true;

        if (Mathf.Abs(delta) > 1)
            return false;

        return from.Ramp || to.Ramp;
    }

    public bool Contains(int x, int y)
    {
        return x >= OriginX && y >= OriginY && x < OriginX + Width && y < OriginY + Height;
    }

    public bool TryGet(int x, int y, out Cell cell)
    {
        if (!Contains(x, y))
        {
            cell = default;
            return false;
        }

        cell = _cells[Index(x, y)];
        return true;
    }

    public void SetLocal(int x, int y, Cell cell)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;

        _cells[y * Width + x] = cell;
    }

    public void Collect(
        Vector2 worldCenter,
        float radius,
        string[] allowed,
        bool requireWalkable,
        List<Vector2> into)
    {
        if (into == null)
            return;

        into.Clear();
        float limit = radius > 0f ? radius * radius : -1f;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = _cells[y * Width + x];
                if (requireWalkable && !cell.Walkable)
                    continue;

                if (!Allows(allowed, cell.TerrainId))
                    continue;

                int gx = OriginX + x;
                int gy = OriginY + y;
                Vector2 center = TerrainCoordinates.CellCenterWorld(gx, gy);
                if (limit >= 0f && (center - worldCenter).sqrMagnitude > limit)
                    continue;

                into.Add(center);
            }
        }
    }

    public static bool Allows(string[] allowed, string terrainId)
    {
        if (allowed == null || allowed.Length == 0)
            return true;

        for (int i = 0; i < allowed.Length; i++)
        {
            if (!string.IsNullOrEmpty(allowed[i]) && allowed[i] == terrainId)
                return true;
        }

        return false;
    }

    int Index(int x, int y)
    {
        return (y - OriginY) * Width + (x - OriginX);
    }
}
