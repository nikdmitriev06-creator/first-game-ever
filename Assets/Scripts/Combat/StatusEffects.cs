using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusKind
{
    None = 0,
    Bleed = 1,
    Weakness = 2
}

/// <summary>
/// A status an attack may apply. Damage kinds do not live here.
/// Reapply refreshes the duration and keeps the stronger power. It does not add a second copy.
/// </summary>
[Serializable]
public struct StatusSpec
{
    public StatusKind Kind;
    public float Chance;
    public float Duration;
    public float Power;
    public float Interval;

    public static StatusSpec Bleed(float chance, float power, float duration, float interval)
    {
        return new StatusSpec
        {
            Kind = StatusKind.Bleed,
            Chance = Mathf.Clamp01(chance),
            Power = Mathf.Max(0f, power),
            Duration = Mathf.Max(0f, duration),
            Interval = Mathf.Max(0.05f, interval)
        };
    }

    public static StatusSpec Weakness(float chance, float power, float duration)
    {
        return new StatusSpec
        {
            Kind = StatusKind.Weakness,
            Chance = Mathf.Clamp01(chance),
            Power = Mathf.Max(0f, power),
            Duration = Mathf.Max(0f, duration),
            Interval = 0f
        };
    }

    public static StatusSpec Default(StatusKind kind, float chance)
    {
        if (kind == StatusKind.Bleed)
            return Bleed(chance, 3f, 4f, 0.5f);

        if (kind == StatusKind.Weakness)
            return Weakness(chance, 0.25f, 5f);

        return default;
    }
}

public static class StatusKinds
{
    public static bool TryParse(string id, out StatusKind kind)
    {
        kind = StatusKind.None;
        if (string.IsNullOrEmpty(id))
            return false;

        switch (id.Trim().ToLowerInvariant())
        {
            case "bleed":
            case "bleeding":
                kind = StatusKind.Bleed;
                return true;
            case "weak":
            case "weakness":
                kind = StatusKind.Weakness;
                return true;
            default:
                return false;
        }
    }

    public static string ToId(StatusKind kind)
    {
        if (kind == StatusKind.Bleed)
            return "bleed";
        if (kind == StatusKind.Weakness)
            return "weakness";
        return string.Empty;
    }
}

/// <summary>
/// Active statuses on one body. Base stats are never written. Weakness is read as a multiplier.
/// </summary>
public sealed class StatusState
{
    public struct Active
    {
        public StatusKind Kind;
        public float Remaining;
        public float Power;
        public float Interval;
        public float Accumulator;
    }

    readonly List<Active> _active = new List<Active>(4);
    readonly List<StatusKind> _immune = new List<StatusKind>(2);

    public int Count => _active.Count;

    public float OutgoingPhysicalMultiplier
    {
        get
        {
            float reduction = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Kind == StatusKind.Weakness)
                    reduction += Mathf.Max(0f, _active[i].Power);
            }

            return Mathf.Max(0f, 1f - reduction);
        }
    }

    public void SetImmune(StatusKind kind, bool immune)
    {
        if (kind == StatusKind.None)
            return;

        bool has = IsImmune(kind);
        if (immune && !has)
            _immune.Add(kind);
        else if (!immune && has)
            _immune.Remove(kind);
    }

    public bool IsImmune(StatusKind kind)
    {
        for (int i = 0; i < _immune.Count; i++)
        {
            if (_immune[i] == kind)
                return true;
        }

        return false;
    }

    public void Clear()
    {
        _active.Clear();
    }

    public bool TryGet(StatusKind kind, out float remaining, out float power)
    {
        remaining = 0f;
        power = 0f;
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Kind != kind)
                continue;

            remaining = _active[i].Remaining;
            power = _active[i].Power;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Roll is 0..1. The status lands when the roll is below the chance.
    /// A second copy refreshes the clock and keeps whichever power is higher.
    /// </summary>
    public bool TryApply(in StatusSpec spec, float roll01)
    {
        if (spec.Kind == StatusKind.None || spec.Duration <= 0f || spec.Chance <= 0f)
            return false;

        if (roll01 >= spec.Chance)
            return false;

        if (IsImmune(spec.Kind))
            return false;

        float power = Mathf.Max(0f, spec.Power);
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Kind != spec.Kind)
                continue;

            Active current = _active[i];
            current.Remaining = spec.Duration;
            current.Power = Mathf.Max(current.Power, power);
            if (spec.Interval > 0f)
                current.Interval = spec.Interval;
            _active[i] = current;
            return true;
        }

        _active.Add(new Active
        {
            Kind = spec.Kind,
            Remaining = spec.Duration,
            Power = power,
            Interval = Mathf.Max(0f, spec.Interval),
            Accumulator = 0f
        });
        return true;
    }

    /// <summary>
    /// Advances every effect. Returns bleed damage dealt this step. Expired effects drop off.
    /// </summary>
    public float Tick(float deltaTime)
    {
        if (deltaTime <= 0f || _active.Count == 0)
            return 0f;

        float bleed = 0f;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Active active = _active[i];
            float alive = deltaTime;
            if (active.Remaining < alive)
                alive = Mathf.Max(0f, active.Remaining);

            active.Remaining -= deltaTime;
            if (active.Kind == StatusKind.Bleed && active.Power > 0f && alive > 0f)
            {
                float interval = Mathf.Max(0.05f, active.Interval);
                active.Accumulator += alive;
                int ticks = Mathf.FloorToInt(active.Accumulator / interval);
                if (ticks > 0)
                {
                    active.Accumulator -= ticks * interval;
                    bleed += ticks * active.Power;
                }
            }

            if (active.Remaining <= 0f)
                _active.RemoveAt(i);
            else
                _active[i] = active;
        }

        return bleed < 0f ? 0f : bleed;
    }
}
