using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional strategies keyed by id. Missing ids stay on the plan so a presentation can handle them later.
/// </summary>
public static class AttackStrategies
{
    static readonly Dictionary<string, IAttackStrategy> Extra = new Dictionary<string, IAttackStrategy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Extra.Clear();
    }

    public static void Register(IAttackStrategy strategy)
    {
        if (strategy == null || string.IsNullOrEmpty(strategy.Id))
            return;

        Extra[strategy.Id] = strategy;
    }

    public static bool TryGet(string id, out IAttackStrategy strategy)
    {
        strategy = null;
        if (string.IsNullOrEmpty(id))
            return false;

        return Extra.TryGetValue(id, out strategy);
    }
}
