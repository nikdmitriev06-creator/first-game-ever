using UnityEngine;

public enum ModifierStat
{
    DamageAdd,
    DamageMultiplier,
    AttackSpeedAdd,
    RangeAdd,
    ExtraProjectiles,
    DamageTypeOverride,
    OnHitStatus,
    LifeOnHit,
    PhysicalDamageAdd,
    ElementalDamageAdd,
    StatusChanceAdd,
    StatusPowerAdd
}

public enum ModifierScope
{
    Item,
    Run
}

/// <summary>
/// One rune or one temporary run buff. The same asset can sit on many weapons.
/// Scope decides whether it is saved on an item or forgotten when the run ends.
/// </summary>
[CreateAssetMenu(fileName = "Modifier", menuName = "Game/Modifier")]
public class ModifierDefinition : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] ModifierScope scope = ModifierScope.Item;
    [SerializeField] ModifierStat stat;
    [SerializeField] float amount;
    [SerializeField] string textId;

    public string Id => id;
    public ModifierScope Scope => scope;
    public ModifierStat Stat => stat;
    public float Amount => amount;
    public string TextId => textId;

    public void Configure(string modifierId, ModifierScope modifierScope, ModifierStat modifierStat, float value, string text)
    {
        id = modifierId;
        scope = modifierScope;
        stat = modifierStat;
        amount = value;
        textId = text;
    }
}
