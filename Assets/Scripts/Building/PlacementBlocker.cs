using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A solid footprint that is not itself a placeable, such as the cabin walls.
/// </summary>
public class PlacementBlocker : MonoBehaviour
{
    static readonly List<PlacementBlocker> All = new List<PlacementBlocker>();

    [SerializeField] Vector2 center;
    [SerializeField] Vector2 extents = new Vector2(1f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        All.Clear();
    }

    public void Configure(Vector2 localCenter, Vector2 localExtents)
    {
        center = localCenter;
        extents = new Vector2(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y));
    }

    public static bool Hits(Vector2 world)
    {
        for (int i = 0; i < All.Count; i++)
        {
            PlacementBlocker blocker = All[i];
            if (blocker == null || !blocker.isActiveAndEnabled)
                continue;

            Vector2 local = blocker.transform.InverseTransformPoint(world);
            Vector2 delta = local - blocker.center;
            if (Mathf.Abs(delta.x) <= blocker.extents.x && Mathf.Abs(delta.y) <= blocker.extents.y)
                return true;
        }

        return false;
    }

    void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }
}
