using System.Collections.Generic;
using UnityEngine;

public struct AuditIssue
{
    public bool Error;
    public string Message;
}

/// <summary>
/// Editor and test checks for one room. Warnings do not block painting.
/// </summary>
public static class RoomAudit
{
    public static void Evaluate(RoomDefinition room, int difficulty, List<AuditIssue> into)
    {
        if (into == null)
            return;

        into.Clear();
        if (room == null)
        {
            into.Add(Error("Комната не выбрана."));
            return;
        }

        if (room.DefaultGround == null)
            into.Add(Error("Не задана поверхность по умолчанию."));

        var markers = room.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null)
                continue;

            if (!room.Inside(marker.x, marker.y))
            {
                into.Add(Error("Маркер " + marker.id + " вне комнаты."));
                continue;
            }

            if (marker.kind == MarkerKind.Socket)
                CheckSocket(room, marker, into);
            else if (marker.kind == MarkerKind.Spawn)
                CheckSpawn(room, marker, difficulty, into);
            else if (marker.enabled && marker.prefab == null && marker.kind != MarkerKind.Spawn)
                into.Add(Warn("У объекта " + marker.id + " нет prefab."));
        }

        CheckBridges(room, into);
    }

    static void CheckSocket(RoomDefinition room, RoomMarkerData marker, List<AuditIssue> into)
    {
        if (string.IsNullOrEmpty(marker.id))
            into.Add(Error("У выхода нет id."));

        if (!IsoSides.OnBorder(marker.x, marker.y, room.Width, room.Height, marker.direction))
            into.Add(Error("Выход " + marker.id + " не на краю " + IsoSides.Label(marker.direction) + "."));
    }

    static void CheckSpawn(RoomDefinition room, RoomMarkerData marker, int difficulty, List<AuditIssue> into)
    {
        if (!marker.enabled)
            return;

        if (marker.pool == null)
        {
            into.Add(Error("Точка " + marker.id + " без пула."));
            return;
        }

        if (marker.minCount > marker.maxCount)
            into.Add(Error("У " + marker.id + " минимум больше максимума."));

        List<SpawnEntry> available = SpawnResolver.Available(marker.pool, difficulty);
        if (available.Count == 0)
            into.Add(Warn("На сложности " + difficulty + " пул " + marker.pool.DisplayName + " пуст."));

        bool anyPrefab = false;
        IReadOnlyList<SpawnEntry> entries = marker.pool.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];
            if (entry == null || !entry.enabled)
                continue;

            if (entry.prefab == null)
                into.Add(Error("В пуле " + marker.pool.DisplayName + " у " + entry.id + " нет prefab."));
            else
                anyPrefab = true;
        }

        if (!anyPrefab)
            into.Add(Error("В пуле " + marker.pool.DisplayName + " нет ни одного prefab."));

        TerrainDefinition surface = room.SurfaceAt(marker.x, marker.y);
        bool walkable = surface == null || surface.Walkable;
        if (!walkable)
            into.Add(Error("Точка " + marker.id + " стоит на непроходимой клетке " + surface.Id + "."));

        if (surface != null && !TerrainField.Allows(marker.allowedSurfaceIds, surface.Id))
            into.Add(Warn("Клетка " + marker.id + " не входит в разрешённые поверхности."));
    }

    static void CheckBridges(RoomDefinition room, List<AuditIssue> into)
    {
        for (int y = 0; y < room.Height; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                TerrainDefinition surface = room.SurfaceAt(x, y);
                if (surface == null || surface.Passage != PassageKind.Bridge)
                    continue;

                if (!Touches(room, x, y, PassageKind.Blocked))
                    into.Add(Warn("Мост в (" + x + ", " + y + ") не касается непроходимой клетки."));
            }
        }
    }

    static bool Touches(RoomDefinition room, int x, int y, PassageKind passage)
    {
        return Is(room, x + 1, y, passage) || Is(room, x - 1, y, passage)
            || Is(room, x, y + 1, passage) || Is(room, x, y - 1, passage);
    }

    static bool Is(RoomDefinition room, int x, int y, PassageKind passage)
    {
        if (!room.Inside(x, y))
            return false;

        TerrainDefinition surface = room.SurfaceAt(x, y);
        return surface != null && surface.Passage == passage;
    }

    static AuditIssue Error(string message)
    {
        return new AuditIssue { Error = true, Message = message };
    }

    static AuditIssue Warn(string message)
    {
        return new AuditIssue { Error = false, Message = message };
    }
}
