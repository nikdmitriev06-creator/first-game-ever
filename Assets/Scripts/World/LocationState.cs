using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session state for one run: placed furniture and ids of bodies that have already broken.
/// Definitions stay in assets. This object is replaced by a save file later, not grown into one now.
/// </summary>
public sealed class LocationState
{
    public struct Record
    {
        public string Id;
        public string ItemId;
        public Vector2 Position;
        public int Health;
    }

    readonly List<Record> _indoor = new List<Record>();
    readonly HashSet<string> _live = new HashSet<string>();
    readonly HashSet<string> _removed = new HashSet<string>();

    public static LocationState Active { get; private set; }

    public int IndoorCount => _indoor.Count;

    public static void Bind(LocationState state)
    {
        Active = state ?? new LocationState();
    }

    public static void Reset()
    {
        Active = new LocationState();
    }

    public void MarkRemoved(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _removed.Add(id);
    }

    public bool WasRemoved(string id)
    {
        return !string.IsNullOrEmpty(id) && _removed.Contains(id);
    }

    public string Remember(string itemId, Vector2 position, int health)
    {
        string id = Guid.NewGuid().ToString("N");
        _indoor.Add(new Record
        {
            Id = id,
            ItemId = itemId,
            Position = position,
            Health = health
        });
        return id;
    }

    public void Touch(string id, Vector2 position, int health)
    {
        for (int i = 0; i < _indoor.Count; i++)
        {
            if (_indoor[i].Id != id)
                continue;

            Record record = _indoor[i];
            record.Position = position;
            record.Health = health;
            _indoor[i] = record;
            return;
        }
    }

    public void Forget(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        for (int i = _indoor.Count - 1; i >= 0; i--)
        {
            if (_indoor[i].Id == id)
                _indoor.RemoveAt(i);
        }

        _live.Remove(id);
        _removed.Add(id);
    }

    public void MarkLive(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _live.Add(id);
    }

    public void MarkGone(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _live.Remove(id);
    }

    public void Restore(Transform parent)
    {
        for (int i = 0; i < _indoor.Count; i++)
        {
            Record record = _indoor[i];
            if (string.IsNullOrEmpty(record.Id) || _live.Contains(record.Id))
                continue;

            ItemDefinition definition = ItemCatalog.Find(record.ItemId);
            if (definition == null)
                continue;

            PlaceableObject.Create(definition, record.Position, parent, true, record.Id, record.Health);
        }
    }
}
