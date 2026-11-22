using System;

/// <summary>
/// Worn gear that is not a weapon. Bonuses stay data. The player does not read them yet.
/// </summary>
[Serializable]
public class EquipmentProfile
{
    public string SlotId = "body";
    public float HealthBonus;
}
