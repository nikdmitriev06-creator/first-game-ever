using System.Collections.Generic;

/// <summary>
/// What the minimap is allowed to draw. Built from room records, not from a scene search.
/// Mode 0 shows the map, 1 leaves the caption, 2 leaves only the restore tab.
/// </summary>
public sealed class MapSketch
{
    public struct Cell
    {
        public int Column;
        public int Row;
        public bool Visited;
        public bool Current;
        public bool Exit;
        public bool Shown;
    }

    public struct Pin
    {
        public int Column;
        public int Row;
        public float X;
        public float Y;
        public MapPinKind Kind;
        public bool Shown;
    }

    public int Mode;
    public int PlayerColumn = -1;
    public int PlayerRow = -1;
    public readonly List<Cell> Cells = new List<Cell>();
    public readonly List<Pin> Pins = new List<Pin>();

    public bool ShowsMap => Mode == 0;
    public bool ShowsCaption => Mode != 2;

    public void Clear()
    {
        Cells.Clear();
        Pins.Clear();
        PlayerColumn = -1;
        PlayerRow = -1;
    }

    public int CycleMode()
    {
        Mode = (Mode + 1) % 3;
        return Mode;
    }

    public void AddCell(int column, int row, bool visited, bool current, bool exit, bool fog)
    {
        bool shown = Mode == 0 && (!fog || visited);
        Cells.Add(new Cell
        {
            Column = column,
            Row = row,
            Visited = visited,
            Current = current,
            Exit = exit,
            Shown = shown
        });

        if (current && shown)
        {
            PlayerColumn = column;
            PlayerRow = row;
        }
    }

    public void AddPin(int column, int row, float x, float y, MapPinKind kind, bool roomShown)
    {
        Pins.Add(new Pin
        {
            Column = column,
            Row = row,
            X = x,
            Y = y,
            Kind = kind,
            Shown = Mode == 0 && roomShown
        });
    }
}
