using System.Collections.Generic;
using UnityEngine;

public struct SpawnOutcome
{
    public int Requested;
    public int Spawned;
    public string Note;
}

/// <summary>
/// Turns a resolved entry into an existing prefab. It does not know slime, rock, or tree classes.
/// </summary>
public static class SpawnDirector
{
    public static SpawnOutcome Spawn(
        RoomMarkerData marker,
        int difficulty,
        int seed,
        TerrainField field,
        Transform parent,
        PlayArea area,
        bool force)
    {
        var outcome = new SpawnOutcome();
        if (marker == null || marker.kind != MarkerKind.Spawn)
        {
            outcome.Note = "Это не точка спавна.";
            return outcome;
        }

        if (!force && marker.repeat == SpawnRepeat.ManualOnly)
        {
            outcome.Note = "Точка ждёт ручного спавна.";
            return outcome;
        }

        if (!SpawnResolver.PointAllows(marker.enabled, difficulty, marker.minDifficulty, marker.maxDifficulty))
        {
            outcome.Note = "Точка выключена на сложности " + difficulty + ".";
            return outcome;
        }

        List<SpawnEntry> available = SpawnResolver.Available(marker.pool, difficulty);
        if (available.Count == 0)
        {
            outcome.Note = "Нет доступных вариантов.";
            return outcome;
        }

        var roll = new SpawnRoll(seed);
        int count = roll.Count(marker.minCount, marker.maxCount);
        outcome.Requested = count;
        if (count <= 0)
        {
            outcome.Note = "Количество 0.";
            return outcome;
        }

        var spots = new List<Vector2>();
        Vector2 anchor = TerrainCoordinates.CellCenterWorld(marker.x, marker.y);
        if (field != null)
            field.Collect(anchor, marker.radius, marker.allowedSurfaceIds, true, spots);

        if (spots.Count == 0)
        {
            outcome.Note = "Нет подходящей поверхности в радиусе.";
            return outcome;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnEntry entry = roll.Pick(available);
            if (entry == null || entry.prefab == null)
                continue;

            Vector2 spot = spots[roll.Index(spots.Count)];
            SpawnPrefab(entry.prefab, spot, parent, area);
            outcome.Spawned++;
        }

        if (outcome.Spawned == 0)
            outcome.Note = "Варианты нашлись, но у них нет prefab.";

        return outcome;
    }

    public static GameObject SpawnPrefab(GameObject prefab, Vector2 world, Transform parent, PlayArea area)
    {
        if (prefab == null)
            return null;

        GameObject body = Object.Instantiate(prefab, parent);
        IWorldSpawn placed = body.GetComponent<IWorldSpawn>();
        if (placed != null)
            placed.PlaceInWorld(world, area, parent);
        else
        {
            if (parent != null)
                body.transform.SetParent(parent, false);
            body.transform.position = new Vector3(world.x, world.y, 0f);
        }

        BillboardSort sort = body.GetComponent<BillboardSort>();
        if (sort != null)
            sort.LineY = world.y;

        return body;
    }
}
