using UnityEngine;

/// <summary>
/// A held weapon. The swing or the shot is still this object's job.
/// The numbers come from <see cref="AttackResolver"/> when the item has a profile.
/// </summary>
public abstract class Weapon : MonoBehaviour, IWeapon
{
    [SerializeField] WeaponStats stats = new WeaponStats();

    PlayerCombat _combat;
    PlayerVitals _vitals;

    public WeaponStats Stats => stats;

    public abstract string ItemId { get; }
    public abstract Sprite IconSprite { get; }
    public abstract bool IsHeld { get; }
    public virtual bool Wants(string itemId) => itemId == ItemId;
    public abstract void SetHeld(bool held);
    public abstract void Attack();

    protected void BindOwner()
    {
        Transform host = transform;
        if (host.GetComponent<PlayerVitals>() == null && host.parent != null)
            host = host.parent;

        _combat = host.GetComponent<PlayerCombat>();
        _vitals = host.GetComponent<PlayerVitals>();
    }

    // Seconds between attacks after the character's attack speed multiplier.
    protected float AttackInterval()
    {
        float perSecond = stats.AttackSpeed;
        if (TryResolvePlan(out AttackPlan plan))
            perSecond = plan.AttackSpeed;

        return ScaledInterval(perSecond);
    }

    protected float ScaledInterval(float attacksPerSecond)
    {
        float perSecond = attacksPerSecond * AttackScale();
        return 1f / Mathf.Max(0.05f, perSecond);
    }

    protected float RollDamage()
    {
        float amount = stats.Damage;
        if (stats.CritChance > 0f && Random.value < stats.CritChance)
            amount *= stats.CritMultiplier;

        return amount;
    }

    protected bool TrySpendStamina()
    {
        float cost = stats.StaminaCost;
        WeaponProfile profile = BoundWeapon();
        if (profile != null)
            cost = Mathf.Max(0f, profile.Stamina);

        if (cost <= 0f || _vitals == null)
            return true;

        return _vitals.TrySpendStaminaAmount(cost);
    }

    protected bool HasMana(float cost)
    {
        if (cost <= 0f || _vitals == null)
            return true;

        return _vitals.Mana + 0.0001f >= cost;
    }

    protected bool TrySpendMana(float cost)
    {
        if (cost <= 0f || _vitals == null)
            return true;

        return _vitals.TrySpendMana(cost);
    }

    float AttackScale()
    {
        if (_combat == null)
            return 1f;

        return Mathf.Max(0.05f, _combat.AttackSpeedMultiplier);
    }

    protected ItemInstance HeldStack()
    {
        PlayerInventory bag = GetComponent<PlayerInventory>();
        if (bag == null && transform.parent != null)
            bag = transform.parent.GetComponent<PlayerInventory>();
        if (bag == null)
            return null;

        return bag.SelectedInstance;
    }

    protected WeaponProfile BoundWeapon()
    {
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        return definition != null ? definition.ResolvedWeapon : null;
    }

    public bool TryPreview(out AttackPlan plan)
    {
        return TryResolvePlan(out plan);
    }

    public float CurrentAttackInterval()
    {
        return AttackInterval();
    }

    protected bool TryResolvePlan(out AttackPlan plan)
    {
        plan = default;
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        if (definition == null || definition.ResolvedWeapon == null)
            return false;

        plan = AttackResolver.Resolve(definition, HeldStack(), AttackResolver.ActiveRunModifiers());
        return true;
    }

    protected bool TryPrepareAttack(float roll01, out AttackPlan plan, out CombatStrike strike)
    {
        strike = null;
        if (!TryResolvePlan(out plan))
            return false;

        float scale = 1f;
        StatusHost host = GetComponentInParent<StatusHost>();
        if (host != null)
            scale = host.OutgoingPhysicalMultiplier;

        strike = CombatStrike.Build(plan, roll01, scale);
        strike.Owner = this;
        return true;
    }

    protected bool TryResolveAttack(float roll01, out AttackPlan plan, out float damage)
    {
        if (!TryPrepareAttack(roll01, out plan, out CombatStrike strike))
        {
            damage = 0f;
            return false;
        }

        damage = strike.Total;
        return true;
    }
}
