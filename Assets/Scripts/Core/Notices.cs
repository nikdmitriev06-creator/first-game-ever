using System;
using System.Collections.Generic;

public enum NoticeKind
{
    Item,
    Crafted,
    Lack,
    Recipe,
    Rare,
    Chest,
    Location,
    Mechanism,
    Impossible,
    Broken
}

/// <summary>
/// One queued message. The same kind and subject add to the amount instead of stacking copies.
/// </summary>
public sealed class NoticeEntry
{
    public NoticeKind Kind;
    public string Subject;
    public int Amount;
    public int Priority;
    public float TimeLeft;
    public string Key;
}

/// <summary>
/// Presentation queue. It does not decide crafting, loot, or inventory rules.
/// Callers post a fact. The HUD is the only thing that turns the fact into a sentence.
/// </summary>
public sealed class NoticeBoard
{
    public int MaxVisible = 4;
    public int MaxQueued = 12;
    public float Duration = 3.4f;
    public float HighDuration = 4.8f;

    readonly List<NoticeEntry> _visible = new List<NoticeEntry>();
    readonly List<NoticeEntry> _queue = new List<NoticeEntry>();

    public int VisibleCount => _visible.Count;
    public int Queued => _queue.Count;

    public NoticeEntry VisibleAt(int index)
    {
        if ((uint)index >= (uint)_visible.Count)
            return null;

        return _visible[index];
    }

    public void Clear()
    {
        _visible.Clear();
        _queue.Clear();
    }

    public void Post(NoticeKind kind, string subject, int amount, int priority)
    {
        if (amount < 0)
            amount = 0;

        string key = kind + "|" + (subject ?? string.Empty);
        NoticeEntry sitting = Find(_visible, key);
        if (sitting == null)
            sitting = Find(_queue, key);

        if (sitting != null)
        {
            sitting.Amount += amount;
            if (priority > sitting.Priority)
                sitting.Priority = priority;
            if (Contains(_visible, sitting))
                sitting.TimeLeft = TimeFor(sitting.Priority);
            return;
        }

        var entry = new NoticeEntry
        {
            Kind = kind,
            Subject = subject ?? string.Empty,
            Amount = amount,
            Priority = priority > 0 ? 1 : 0,
            TimeLeft = TimeFor(priority),
            Key = key
        };

        if (_visible.Count < MaxVisible)
        {
            _visible.Add(entry);
            return;
        }

        if (entry.Priority > 0 && Displace(entry))
            return;

        Enqueue(entry);
    }

    public void Tick(float delta)
    {
        if (delta < 0f)
            delta = 0f;

        for (int i = _visible.Count - 1; i >= 0; i--)
        {
            _visible[i].TimeLeft -= delta;
            if (_visible[i].TimeLeft > 0f)
                continue;

            _visible.RemoveAt(i);
        }

        while (_visible.Count < MaxVisible && _queue.Count > 0)
        {
            NoticeEntry next = _queue[0];
            _queue.RemoveAt(0);
            next.TimeLeft = TimeFor(next.Priority);
            _visible.Add(next);
        }
    }

    bool Displace(NoticeEntry important)
    {
        for (int i = 0; i < _visible.Count; i++)
        {
            if (_visible[i].Priority > 0)
                continue;

            NoticeEntry displaced = _visible[i];
            _visible.RemoveAt(i);
            _visible.Insert(0, important);
            Enqueue(displaced);
            return true;
        }

        return false;
    }

    void Enqueue(NoticeEntry entry)
    {
        if (entry.Priority > 0)
            _queue.Insert(0, entry);
        else
            _queue.Add(entry);

        while (_queue.Count > MaxQueued)
            _queue.RemoveAt(_queue.Count - 1);
    }

    float TimeFor(int priority)
    {
        return priority > 0 ? HighDuration : Duration;
    }

    static NoticeEntry Find(List<NoticeEntry> list, string key)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Key == key)
                return list[i];
        }

        return null;
    }

    static bool Contains(List<NoticeEntry> list, NoticeEntry entry)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == entry)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Shared queue. Gameplay posts facts here the same way it posts a sound: no widget, no sentence.
/// </summary>
public static class Notices
{
    public static readonly NoticeBoard Board = new NoticeBoard();
    public static event Action Changed;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Board.Clear();
        Board.MaxVisible = 4;
        Board.MaxQueued = 12;
        Board.Duration = 3.4f;
        Board.HighDuration = 4.8f;
        Changed = null;
    }

    public static void Post(NoticeKind kind, string subject, int amount, int priority)
    {
        Board.Post(kind, subject, amount, priority);
        Changed?.Invoke();
    }

    public static void Tick(float delta)
    {
        int before = Board.VisibleCount + Board.Queued;
        Board.Tick(delta);
        if (Board.VisibleCount + Board.Queued != before)
            Changed?.Invoke();
    }
}
