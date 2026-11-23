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
        WeaponProfile profile = definition != null ? definition.ResolvedWeapon : null;
        AttackPlan plan = FromProfile(profile);
        if (!string.IsNullOrEmpty(plan.StrategyId) && AttackStrategies.TryGet(plan.StrategyId, out IAttackStrategy strategy))
            strategy.Adjust(ref plan, instance);

        Apply(ref plan, ModifierLibrary.Collect(instance != null ? instance.ModifierIds : null, ModifierScope.Item));
        Apply(ref plan, ModifierLibrary.Collect(runModifierIds, ModifierScope.Run));
        return plan;
    }

    public static IList<string> ActiveRunModifiers()
    {
        RunSession run = ProgressionService.Run;
        if (run == null || run.TemporaryModifiers == null || run.TemporaryModifiers.Count == 0)
            return null;

        return run.TemporaryModifiers;
    }

    static AttackPlan FromProfile(WeaponProfile profile)
    {
        if (profile == null)
        {
            return new AttackPlan
            {
                Kind = AttackKind.Melee,
                BehaviourId = AttackKind.Melee.ToString(),
                DamageTypeId = "slashing",
                PrimaryType = DamageType.Slashing,
                CritMultiplier = 1.4f,
                Payload = new AttackPayload()
            };
        }

        bool shoots = profile.Kind == AttackKind.Projectile || profile.Kind == AttackKind.Magic;
        DamageType primary = DamageTypes.Resolve(profile.DamageTypeId, profile.Kind, profile.TypedDamage);
        var payload = new AttackPayload();
        if (profile.Damage > 0f)
            payload.AddDamage(primary, profile.Damage);

        if (profile.BonusDamage != null)
        {
            for (int i = 0; i < profile.BonusDamage.Length; i++)
                payload.AddDamage(profile.BonusDamage[i].Type, profile.BonusDamage[i].Amount);
        }

        if (profile.OnHit != null)
        {
            for (int i = 0; i < profile.OnHit.Length; i++)
                payload.AddStatus(profile.OnHit[i]);
        }

        WeaponHitbox hitbox = profile.Hitbox;
        return new AttackPlan
        {
            Kind = profile.Kind,
            StrategyId = profile.StrategyId,
            BehaviourId = string.IsNullOrEmpty(profile.StrategyId) ? profile.Kind.ToString() : profile.StrategyId,
            Damage = payload.Sum(),
            AttackSpeed = Mathf.Max(0.05f, profile.AttackSpeed),
            Range = Mathf.Max(0f, profile.Range),
            Stamina = Mathf.Max(0f, profile.Stamina),
            CritChance = Mathf.Clamp01(profile.CritChance),
            CritMultiplier = Mathf.Max(1f, profile.CritMultiplier),
            ProjectileSpeed = Mathf.Max(0f, profile.ProjectileSpeed),
            DamageTypeId = DamageTypes.ToId(primary),
            PrimaryType = primary,
            ProjectileCount = shoots ? 1 : 0,
            ArcDegrees = Mathf.Max(0f, profile.ArcDegrees),
            Knockback = Mathf.Max(0f, profile.Knockback),
            ManaCost = Mathf.Max(0f, profile.ManaCost),
            Reload = Mathf.Max(0f, profile.Reload),
            Windup = Mathf.Clamp01(profile.Windup),
            HitHalfLength = hitbox != null ? Mathf.Max(0f, hitbox.HalfLength) : 0f,
            HitHalfWidth = hitbox != null ? Mathf.Max(0f, hitbox.HalfWidth) : 0f,
            HitAlong = hitbox != null ? hitbox.Along : 0f,
            HitSide = hitbox != null ? hitbox.Side : 0f,
            Payload = payload
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
                    PayloadOf(ref plan).AddDamage(PrimaryOr(plan), modifier.Amount);
                    break;
                case ModifierStat.DamageMultiplier:
                    PayloadOf(ref plan).MultiplyAll(modifier.Amount);
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
                    ApplyTypeOverride(ref plan, modifier.TextId);
                    break;
                case ModifierStat.OnHitStatus:
                    ApplyOnHit(ref plan, modifier);
                    break;
                case ModifierStat.LifeOnHit:
                    plan.LifeOnHit += modifier.Amount;
                    break;
                case ModifierStat.PhysicalDamageAdd:
                    PayloadOf(ref plan).AddPhysical(modifier.Amount, plan.PrimaryType);
                    break;
                case ModifierStat.ElementalDamageAdd:
                    ApplyElemental(ref plan, modifier);
                    break;
                case ModifierStat.StatusChanceAdd:
                    ApplyStatusChance(ref plan, modifier);
                    break;
                case ModifierStat.StatusPowerAdd:
                    ApplyStatusPower(ref plan, modifier);
                    break;
            }
        }

        plan.Damage = plan.Payload != null ? Mathf.Max(0f, plan.Payload.Sum()) : Mathf.Max(0f, plan.Damage);
        plan.AttackSpeed = Mathf.Max(0.05f, plan.AttackSpeed);
        plan.Range = Mathf.Max(0f, plan.Range);
        plan.LifeOnHit = Mathf.Max(0f, plan.LifeOnHit);
        if (plan.ProjectileCount < 0)
            plan.ProjectileCount = 0;
        if (plan.Payload != null)
            plan.Payload.ClampStatus();
    }

    static AttackPayload PayloadOf(ref AttackPlan plan)
    {
        if (plan.Payload == null)
            plan.Payload = new AttackPayload();

        return plan.Payload;
    }

    static DamageType PrimaryOr(AttackPlan plan)
    {
        if (plan.PrimaryType != DamageType.None)
            return plan.PrimaryType;

        if (plan.Payload != null && plan.Payload.Parts.Count > 0)
            return plan.Payload.Parts[0].Type;

        return DamageType.Slashing;
    }

    static void ApplyTypeOverride(ref AttackPlan plan, string textId)
    {
        if (!DamageTypes.TryParse(textId, out DamageType type))
            return;

        plan.PrimaryType = type;
        plan.DamageTypeId = DamageTypes.ToId(type);
        if (plan.Payload != null)
            plan.Payload.SetPrimaryType(type);
    }

    static void ApplyElemental(ref AttackPlan plan, ModifierDefinition modifier)
    {
        DamageType hinted = DamageType.None;
        if (DamageTypes.TryParse(modifier.TextId, out DamageType parsed) && DamageTypes.IsElemental(parsed))
            hinted = parsed;

        PayloadOf(ref plan).AddElemental(modifier.Amount, hinted);
    }

    static void ApplyOnHit(ref AttackPlan plan, ModifierDefinition modifier)
    {
        if (!StatusKinds.TryParse(modifier.TextId, out StatusKind kind))
            return;

        float chance = modifier.Amount > 0f ? modifier.Amount : 1f;
        PayloadOf(ref plan).AddStatus(StatusSpec.Default(kind, chance));
        plan.StatusEffectId = StatusKinds.ToId(kind);
    }

    static void ApplyStatusChance(ref AttackPlan plan, ModifierDefinition modifier)
    {
        AttackPayload payload = PayloadOf(ref plan);
        if (StatusKinds.TryParse(modifier.TextId, out StatusKind kind))
        {
            if (payload.IndexOfStatus(kind) < 0)
                payload.AddStatus(StatusSpec.Default(kind, 0f));

            payload.AddStatusChance(kind, modifier.Amount);
            plan.StatusEffectId = StatusKinds.ToId(kind);
            return;
        }

        payload.AddStatusChanceAll(modifier.Amount);
    }

    static void ApplyStatusPower(ref AttackPlan plan, ModifierDefinition modifier)
    {
        AttackPayload payload = PayloadOf(ref plan);
        if (!StatusKinds.TryParse(modifier.TextId, out StatusKind kind))
        {
            payload.AddStatusPowerAll(modifier.Amount);
            return;
        }

        if (payload.IndexOfStatus(kind) < 0)
        {
            StatusSpec created = StatusSpec.Default(kind, 1f);
            created.Power = Mathf.Max(0f, modifier.Amount);
            payload.AddStatus(created);
        }
        else
        {
            payload.AddStatusPower(kind, modifier.Amount);
        }

        plan.StatusEffectId = StatusKinds.ToId(kind);
    }
}
