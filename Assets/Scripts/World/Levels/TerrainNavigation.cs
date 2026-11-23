using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Active room fields. Registered while a room is enabled. No per-frame scene search.
/// </summary>
public static class TerrainNavigation
{
    const float Sample = 0.35f;

    static readonly List<TerrainField> Fields = new List<TerrainField>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Fields.Clear();
        MovementGate.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallOnLoad()
    {
        Install();
    }

    public static void Install()
    {
        MovementGate.Bind(BlocksTravel, SpeedScale);
    }

    public static void Register(TerrainField field)
    {
        if (field == null || Fields.Contains(field))
            return;

        Install();
        Fields.Add(field);
    }

    public static void Unregister(TerrainField field)
    {
        if (field == null)
            return;

        Fields.Remove(field);
    }

    public static int FieldCount => Fields.Count;

    public static bool BlocksTravel(Vector2 from, Vector2 to)
    {
        if (Fields.Count == 0)
            return false;

        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Sample));
        Vector2 previous = from;
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
            if (!CanOccupy(previous, point))
                return true;

            previous = point;
        }

        return false;
    }

    public static float SpeedScale(Vector2 world)
    {
        if (!TrySample(world, out TerrainField.Cell cell))
            return 1f;

        return cell.Speed > 0f ? cell.Speed : 1f;
    }

    public static bool TrySample(Vector2 world, out TerrainField.Cell cell)
    {
        Vector2Int index = TerrainCoordinates.WorldToCell(world);
        for (int i = 0; i < Fields.Count; i++)
        {
            TerrainField field = Fields[i];
            if (field != null && field.TryGet(index.x, index.y, out cell))
                return true;
        }

        cell = default;
        return false;
    }

    public static bool CanOccupy(Vector2 from, Vector2 to)
    {
        bool hasTo = TrySample(to, out TerrainField.Cell dest);
        if (!hasTo)
            return true;

        bool hasFrom = TrySample(from, out TerrainField.Cell src);
        return TerrainField.CanStep(src, hasFrom, dest, true);
    }
}
