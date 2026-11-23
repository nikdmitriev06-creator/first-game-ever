using System.Collections.Generic;
using UnityEngine;

public enum RoomTool
{
    Paint,
    Erase,
    Spawn,
    Object,
    Socket,
    Select
}

/// <summary>
/// Shared brush for the level window and the scene view. Both edit the same room asset.
/// </summary>
public static class RoomEditSession
{
    public static RoomTool Tool = RoomTool.Paint;
    public static RoomLayer Layer = RoomLayer.Ground;
    public static TerrainDefinition Terrain;
    public static SpawnPoolDefinition Pool;
    public static SpawnCategory Category = SpawnCategory.Monster;
    public static GameObject Prefab;
    public static IsoDirection Direction = IsoDirection.NorthEast;
    public static bool SocketRequired = true;
    public static string SocketTag = "path";
    public static int Difficulty = 1;
    public static int Seed = 1;
    public static int MinCount = 1;
    public static int MaxCount = 2;
    public static float Radius = 1.5f;
    public static readonly List<string> Selected = new List<string>();

    public static bool TileLayer => Layer == RoomLayer.Ground || Layer == RoomLayer.Details || Layer == RoomLayer.Terrain;

    public static void SelectOnly(string id)
    {
        Selected.Clear();
        if (!string.IsNullOrEmpty(id))
            Selected.Add(id);
    }

    public static void Toggle(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        if (Selected.Contains(id))
            Selected.Remove(id);
        else
            Selected.Add(id);
    }
}
