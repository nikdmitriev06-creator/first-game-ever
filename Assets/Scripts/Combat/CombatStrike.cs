using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One landed blow after crit and the attacker's weakness, before the target's resistances.
/// Weapons build it. <see cref="Enemy"/> is the only place that turns it into health loss.
/// </summary>
public sealed class CombatStrike
{
    public readonly List<DamageComponent> Parts = new List<DamageComponent>(4);
    public readonly List<StatusSpec> Statuses = new List<StatusSpec>(2);
    public float LifeOnHit;
    public float Knockback;
    public bool Crit;
    public Object Owner;

    public float Total
    {
        get
        {
            float sum = 0f;
            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Amount > 0f)
                    sum += Parts[i].Amount;
            }

            return sum;
        }
    }

    public static CombatStrike Flat(DamageType type, float amount, Object owner)
    {
        var strike = new CombatStrike();
        strike.Owner = owner;
        if (amount > 0f && !float.IsNaN(amount))
        {
            DamageType kind = type == DamageType.None ? DamageType.Slashing : type;
            strike.Parts.Add(new DamageComponent(kind, amount));
        }

        return strike;
    }

    public static CombatStrike Build(AttackPlan plan, float damageRoll, float physicalScale)
    {
        var strike = new CombatStrike();
        bool crit = plan.CritChance > 0f && damageRoll < plan.CritChance;
        float critMul = crit ? Mathf.Max(1f, plan.CritMultiplier) : 1f;
        strike.Crit = crit;
        strike.LifeOnHit = Mathf.Max(0f, plan.LifeOnHit);
        strike.Knockback = Mathf.Max(0f, plan.Knockback);
        if (physicalScale < 0f || float.IsNaN(physicalScale))
            physicalScale = 0f;

        if (plan.Payload != null)
        {
            for (int i = 0; i < plan.Payload.Parts.Count; i++)
                AddScaled(strike, plan.Payload.Parts[i], physicalScale, critMul);

            for (int i = 0; i < plan.Payload.Statuses.Count; i++)
                strike.Statuses.Add(plan.Payload.Statuses[i]);
        }

        if (strike.Parts.Count == 0 && plan.Damage > 0f)
        {
            DamageType type = plan.PrimaryType != DamageType.None ? plan.PrimaryType : DamageType.Slashing;
            AddScaled(strike, new DamageComponent(type, plan.Damage), physicalScale, critMul);
        }

        return strike;
    }

    static void AddScaled(CombatStrike strike, DamageComponent part, float physicalScale, float critMul)
    {
        if (part.Type == DamageType.None)
            return;

        float amount = part.Amount;
        if (amount <= 0f || float.IsNaN(amount))
            return;

        if (DamageTypes.IsPhysical(part.Type))
            amount *= physicalScale;

        amount *= critMul;
        if (amount <= 0f || float.IsNaN(amount))
            return;

        strike.Parts.Add(new DamageComponent(part.Type, amount));
    }
}
