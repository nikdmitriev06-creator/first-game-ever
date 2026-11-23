using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocationLevelRecord
{
    public string id;
    public int level;
}

/// <summary>
/// Persistent location levels only. A run does not write here by itself.
/// </summary>
public sealed class GameProgress
{
    public List<LocationLevelRecord> Locations = new List<LocationLevelRecord>();

    public int Get(string locationId)
    {
        LocationLevelRecord record = Find(locationId);
        return record == null ? 1 : Mathf.Max(1, record.level);
    }

    public void Set(string locationId, int level)
    {
        if (string.IsNullOrEmpty(locationId))
            return;

        LocationLevelRecord record = Find(locationId);
        if (record == null)
        {
            Locations.Add(new LocationLevelRecord { id = locationId, level = Mathf.Max(1, level) });
            return;
        }

        record.level = Mathf.Max(1, level);
    }

    LocationLevelRecord Find(string locationId)
    {
        for (int i = 0; i < Locations.Count; i++)
        {
            LocationLevelRecord record = Locations[i];
            if (record != null && record.id == locationId)
                return record;
        }

        return null;
    }
}

/// <summary>
/// State of the current visit. Discarded with the play session. Never saved as location progress.
/// </summary>
public sealed class RunSession
{
    public int Seed = 1;
    public string LocationId;
    public int? DifficultyOverride;
    public string ActiveRoomId;
    public readonly List<string> TemporaryModifiers = new List<string>();

    public bool AddTemporaryModifier(string id)
    {
        if (string.IsNullOrEmpty(id) || TemporaryModifiers.Contains(id))
            return false;

        TemporaryModifiers.Add(id);
        return true;
    }

    public void ClearTemporaryModifiers()
    {
        TemporaryModifiers.Clear();
    }
}
