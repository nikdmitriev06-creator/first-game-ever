using UnityEngine;

public enum ItemCategory
{
    Resource,
    Tool,
    Weapon,
    Placeable
}

/// <summary>
/// Where a carried placeable is allowed to become a world object.
/// Home covers the meadow and the house. Indoor is the house only.
/// </summary>
public enum PlacementZone
{
    None,
    Home,
    Indoor
}

/// <summary>
/// Data for one item. New items are assets, not new branches in the inventory.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] string displayName;
    [SerializeField] string displayNameEnglish;
    [SerializeField] Sprite icon;
    [SerializeField] Sprite worldSprite;
    [SerializeField] int maxStack = 1;
    [SerializeField] ItemCategory category;
    [SerializeField] PlacementZone placement = PlacementZone.None;
    [SerializeField] GameObject prefab;
    [SerializeField] string stationId;
    [SerializeField] float stationRadius = 2.8f;
    [SerializeField] int durability = 1;
    [SerializeField] float footprint = 0.6f;
    [SerializeField] ToolProfile tool;
    [SerializeField] PlacementPolicy placementPolicy;
    [SerializeField] MaterialDefinition breakMaterial;
    [SerializeField] bool indestructible;

    public string Id => id;
    public string DisplayName => displayName;
    public string DisplayNameEnglish => displayNameEnglish;
    public int MaxStack => Mathf.Max(1, maxStack);
    public ItemCategory Category => category;
    public PlacementZone Placement => placement;
    public bool IsPlaceable => placement != PlacementZone.None;
    public GameObject Prefab => prefab;
    public string StationId => stationId;
    public float StationRadius => stationRadius;
    public int Durability => Mathf.Max(1, durability);
    public float Footprint => Mathf.Max(0.2f, footprint);
    public bool IsStation => !string.IsNullOrEmpty(stationId);
    public MaterialDefinition BreakMaterial => breakMaterial;
    public bool Indestructible => indestructible;

    public ToolProfile ResolvedTool
    {
        get
        {
            if (tool != null && tool.Gesture != ToolGesture.None && tool.Impact != null)
                return tool;

            if (id != ItemIds.Axe)
                return null;

            ImpactLibrary.Ensure();
            if (_axeTool == null || _axeTool.Impact != ImpactLibrary.Chop)
                _axeTool = ToolProfile.Swing(ImpactLibrary.Chop, 1f, 3.6f, 6f);

            return _axeTool;
        }
    }

    public bool HasTool => ResolvedTool != null;

    static ToolProfile _axeTool;

    public Sprite IconSprite => icon != null ? icon : ItemArt.Icon(id);

    public Sprite PlacedSprite
    {
        get
        {
            if (worldSprite != null)
                return worldSprite;

            Sprite painted = ItemArt.WorldSprite(id);
            return painted != null ? painted : IconSprite;
        }
    }

    public bool Fits(LocationContext place)
    {
        if (place == null || !IsPlaceable)
            return false;

        PlacementPolicy rules = placementPolicy != null ? placementPolicy : PolicyLibrary.For(placement);
        return rules != null && rules.Allows(place);
    }

    public void AssignTool(ToolProfile profile)
    {
        tool = profile;
    }

    public void AssignPolicy(PlacementPolicy policy)
    {
        placementPolicy = policy;
    }

    public void AssignBreakMaterial(MaterialDefinition material)
    {
        breakMaterial = material;
    }

    public void Configure(
        string itemId,
        string russianName,
        string englishName,
        int stack,
        ItemCategory itemCategory,
        PlacementZone zone,
        string station,
        float radius,
        int hits,
        float size,
        GameObject placePrefab)
    {
        id = itemId;
        displayName = russianName;
        displayNameEnglish = englishName;
        maxStack = Mathf.Max(1, stack);
        category = itemCategory;
        placement = zone;
        stationId = station;
        stationRadius = radius;
        durability = Mathf.Max(1, hits);
        footprint = size;
        prefab = placePrefab;
    }

    public void AssignPrefab(GameObject placePrefab)
    {
        prefab = placePrefab;
    }
}
