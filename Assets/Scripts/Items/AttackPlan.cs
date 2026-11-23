using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pieces of one attack after the profile and its modifiers. Built fresh each swing.
/// Nothing here is written back to the item.
/// </summary>
public sealed class AttackPayload
{
    public readonly List<DamageComponent> Parts = new List<DamageComponent>(4);
    public readonly List<StatusSpec> Statuses = new List<StatusSpec>(2);

    public float Sum()
    {
        float sum = 0f;
        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i].Amount > 0f)
                sum += Parts[i].Amount;
        }

        return sum;
    }

    public void AddDamage(DamageType type, float amount)
    {
        if (type == DamageType.None || amount == 0f || float.IsNaN(amount))
            return;

        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i].Type != type)
                continue;

            DamageComponent part = Parts[i];
            part.Amount = Mathf.Max(0f, part.Amount + amount);
            if (part.Amount <= 0f)
                Parts.RemoveAt(i);
            else
                Parts[i] = part;
            return;
        }

        if (amount > 0f)
            Parts.Add(new DamageComponent(type, amount));
    }

    public void AddPhysical(float amount, DamageType fallback)
    {
        if (amount == 0f || float.IsNaN(amount))
            return;

        for (int i = 0; i < Parts.Count; i++)
        {
            if (!DamageTypes.IsPhysical(Parts[i].Type))
                continue;

            AddDamage(Parts[i].Type, amount);
            return;
        }

        DamageType type = DamageTypes.IsPhysical(fallback) ? fallback : DamageType.Slashing;
        AddDamage(type, amount);
    }

    public void AddElemental(float amount, DamageType hinted)
    {
        if (amount == 0f || float.IsNaN(amount))
            return;

        if (DamageTypes.IsElemental(hinted))
        {
            AddDamage(hinted, amount);
            return;
        }

        for (int i = 0; i < Parts.Count; i++)
        {
            if (!DamageTypes.IsElemental(Parts[i].Type))
                continue;

            AddDamage(Parts[i].Type, amount);
            return;
        }
    }

    public void MultiplyAll(float factor)
    {
        if (float.IsNaN(factor) || factor < 0f)
            factor = 0f;

        for (int i = Parts.Count - 1; i >= 0; i--)
        {
            DamageComponent part = Parts[i];
            part.Amount = Mathf.Max(0f, part.Amount * factor);
            if (part.Amount <= 0f)
                Parts.RemoveAt(i);
            else
                Parts[i] = part;
        }
    }

    public void SetPrimaryType(DamageType type)
    {
        if (type == DamageType.None || Parts.Count == 0)
            return;

        float amount = Parts[0].Amount;
        Parts.RemoveAt(0);
        AddDamage(type, amount);
        for (int i = 0; i < Parts.Count; i++)
        {
            if (Parts[i].Type != type || i == 0)
                continue;

            DamageComponent part = Parts[i];
            Parts.RemoveAt(i);
            Parts.Insert(0, part);
            return;
        }
    }

    public int IndexOfStatus(StatusKind kind)
    {
        for (int i = 0; i < Statuses.Count; i++)
        {
            if (Statuses[i].Kind == kind)
                return i;
        }

        return -1;
    }

    public void AddStatus(StatusSpec spec)
    {
        if (spec.Kind == StatusKind.None)
            return;

        spec.Chance = Mathf.Clamp01(spec.Chance);
        spec.Power = Mathf.Max(0f, spec.Power);
        spec.Duration = Mathf.Max(0f, spec.Duration);
        spec.Interval = Mathf.Max(0f, spec.Interval);

        int index = IndexOfStatus(spec.Kind);
        if (index < 0)
        {
            Statuses.Add(spec);
            return;
        }

        StatusSpec current = Statuses[index];
        current.Chance = Mathf.Clamp01(Mathf.Max(current.Chance, spec.Chance));
        current.Power = Mathf.Max(current.Power, spec.Power);
        if (spec.Duration > 0f)
            current.Duration = spec.Duration;
        if (spec.Interval > 0f)
            current.Interval = spec.Interval;
        Statuses[index] = current;
    }

    public void AddStatusChance(StatusKind kind, float amount)
    {
        int index = IndexOfStatus(kind);
        if (index < 0 || float.IsNaN(amount))
            return;

        StatusSpec spec = Statuses[index];
        spec.Chance = Mathf.Clamp01(spec.Chance + amount);
        Statuses[index] = spec;
    }

    public void AddStatusChanceAll(float amount)
    {
        if (float.IsNaN(amount))
            return;

        for (int i = 0; i < Statuses.Count; i++)
        {
            StatusSpec spec = Statuses[i];
            spec.Chance = Mathf.Clamp01(spec.Chance + amount);
            Statuses[i] = spec;
        }
    }

    public void AddStatusPower(StatusKind kind, float amount)
    {
        int index = IndexOfStatus(kind);
        if (index < 0 || float.IsNaN(amount))
            return;

        StatusSpec spec = Statuses[index];
        spec.Power = Mathf.Max(0f, spec.Power + amount);
        Statuses[index] = spec;
    }

    public void AddStatusPowerAll(float amount)
    {
        if (float.IsNaN(amount))
            return;

        for (int i = 0; i < Statuses.Count; i++)
        {
            StatusSpec spec = Statuses[i];
            spec.Power = Mathf.Max(0f, spec.Power + amount);
            Statuses[i] = spec;
        }
    }

    public void ClampStatus()
    {
        for (int i = Statuses.Count - 1; i >= 0; i--)
        {
            StatusSpec spec = Statuses[i];
            if (spec.Kind == StatusKind.None)
            {
                Statuses.RemoveAt(i);
                continue;
            }

            spec.Chance = Mathf.Clamp01(spec.Chance);
            spec.Power = Mathf.Max(0f, spec.Power);
            spec.Duration = Mathf.Max(0f, spec.Duration);
            spec.Interval = Mathf.Max(0f, spec.Interval);
            Statuses[i] = spec;
        }
    }
}

/// <summary>
/// Numbers for one swing or shot after the weapon profile and its modifiers.
/// Built fresh each attack. Nothing here is written back to the item asset.
/// </summary>
public struct AttackPlan
{
    public AttackKind Kind;
    public string StrategyId;
    public string BehaviourId;
    public float Damage;
    public float AttackSpeed;
    public float Range;
    public float Stamina;
    public float CritChance;
    public float CritMultiplier;
    public float ProjectileSpeed;
    public string DamageTypeId;
    public DamageType PrimaryType;
    public int ProjectileCount;
    public string StatusEffectId;
    public float LifeOnHit;
    public float ArcDegrees;
    public float Knockback;
    public float ManaCost;
    public float Reload;
    public float Windup;
    public float HitHalfLength;
    public float HitHalfWidth;
    public float HitAlong;
    public float HitSide;
    public AttackPayload Payload;

    public float RollDamage(float roll01)
    {
        float amount = Payload != null && Payload.Parts.Count > 0 ? Payload.Sum() : Mathf.Max(0f, Damage);
        if (CritChance > 0f && roll01 < CritChance)
            amount *= Mathf.Max(1f, CritMultiplier);

        return amount < 0f ? 0f : amount;
    }
}
