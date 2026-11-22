using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A placed object that unlocks recipes. The id decides which recipes, not a subclass per bench.
/// </summary>
public class CraftingStation : MonoBehaviour
{
    public const string WorkbenchId = "workbench";

    static readonly List<CraftingStation> Active = new List<CraftingStation>();

    [SerializeField] string stationId = WorkbenchId;
    [SerializeField] float radius = 2.8f;

    public string StationId => stationId;
    public float Radius => radius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Active.Clear();
    }

    public void Configure(string id, float range)
    {
        stationId = id;
        radius = Mathf.Max(0.4f, range);
    }

    public static bool AnyInRange(string id, Vector2 point)
    {
        if (string.IsNullOrEmpty(id))
            return true;

        for (int i = 0; i < Active.Count; i++)
        {
            CraftingStation station = Active[i];
            if (station == null || !station.isActiveAndEnabled || station.stationId != id)
                continue;

            float range = station.radius;
            if (((Vector2)station.transform.position - point).sqrMagnitude <= range * range)
                return true;
        }

        return false;
    }

    public void Join()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    void OnEnable()
    {
        Join();
    }

    void OnDisable()
    {
        Active.Remove(this);
    }
}
