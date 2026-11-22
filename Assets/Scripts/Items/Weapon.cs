using UnityEngine;

/// <summary>
/// A held weapon. The combat numbers live on Stats. Each weapon plays its own attack and only changes those numbers.
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
    public abstract void SetHeld(bool held);
    public abstract void Attack();

    protected void BindOwner()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;

        _combat = parent.GetComponent<PlayerCombat>();
        _vitals = parent.GetComponent<PlayerVitals>();
    }

    // Seconds between attacks after the character's attack speed multiplier.
    protected float AttackInterval()
    {
        float perSecond = stats.AttackSpeed;
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        if (definition != null && definition.Weapon != null)
            perSecond = definition.Weapon.AttackSpeed;

        perSecond *= AttackScale();
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
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        if (definition != null && definition.Weapon != null)
            cost = Mathf.Max(0f, definition.Weapon.Stamina);

        if (cost <= 0f || _vitals == null)
            return true;

        return _vitals.TrySpendStaminaAmount(cost);
    }

    float AttackScale()
    {
        if (_combat == null)
            return 1f;

        return Mathf.Max(0.05f, _combat.AttackSpeedMultiplier);
    }

    protected ItemInstance HeldStack()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return null;

        PlayerInventory bag = parent.GetComponent<PlayerInventory>();
        if (bag == null)
            return null;

        return bag.SelectedInstance;
    }

    protected WeaponProfile BoundWeapon()
    {
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        return definition != null ? definition.Weapon : null;
    }

    protected bool TryResolveAttack(float roll01, out AttackPlan plan, out float damage)
    {
        plan = default;
        damage = 0f;
        ItemDefinition definition = ItemCatalog.Find(ItemId);
        if (definition == null || definition.Weapon == null)
            return false;

        plan = AttackResolver.Resolve(definition, HeldStack(), null);
        damage = plan.RollDamage(roll01);
        return true;
    }
}
