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
}
