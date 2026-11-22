using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns a weapon definition plus item and run modifiers into one attack plan.
/// Item modifiers and run modifiers stay in separate lists.
/// </summary>
public static class AttackResolver
{
    public static AttackPlan Resolve(ItemDefinition definition, ItemInstance instance, IList<string> runModifierIds)
    {
        WeaponProfile profile = definition != null ? definition.Weapon : null;
        AttackPlan plan = FromProfile(profile);
        if (!string.IsNullOrEmpty(plan.StrategyId) && AttackStrategies.TryGet(plan.StrategyId, out IAttackStrategy strategy))
            strategy.Adjust(ref plan, instance);

        Apply(ref plan, ModifierLibrary.Collect(instance != null ? instance.ModifierIds : null, ModifierScope.Item));
        Apply(ref plan, ModifierLibrary.Collect(runModifierIds, ModifierScope.Run));
        return plan;
    }

    static AttackPlan FromProfile(WeaponProfile profile)
    {
        if (profile == null)
        {
            return new AttackPlan
            {
                Kind = AttackKind.Melee,
                BehaviourId = AttackKind.Melee.ToString(),
                DamageTypeId = "physical",
                CritMultiplier = 1.4f
            };
        }

        bool shoots = profile.Kind == AttackKind.Projectile || profile.Kind == AttackKind.Magic;
        return new AttackPlan
        {
            Kind = profile.Kind,
            StrategyId = profile.StrategyId,
            BehaviourId = string.IsNullOrEmpty(profile.StrategyId) ? profile.Kind.ToString() : profile.StrategyId,
            Damage = Mathf.Max(0f, profile.Damage),
            AttackSpeed = Mathf.Max(0.05f, profile.AttackSpeed),
            Range = Mathf.Max(0f, profile.Range),
            Stamina = Mathf.Max(0f, profile.Stamina),
            CritChance = Mathf.Clamp01(profile.CritChance),
            CritMultiplier = Mathf.Max(1f, profile.CritMultiplier),
            ProjectileSpeed = Mathf.Max(0f, profile.ProjectileSpeed),
            DamageTypeId = string.IsNullOrEmpty(profile.DamageTypeId) ? "physical" : profile.DamageTypeId,
            ProjectileCount = shoots ? 1 : 0
        };
    }

    static void Apply(ref AttackPlan plan, List<ModifierDefinition> modifiers)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            ModifierDefinition modifier = modifiers[i];
            if (modifier == null)
                continue;

            switch (modifier.Stat)
            {
                case ModifierStat.DamageAdd:
                    plan.Damage += modifier.Amount;
                    break;
                case ModifierStat.DamageMultiplier:
                    plan.Damage *= modifier.Amount;
                    break;
                case ModifierStat.AttackSpeedAdd:
                    plan.AttackSpeed += modifier.Amount;
                    break;
                case ModifierStat.RangeAdd:
                    plan.Range += modifier.Amount;
                    break;
                case ModifierStat.ExtraProjectiles:
                    plan.ProjectileCount += Mathf.RoundToInt(modifier.Amount);
                    break;
                case ModifierStat.DamageTypeOverride:
                    if (!string.IsNullOrEmpty(modifier.TextId))
                        plan.DamageTypeId = modifier.TextId;
                    break;
                case ModifierStat.OnHitStatus:
                    if (!string.IsNullOrEmpty(modifier.TextId))
                        plan.StatusEffectId = modifier.TextId;
                    break;
                case ModifierStat.LifeOnHit:
                    plan.LifeOnHit += modifier.Amount;
                    break;
            }
        }

        plan.Damage = Mathf.Max(0f, plan.Damage);
        plan.AttackSpeed = Mathf.Max(0.05f, plan.AttackSpeed);
        plan.Range = Mathf.Max(0f, plan.Range);
        if (plan.ProjectileCount < 0)
            plan.ProjectileCount = 0;
    }
}
