using System.Collections.Generic;
using UnityEngine;

public sealed class LayoutNode
{
    public RoomDefinition Room;
    public Vector2Int Origin;
}

public sealed class LayoutLink
{
    public int From;
    public int To;
    public string FromSocket;
    public string ToSocket;
}

public sealed class LayoutResult
{
    public readonly List<LayoutNode> Nodes = new List<LayoutNode>();
    public readonly List<LayoutLink> Links = new List<LayoutLink>();
    public readonly List<string> Problems = new List<string>();
    public bool Ok;
    public int Seed;
}

/// <summary>
/// Joins authored rooms by matching sockets. The same seed repeats the same arrangement.
/// </summary>
public static class RoomLayoutBuilder
{
    struct OpenSocket
    {
        public int Node;
        public string SocketId;
    }

    public static LayoutResult Build(LevelDefinition level, int seed)
    {
        var result = new LayoutResult { Seed = seed };
        if (level == null || level.StartRoom == null)
        {
            result.Problems.Add("Нет стартовой комнаты.");
            return result;
        }

        var random = new System.Random(seed);
        result.Nodes.Add(new LayoutNode { Room = level.StartRoom, Origin = Vector2Int.zero });

        int target = Mathf.Clamp(level.RollCount(random), 1, 64);
        int must = result.Nodes.Count + RequiredMissing(level, result.Nodes).Count;
        if (must > target)
            target = Mathf.Min(64, must);

        var search = new System.Random(seed);
        if (!Search(level, result, target, search, 0))
            result.Problems.Add("Не удалось соединить комнаты без пересечений и открытых обязательных выходов.");

        Finish(level, result);
        return result;
    }

    static bool Search(LevelDefinition level, LayoutResult result, int target, System.Random random, int depth)
    {
        if (result.Nodes.Count >= target)
            return RequiredSatisfied(level, result);

        if (depth > 4000)
            return false;

        List<OpenSocket> opens = OpenSockets(result, null);
        if (opens.Count == 0)
            return false;

        Shuffle(opens, random);
        for (int o = 0; o < opens.Count; o++)
        {
            OpenSocket open = opens[o];
            LayoutNode node = result.Nodes[open.Node];
            if (!TrySocket(node.Room, open.SocketId, out RoomMarkerData socket))
                continue;

            List<RoomDefinition> candidates = Candidates(level, result, target);
            Shuffle(candidates, random);
            for (int c = 0; c < candidates.Count; c++)
            {
                RoomDefinition room = candidates[c];
                if (room == null || CountOf(result, room) >= level.RepeatLimit())
                    continue;

                List<RoomMarkerData> sockets = Sockets(room);
                for (int s = 0; s < sockets.Count; s++)
                {
                    RoomMarkerData other = sockets[s];
                    if (!Compatible(node.Room, socket, room, other))
                        continue;

                    Vector2Int origin = OriginFor(node.Origin, socket, other);
                    if (OverlapsAny(result, origin, room))
                        continue;

                    result.Nodes.Add(new LayoutNode { Room = room, Origin = origin });
                    result.Links.Add(new LayoutLink
                    {
                        From = open.Node,
                        To = result.Nodes.Count - 1,
                        FromSocket = socket.id,
                        ToSocket = other.id
                    });
                    if (Search(level, result, target, random, depth + 1))
                        return true;

                    result.Links.RemoveAt(result.Links.Count - 1);
                    result.Nodes.RemoveAt(result.Nodes.Count - 1);
                }
            }
        }

        return false;
    }

    static bool RequiredSatisfied(LevelDefinition level, LayoutResult result)
    {
        if (level.EndRoom != null && CountOf(result, level.EndRoom) == 0)
            return false;

        RoomDefinition[] required = level.RequiredRooms;
        for (int i = 0; i < required.Length; i++)
        {
            if (required[i] != null && CountOf(result, required[i]) == 0)
                return false;
        }

        var markers = new List<RoomMarkerData>();
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            RoomDefinition room = result.Nodes[i].Room;
            if (room == null)
                continue;

            room.Collect(MarkerKind.Socket, markers);
            for (int s = 0; s < markers.Count; s++)
            {
                RoomMarkerData socket = markers[s];
                if (socket != null && socket.required && !string.IsNullOrEmpty(socket.id) && !Linked(result, i, socket.id))
                    return false;
            }
        }

