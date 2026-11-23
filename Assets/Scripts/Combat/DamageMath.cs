using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One slice of an attack. A swing can carry several at once. They are not collapsed into one type.
/// </summary>
[Serializable]
public struct DamageComponent
{
    public DamageType Type;
    public float Amount;

    public DamageComponent(DamageType type, float amount)
    {
        Type = type;
        Amount = amount;
    }
}

/// <summary>
/// How one creature treats one damage kind.
/// 0.25 blocks a quarter. -0.5 takes half again as much. 1 or more blocks that kind.
/// Other kinds are unchanged. A missing entry is full damage.
/// </summary>
[Serializable]
public struct DamageResistance
{
    public DamageType Type;
    public float Amount;
}

/// <summary>
/// Pure damage math. Resistances apply per component, then the results are added.
/// </summary>
public static class DamageMath
{
    public static float AfterResist(IList<DamageComponent> parts, IList<DamageResistance> resists)
    {
        if (parts == null || parts.Count == 0)
            return 0f;

        float total = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            DamageComponent part = parts[i];
            float amount = part.Amount;
            if (amount <= 0f || float.IsNaN(amount))
                continue;

            float resist = 0f;
            if (resists != null)
            {
                for (int r = 0; r < resists.Count; r++)
                {
                    if (resists[r].Type == part.Type)
                        resist = resists[r].Amount;
                }
            }

            if (float.IsNaN(resist))
                resist = 0f;

            float factor = 1f - resist;
            if (factor < 0f)
                factor = 0f;

            total += amount * factor;
        }

        if (total < 0f || float.IsNaN(total))
            return 0f;

        return total;
    }
}
