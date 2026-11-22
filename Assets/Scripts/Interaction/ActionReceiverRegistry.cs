using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Targets of tools that do not deal damage. Objects join in OnEnable and leave in OnDisable.
/// </summary>
public static class ActionReceiverRegistry
{
    static readonly List<IToolActionReceiver> All = new List<IToolActionReceiver>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        All.Clear();
    }

    public static void Register(IToolActionReceiver target)
    {
        if (target != null && !All.Contains(target))
            All.Add(target);
    }

    public static void Unregister(IToolActionReceiver target)
    {
        All.Remove(target);
    }

    public static int Deliver(in ToolAction action, float range)
    {
        int applied = 0;
        float reach = Mathf.Max(0.1f, range);
        float reachSqr = reach * reach;
        for (int i = 0; i < All.Count; i++)
        {
            IToolActionReceiver target = All[i];
            if (target == null || !target.CanReceiveAction)
                continue;

            if ((target.Origin - action.Point).sqrMagnitude > reachSqr)
                continue;

            if (target.Apply(action))
                applied++;
        }

        return applied;
    }
}
