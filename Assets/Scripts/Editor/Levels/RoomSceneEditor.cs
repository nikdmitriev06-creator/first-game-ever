using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static class RoomEdits
{
    public static void Apply(RoomDefinition room, Vector2Int cell)
    {
        if (room == null || !room.Inside(cell.x, cell.y))
            return;

        Undo.RecordObject(room, "Комната");
        if (RoomEditSession.Tool == RoomTool.Erase)
        {
            if (RoomEditSession.TileLayer)
                room.ClearTile(cell.x, cell.y, RoomEditSession.Layer);
            else
                RemoveAt(room, cell);
        }
        else if (RoomEditSession.Tool == RoomTool.Paint && RoomEditSession.TileLayer)
            room.SetTile(cell.x, cell.y, RoomEditSession.Layer, RoomEditSession.Terrain);
        else if (RoomEditSession.Tool == RoomTool.Select)
            SelectAt(room, cell);
        else
            Stamp(room, cell);

        EditorUtility.SetDirty(room);
        RoomDocument.Find(room)?.Repaint();
    }

    public static void DeleteSelected(RoomDefinition room)
    {
        if (room == null)
            return;

        Undo.RecordObject(room, "Удалить маркеры");
        for (int i = 0; i < RoomEditSession.Selected.Count; i++)
            room.RemoveMarker(RoomEditSession.Selected[i]);

        RoomEditSession.Selected.Clear();
        EditorUtility.SetDirty(room);
        RoomDocument.Find(room)?.Repaint();
    }

    public static void DuplicateSelected(RoomDefinition room)
    {
        if (room == null)
            return;

        Undo.RecordObject(room, "Дублировать маркеры");
        var copies = new List<string>();
        for (int i = 0; i < RoomEditSession.Selected.Count; i++)
        {
            RoomMarkerData copy = room.DuplicateMarker(RoomEditSession.Selected[i]);
            if (copy != null)
                copies.Add(copy.id);
        }

        RoomEditSession.Selected.Clear();
        RoomEditSession.Selected.AddRange(copies);
        EditorUtility.SetDirty(room);
        RoomDocument.Find(room)?.Repaint();
    }

    public static void Nudge(RoomDefinition room, int dx, int dy)
    {
        if (room == null)
            return;

        Undo.RecordObject(room, "Сдвинуть маркеры");
        for (int i = 0; i < RoomEditSession.Selected.Count; i++)
        {
            RoomMarkerData marker = room.FindMarker(RoomEditSession.Selected[i]);
            if (marker != null)
                room.MoveMarker(marker.id, marker.x + dx, marker.y + dy);
        }

        EditorUtility.SetDirty(room);
        RoomDocument.Find(room)?.Repaint();
    }

    static void Stamp(RoomDefinition room, Vector2Int cell)
    {
        RemoveAt(room, cell);
        var marker = new RoomMarkerData
        {
            x = cell.x,
            y = cell.y,
            enabled = true
        };

        if (RoomEditSession.Tool == RoomTool.Spawn)
        {
            marker.kind = MarkerKind.Spawn;
            marker.pool = RoomEditSession.Pool;
            marker.category = RoomEditSession.Category;
            marker.minCount = RoomEditSession.MinCount;
            marker.maxCount = Mathf.Max(RoomEditSession.MinCount, RoomEditSession.MaxCount);
            marker.radius = RoomEditSession.Radius;
            marker.minDifficulty = 1;
        }
        else if (RoomEditSession.Tool == RoomTool.Socket)
        {
            marker.kind = MarkerKind.Socket;
            marker.direction = RoomEditSession.Direction;
            marker.required = RoomEditSession.SocketRequired;
            marker.socketTag = RoomEditSession.SocketTag;
        }
        else
        {
            marker.kind = RoomEditSession.Layer == RoomLayer.Decorations ? MarkerKind.Decor
                : RoomEditSession.Layer == RoomLayer.Special ? MarkerKind.Special
                : MarkerKind.Object;
            marker.prefab = RoomEditSession.Prefab;
        }

        room.AddMarker(marker);
        RoomEditSession.SelectOnly(marker.id);
    }

    static void SelectAt(RoomDefinition room, Vector2Int cell)
    {
        IReadOnlyList<RoomMarkerData> markers = room.Markers;
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            RoomMarkerData marker = markers[i];
            if (marker != null && marker.x == cell.x && marker.y == cell.y)
            {
                if (Event.current != null && Event.current.shift)
                    RoomEditSession.Toggle(marker.id);
                else
                    RoomEditSession.SelectOnly(marker.id);
                return;
            }
        }
    }

    static void RemoveAt(RoomDefinition room, Vector2Int cell)
    {
        IReadOnlyList<RoomMarkerData> markers = room.Markers;
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            RoomMarkerData marker = markers[i];
            if (marker != null && marker.x == cell.x && marker.y == cell.y && marker.kind != MarkerKind.Socket)
                room.RemoveMarker(marker.id);
        }
    }
}

[CustomEditor(typeof(RoomDocument))]
public class RoomSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var document = (RoomDocument)target;
        if (GUILayout.Button("Обновить карту"))
            document.Rebuild();

        if (GUILayout.Button("Открыть редактор уровней"))
            LevelEditorWindow.Open(document.Definition);
    }

    void OnSceneGUI()
    {
        var document = (RoomDocument)target;
        RoomDefinition room = document.Definition;
        if (room == null)
            return;

        DrawGrid(room);
        Event current = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(control);
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Delete)
        {
            RoomEdits.DeleteSelected(room);
            current.Use();
            return;
        }

        if (current.alt || current.button != 0)
            return;

        bool drag = current.type == EventType.MouseDrag && (RoomEditSession.Tool == RoomTool.Paint || RoomEditSession.Tool == RoomTool.Erase);
        bool click = current.type == EventType.MouseDown;
        if (!drag && !click)
            return;

        Vector2 world = PlanePoint(current.mousePosition);
        RoomEdits.Apply(room, TerrainCoordinates.WorldToCell(world));
        current.Use();
    }

    static void DrawGrid(RoomDefinition room)
    {
        var corners = new Vector3[4];
        Handles.color = new Color(1f, 1f, 1f, 0.25f);
        for (int y = 0; y < room.Height; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                TerrainCoordinates.WriteDiamond(x, y, corners);
                Handles.DrawLine(corners[0], corners[1]);
                Handles.DrawLine(corners[1], corners[2]);
                Handles.DrawLine(corners[2], corners[3]);
                Handles.DrawLine(corners[3], corners[0]);
                TerrainDefinition surface = room.SurfaceAt(x, y);
                if (surface != null && !surface.Walkable)
                {
                    Handles.color = new Color(0.85f, 0.2f, 0.15f, 0.9f);
                    Handles.DrawLine(corners[0], corners[2]);
                    Handles.color = new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }
    }

    static Vector2 PlanePoint(Vector2 gui)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(gui);
        if (Mathf.Abs(ray.direction.z) < 0.0001f)
            return ray.origin;

        float distance = -ray.origin.z / ray.direction.z;
        Vector3 point = ray.origin + ray.direction * distance;
        return point;
    }
}
