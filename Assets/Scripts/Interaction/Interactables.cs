using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Living interactables. The focus asks for the nearest one. Each object registers itself.
/// </summary>
public static class Interactables
{
    static readonly List<IInteractable> All = new List<IInteractable>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        All.Clear();
    }

    public static void Register(IInteractable target)
    {
        if (target != null && !All.Contains(target))
            All.Add(target);
    }

    public static void Unregister(IInteractable target)
    {
        All.Remove(target);
    }

    public static IInteractable Nearest(Vector2 feet, float reach)
    {
        IInteractable best = null;
        float bestDistance = reach;
        for (int i = All.Count - 1; i >= 0; i--)
        {
            IInteractable target = All[i];
            if (target == null)
            {
                All.RemoveAt(i);
                continue;
            }

            float allowed = Mathf.Min(reach, Mathf.Max(0.2f, target.Radius));
            float distance = Vector2.Distance(feet, target.Origin);
            if (distance > allowed || distance > bestDistance)
                continue;

            bestDistance = distance;
            best = target;
        }

        return best;
    }
}
