using System;
using UnityEngine;

/// <summary>
/// Combat numbers shared by every weapon. A sword or a bow only fills these in.
/// </summary>
[Serializable]
public class WeaponStats
{
    public float damage = 10f;
    // Attacks per second before the character's attack speed multiplier.
    public float attackSpeed = 1.5f;
    // Melee reach, or how far a shot flies.
    public float range = 3f;
    public float staminaCost = 8f;
    // Units per second. Zero means this weapon does not fire a projectile.
    public float projectileSpeed;
    // Fraction from 0 to 1.
    public float critChance;
    // 1.4 adds 40 percent. Crit is a small damage bump until it becomes its own style.
    public float critMultiplier = 1.4f;

    public float Damage => Mathf.Max(0f, damage);
    public float AttackSpeed => Mathf.Max(0.05f, attackSpeed);
    public float Range => Mathf.Max(0f, range);
    public float StaminaCost => Mathf.Max(0f, staminaCost);
    public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
    public float CritChance => Mathf.Clamp01(critChance);
    public float CritMultiplier => Mathf.Max(1f, critMultiplier);
}
