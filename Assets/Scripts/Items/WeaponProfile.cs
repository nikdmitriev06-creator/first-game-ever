using System;
using UnityEngine;

public enum AttackKind
{
    Melee,
    Sequence,
    Area,
    Projectile,
    Magic
}

/// <summary>
/// Combat numbers on an item asset. A second sword is another asset, not another class.
/// </summary>
[Serializable]
public class WeaponProfile
{
    public AttackKind Kind = AttackKind.Melee;
    public float Damage = 10f;
    public float AttackSpeed = 1.5f;
    public float Range = 3f;
    public float Stamina = 8f;
    public float CritChance;
    public float CritMultiplier = 1.4f;
    public float ProjectileSpeed;
    public string DamageTypeId = "physical";
    public string StrategyId;
    // None keeps DamageTypeId, including the old "physical" value.
    public DamageType TypedDamage;
    public DamageComponent[] BonusDamage;
    public StatusSpec[] OnHit;
    // Full arc of a melee swing, in degrees. A spear is narrow. A glaive is wide.
    public float ArcDegrees = 80f;
    public float Knockback;
    public float ManaCost;
    // Seconds before the next shot when this is slower than the attack interval. Crossbow uses it.
    public float Reload;
    // Fraction of the swing spent before the hit. A heavy sword winds up longer.
    public float Windup;
    // Per-weapon box. Empty world length keeps the standard arc. The painted sword uses the sprite fields.
    public WeaponHitbox Hitbox = new WeaponHitbox();

    /// <summary>
    /// True for a missing profile and for the serializer default (10 damage, melee, no explicit type).
    /// Sword and bow use that placeholder until their real numbers are written.
    /// </summary>
    public static bool IsBlank(WeaponProfile profile)
    {
        if (profile == null)
            return true;

        if (profile.TypedDamage != DamageType.None)
            return false;

        if (!string.IsNullOrEmpty(profile.DamageTypeId) && profile.DamageTypeId != "physical")
            return false;

        return Mathf.Approximately(profile.Damage, 10f)
            && Mathf.Approximately(profile.AttackSpeed, 1.5f)
            && Mathf.Approximately(profile.Range, 3f);
    }
}
