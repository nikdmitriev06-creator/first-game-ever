using UnityEngine;

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
    public int ProjectileCount;
    public string StatusEffectId;
    public float LifeOnHit;

    public float RollDamage(float roll01)
    {
        float amount = Mathf.Max(0f, Damage);
        if (CritChance > 0f && roll01 < CritChance)
            amount *= Mathf.Max(1f, CritMultiplier);

        return amount;
    }
}