        return true;
    }

    public static LayoutResult FromPlacements(IList<LayoutPlacement> placements, int seed)
    {
        var result = new LayoutResult { Seed = seed };
        if (placements == null)
        {
            result.Problems.Add("Макет пуст.");
            return result;
        }

        for (int i = 0; i < placements.Count; i++)
        {
            LayoutPlacement placement = placements[i];
            if (placement == null || placement.room == null)
                continue;

            result.Nodes.Add(new LayoutNode { Room = placement.room, Origin = placement.origin });
        }

        LinkExisting(result);
        ReportSockets(result);
        ReportOverlaps(result);
        result.Ok = result.Problems.Count == 0 && result.Nodes.Count > 0;
        return result;
    }

    static List<RoomDefinition> Candidates(LevelDefinition level, LayoutResult result, int target)
    {
        List<RoomDefinition> missing = RequiredMissing(level, result.Nodes);
        int remaining = target - result.Nodes.Count;
        if (missing.Count > 0 && remaining <= missing.Count)
            return missing;

        if (remaining == 1 && level.EndRoom != null && CountOf(result, level.EndRoom) == 0)
        {
            var endOnly = new List<RoomDefinition> { level.EndRoom };
            return endOnly;
        }

        var list = new List<RoomDefinition>();
        AddUnique(list, level.Rooms);
        AddUnique(list, level.RequiredRooms);
        AddUnique(list, new[] { level.StartRoom, level.EndRoom });
        return list;
    }

    static List<RoomDefinition> RequiredMissing(LevelDefinition level, List<LayoutNode> nodes)
    {
        var missing = new List<RoomDefinition>();
        AddIfMissing(missing, nodes, level.EndRoom);
        RoomDefinition[] required = level.RequiredRooms;
        for (int i = 0; i < required.Length; i++)
            AddIfMissing(missing, nodes, required[i]);
        return missing;
    }

    static void AddIfMissing(List<RoomDefinition> into, List<LayoutNode> nodes, RoomDefinition room)
    {
        if (room == null || Contains(into, room))
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Room == room)
                return;
        }

        into.Add(room);
    }

    static void Finish(LevelDefinition level, LayoutResult result)
    {
        if (level.EndRoom != null && CountOf(result, level.EndRoom) == 0)
            result.Problems.Add("Финальная комната не поставлена.");

        RoomDefinition[] required = level.RequiredRooms;
        for (int i = 0; i < required.Length; i++)
        {
            if (required[i] != null && CountOf(result, required[i]) == 0)
                result.Problems.Add("Нет обязательной комнаты " + required[i].DisplayName + ".");
        }

        ReportSockets(result);
        ReportOverlaps(result);
        result.Ok = result.Problems.Count == 0;
    }

    static void ReportSockets(LayoutResult result)
    {
        var markers = new List<RoomMarkerData>();
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            RoomDefinition room = result.Nodes[i].Room;
            if (room == null)
                continue;

            room.Collect(MarkerKind.Socket, markers);
            for (int s = 0; s < markers.Count; s++)
            {
                RoomMarkerData socket = markers[s];
                if (socket == null || !socket.required)
                    continue;

                if (!Linked(result, i, socket.id))
                    result.Problems.Add("Открыт обязательный выход " + socket.id + " у " + room.DisplayName + ".");
            }
        }
    }

    static void ReportOverlaps(LayoutResult result)
    {
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            for (int j = i + 1; j < result.Nodes.Count; j++)
            {
                if (Overlaps(result.Nodes[i], result.Nodes[j]))
                    result.Problems.Add("Комнаты пересекаются: " + i + " и " + j + ".");
            }
        }
    }

    static void LinkExisting(LayoutResult result)
    {
        var markers = new List<RoomMarkerData>();
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            RoomDefinition room = result.Nodes[i].Room;
            if (room == null)
                continue;

            room.Collect(MarkerKind.Socket, markers);
            for (int s = 0; s < markers.Count; s++)
            {
                RoomMarkerData socket = markers[s];
                if (socket == null || Linked(result, i, socket.id))
                    continue;

                for (int j = i + 1; j < result.Nodes.Count; j++)
                {
                    RoomDefinition otherRoom = result.Nodes[j].Room;
                    if (otherRoom == null)
                        continue;

                    var others = new List<RoomMarkerData>();
                    otherRoom.Collect(MarkerKind.Socket, others);
                    for (int o = 0; o < others.Count; o++)
                    {
                        RoomMarkerData other = others[o];
                        if (other == null || Linked(result, j, other.id))
                            continue;

                        if (!Compatible(room, socket, otherRoom, other))
                            continue;

                        Vector2Int expected = OriginFor(result.Nodes[i].Origin, socket, other);
                        if (expected != result.Nodes[j].Origin)
                            continue;

                        result.Links.Add(new LayoutLink
                        {
                            From = i,
                            To = j,
                            FromSocket = socket.id,
                            ToSocket = other.id
                        });
                    }
                }
            }
        }
    }

    public static bool Compatible(RoomDefinition aRoom, RoomMarkerData a, RoomDefinition bRoom, RoomMarkerData b)
    {
        if (a == null || b == null || aRoom == null || bRoom == null)
            return false;

        if (b.direction != IsoSides.Opposite(a.direction))
            return false;

        if (!Tags(a.socketTag, b.socketTag))
            return false;

        if (!IsoSides.OnBorder(a.x, a.y, aRoom.Width, aRoom.Height, a.direction))
            return false;

        if (!IsoSides.OnBorder(b.x, b.y, bRoom.Width, bRoom.Height, b.direction))
            return false;

        return true;
    }

    public static Vector2Int OriginFor(Vector2Int aOrigin, RoomMarkerData a, RoomMarkerData b)
    {
        Vector2Int aCell = aOrigin + new Vector2Int(a.x, a.y);
        Vector2Int bCell = aCell + IsoSides.Step(a.direction);
        return bCell - new Vector2Int(b.x, b.y);
    }

    static bool Tags(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return true;

        return a == b;
    }

    static List<OpenSocket> OpenSockets(LayoutResult result, HashSet<string> failed)
    {
        var opens = new List<OpenSocket>();
        var markers = new List<RoomMarkerData>();
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            if (result.Nodes[i].Room == null)
                continue;

            result.Nodes[i].Room.Collect(MarkerKind.Socket, markers);
            for (int s = 0; s < markers.Count; s++)
            {
                RoomMarkerData socket = markers[s];
                if (socket == null || string.IsNullOrEmpty(socket.id))
                    continue;

                if (Linked(result, i, socket.id))
                    continue;

                string key = i + ":" + socket.id;
                if (failed != null && failed.Contains(key))
                    continue;

                opens.Add(new OpenSocket { Node = i, SocketId = socket.id });
            }
        }

        return opens;
    }

    static bool Linked(LayoutResult result, int node, string socketId)
    {
        for (int i = 0; i < result.Links.Count; i++)
        {
            LayoutLink link = result.Links[i];
            if (link.From == node && link.FromSocket == socketId)
                return true;
            if (link.To == node && link.ToSocket == socketId)
                return true;
        }

        return false;
    }

    static bool TrySocket(RoomDefinition room, string socketId, out RoomMarkerData socket)
    {
        socket = room != null ? room.FindMarker(socketId) : null;
        return socket != null && socket.kind == MarkerKind.Socket;
    }

    static List<RoomMarkerData> Sockets(RoomDefinition room)
    {
        var list = new List<RoomMarkerData>();
        if (room != null)
            room.Collect(MarkerKind.Socket, list);
        return list;
    }

    static int CountOf(LayoutResult result, RoomDefinition room)
    {
        int count = 0;
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            if (result.Nodes[i].Room == room)
                count++;
        }

        return count;
    }

    static bool OverlapsAny(LayoutResult result, Vector2Int origin, RoomDefinition room)
    {
        var node = new LayoutNode { Room = room, Origin = origin };
        for (int i = 0; i < result.Nodes.Count; i++)
        {
            if (Overlaps(node, result.Nodes[i]))
                return true;
        }

        return false;
    }

    static bool Overlaps(LayoutNode a, LayoutNode b)
    {
        if (a.Room == null || b.Room == null)
            return false;

        int ax = a.Origin.x;
        int ay = a.Origin.y;
        int bx = b.Origin.x;
        int by = b.Origin.y;
        return ax < bx + b.Room.Width && bx < ax + a.Room.Width
            && ay < by + b.Room.Height && by < ay + a.Room.Height;
    }

    static void AddUnique(List<RoomDefinition> into, RoomDefinition[] rooms)
    {
        if (rooms == null)
            return;

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null && !Contains(into, rooms[i]))
                into.Add(rooms[i]);
        }
    }

    static bool Contains(List<RoomDefinition> list, RoomDefinition room)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == room)
                return true;
        }

        return false;
    }

    static void Shuffle(List<RoomDefinition> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            RoomDefinition held = list[i];
            list[i] = list[swap];
            list[swap] = held;
        }
    }

    static void Shuffle(List<OpenSocket> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            OpenSocket held = list[i];
            list[i] = list[swap];
            list[swap] = held;
        }
    }
}
