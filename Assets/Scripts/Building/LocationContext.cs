using UnityEngine;

public enum LocationKind
{
    Outdoor,
    Indoor
}

public enum RegionKind
{
    Home,
    Wilderness
}

/// <summary>
/// What kind of space the player is standing in. Placement reads this, not a scene name.
/// The active room and the house interior each carry one, and only the enabled space is current.
/// </summary>
public class LocationContext : MonoBehaviour
{
    [SerializeField] RegionKind region = RegionKind.Home;
    [SerializeField] LocationKind kind = LocationKind.Outdoor;
    [SerializeField] string biomeId;
    [SerializeField] PlayArea area;
    Transform _drops;

    public static LocationContext Active { get; private set; }

    public RegionKind Region => region;
    public LocationKind Kind => kind;
    public string BiomeId => biomeId;
    public PlayArea Area => area;
    public Transform DropParent => _drops != null ? _drops : transform;

    public void SetDropParent(Transform parent)
    {
        _drops = parent;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Active = null;
    }

    public static void Use(LocationContext context)
    {
        Active = context;
    }

    public void Configure(RegionKind nextRegion, LocationKind nextKind, PlayArea nextArea)
    {
        region = nextRegion;
        kind = nextKind;
        area = nextArea;
    }

    public void SetBiome(string id)
    {
        biomeId = id;
    }

    public bool Contains(Vector2 world, float inset)
    {
        if (area == null)
            return false;

        Vector2 clamped = area.Clamp(world, Mathf.Max(0f, inset));
        return (clamped - world).sqrMagnitude <= 0.0004f;
    }

    void OnEnable()
    {
        Active = this;
    }

    void OnDisable()
    {
        if (Active == this)
            Active = null;
    }
}
