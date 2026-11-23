using UnityEngine;

/// <summary>
/// Gameplay and art for one surface. A new surface is a new asset, not a new movement class.
/// </summary>
[CreateAssetMenu(fileName = "Terrain", menuName = "Game/Levels/Terrain")]
public class TerrainDefinition : ScriptableObject
{
    [SerializeField] string id = "terrain.grass";
    [SerializeField] string displayName = "Трава";
    [SerializeField] PassageKind passage = PassageKind.Ground;
    [SerializeField] int height;
    [SerializeField] float speed = 1f;
    [SerializeField] string connectGroup = "grass";
    [SerializeField] string hazardId;
    [SerializeField] Color swatch = new Color(0.35f, 0.55f, 0.28f, 1f);
    [SerializeField] Sprite[] centers = System.Array.Empty<Sprite>();
    [SerializeField] Sprite[] edges = System.Array.Empty<Sprite>();
    [SerializeField] TerrainTile tile;

    public string Id => id;
    public string DisplayName => displayName;
    public PassageKind Passage => passage;
    public int Height => height;
    public float Speed => speed > 0f ? speed : 1f;
    public string ConnectGroup => string.IsNullOrEmpty(connectGroup) ? id : connectGroup;
    public string HazardId => hazardId;
    public Color Swatch => swatch;
    public TerrainTile Tile => tile;
    public bool Walkable => passage != PassageKind.Blocked;
    public bool Ramp => passage == PassageKind.Ramp;

    public void Configure(
        string terrainId,
        string name,
        PassageKind kind,
        int cellHeight,
        string group,
        Color color)
    {
        id = terrainId;
        displayName = name;
        passage = kind;
        height = cellHeight;
        connectGroup = group;
        swatch = color;
        if (speed <= 0f)
            speed = 1f;
    }

    public void BindArt(Sprite[] centerSprites, Sprite[] edgeSprites, TerrainTile terrainTile)
    {
        centers = centerSprites ?? System.Array.Empty<Sprite>();
        edges = edgeSprites ?? System.Array.Empty<Sprite>();
        tile = terrainTile;
    }

    public Sprite SpriteFor(int mask, int x, int y)
    {
        if (mask == 15 && centers != null && centers.Length > 0)
            return centers[Mathf.Abs(Hash(x, y)) % centers.Length];

        if (edges != null && mask >= 0 && mask < edges.Length && edges[mask] != null)
            return edges[mask];

        if (centers != null && centers.Length > 0)
            return centers[Mathf.Abs(Hash(x, y)) % centers.Length];

        return null;
    }

    public bool SameGroup(TerrainDefinition other)
    {
        return other != null && ConnectGroup == other.ConnectGroup;
    }

    static int Hash(int x, int y)
    {
        unchecked
        {
            return (x * 73856093) ^ (y * 19349663);
        }
    }
}
