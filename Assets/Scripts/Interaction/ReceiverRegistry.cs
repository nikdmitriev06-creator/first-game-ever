using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Living targets. Each receiver registers itself and leaves when disabled.
/// The list does not know which of them are trees.
/// </summary>
public static class ReceiverRegistry
{
    static readonly List<IImpactReceiver> All = new List<IImpactReceiver>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        All.Clear();
    }

    public static void Register(IImpactReceiver target)
    {
        if (target != null && !All.Contains(target))
            All.Add(target);
    }

    public static void Unregister(IImpactReceiver target)
    {
        All.Remove(target);
    }

    public static IImpactReceiver FindBest(Vector2 feet, Vector2 aim, float reach)
    {
        IImpactReceiver best = null;
        float bestScore = float.MaxValue;
        float reachSqr = reach * reach;

        for (int i = 0; i < All.Count; i++)
        {
            IImpactReceiver target = All[i];
            if (!AcceptsQuery(target))
                continue;

            Vector2 origin = target.Origin;
            if ((feet - origin).sqrMagnitude > reachSqr)
                continue;

            Vector2 crown = origin + Vector2.up * Mathf.Max(0.05f, target.Height);
            float aimDist = DistanceToSegment(aim, origin, crown);
            if (aimDist > target.Slop)
                continue;

            float score = aimDist + Vector2.Distance(feet, origin) * 0.08f;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = target;
        }

        return best;
    }

    public static void CollectNear(Vector2 center, float radius, List<IImpactReceiver> into)
    {
        into.Clear();
        float reachSqr = radius * radius;
        for (int i = 0; i < All.Count; i++)
        {
            IImpactReceiver target = All[i];
            if (!AcceptsQuery(target))
                continue;

            if ((center - target.Origin).sqrMagnitude <= reachSqr)
                into.Add(target);
        }
    }

    public static void CollectOnSegment(Vector2 from, Vector2 to, List<IImpactReceiver> into)
    {
        into.Clear();
        for (int i = 0; i < All.Count; i++)
        {
            IImpactReceiver target = All[i];
            if (!AcceptsQuery(target))
                continue;

            Vector2 origin = target.Origin;
            Vector2 crown = origin + Vector2.up * Mathf.Max(0.05f, target.Height);
            Vector2 mid = (origin + crown) * 0.5f;
            float slop = Mathf.Max(0.05f, target.Slop);
            if (DistanceToSegment(origin, from, to) > slop
                && DistanceToSegment(crown, from, to) > slop
                && DistanceToSegment(mid, from, to) > slop)
                continue;

            into.Add(target);
        }
    }

    static bool AcceptsQuery(IImpactReceiver target)
    {
        Behaviour body = target as Behaviour;
        return body != null && body.isActiveAndEnabled && target.CanReceive;
    }

    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 along = b - a;
        float length = along.sqrMagnitude;
        if (length < 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, along) / length);
        return Vector2.Distance(point, a + along * t);
    }
}
