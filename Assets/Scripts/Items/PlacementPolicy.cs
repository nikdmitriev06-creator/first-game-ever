using UnityEngine;

/// <summary>
/// Where a placeable is allowed to stand. Items share a policy instead of switching on their own ids.
/// </summary>
[CreateAssetMenu(fileName = "PlacementPolicy", menuName = "Game/Placement Policy")]
public class PlacementPolicy : ScriptableObject
{
    [SerializeField] bool anyRegion = true;
    [SerializeField] RegionKind region = RegionKind.Home;
    [SerializeField] bool anyKind = true;
    [SerializeField] LocationKind kind = LocationKind.Outdoor;

    public void Configure(bool restrictRegion, RegionKind regionValue, bool restrictKind, LocationKind kindValue)
    {
        anyRegion = !restrictRegion;
        region = regionValue;
        anyKind = !restrictKind;
        kind = kindValue;
    }

    public bool Allows(LocationContext place)
    {
        if (place == null)
            return false;

        if (!anyRegion && place.Region != region)
            return false;

        if (!anyKind && place.Kind != kind)
            return false;

        return true;
    }
}

public static class PolicyLibrary
{
    static PlacementPolicy _home;
    static PlacementPolicy _indoor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _home = null;
        _indoor = null;
    }

    public static void Ensure()
    {
        if (_home != null && _indoor != null)
            return;

        PlacementPolicy[] loaded = Resources.LoadAll<PlacementPolicy>("GameContent");
        _home = Find(loaded, "policy.home") ?? Make("policy.home", true, RegionKind.Home, false, LocationKind.Outdoor);
        _indoor = Find(loaded, "policy.indoor") ?? Make("policy.indoor", false, RegionKind.Home, true, LocationKind.Indoor);
    }

    public static PlacementPolicy For(PlacementZone zone)
    {
        Ensure();
        if (zone == PlacementZone.Home)
            return _home;
        if (zone == PlacementZone.Indoor)
            return _indoor;
        return null;
    }

    static PlacementPolicy Find(PlacementPolicy[] loaded, string name)
    {
        if (loaded == null)
            return null;

        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].name == name)
                return loaded[i];
        }

        return null;
    }

    static PlacementPolicy Make(string name, bool restrictRegion, RegionKind region, bool restrictKind, LocationKind kind)
    {
        PlacementPolicy policy = ScriptableObject.CreateInstance<PlacementPolicy>();
        policy.name = name;
        policy.Configure(restrictRegion, region, restrictKind, kind);
        policy.hideFlags = HideFlags.HideAndDontSave;
        return policy;
    }
}
