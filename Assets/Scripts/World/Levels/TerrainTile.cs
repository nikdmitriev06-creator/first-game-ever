using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One paint tile. Neighbours of the same connect group pick the edge sprite.
/// Colliders stay off: walking rules live in <see cref="TerrainNavigation"/>.
/// </summary>
[CreateAssetMenu(fileName = "TerrainTile", menuName = "Game/Levels/Terrain Tile")]
public class TerrainTile : TileBase
{
    [SerializeField] TerrainDefinition definition;

    public TerrainDefinition Definition => definition;

    public void Bind(TerrainDefinition terrain)
    {
        definition = terrain;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.colliderType = Tile.ColliderType.None;
        tileData.flags = TileFlags.None;
        tileData.transform = Matrix4x4.identity;
        tileData.color = Color.white;
        tileData.sprite = null;
        if (definition == null)
            return;

        int mask = NeighborMask(position, tilemap);
        Sprite sprite = definition.SpriteFor(mask, position.x, position.y);
        if (sprite == null)
            sprite = TerrainFallbackSprites.Get(definition.Swatch, definition.Id);
        tileData.sprite = sprite;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int next = new Vector3Int(position.x + x, position.y + y, position.z);
                tilemap.RefreshTile(next);
            }
        }
    }

    int NeighborMask(Vector3Int position, ITilemap tilemap)
    {
        int mask = 0;
        if (Connected(tilemap, position + new Vector3Int(0, 1, 0)))
            mask |= 1;
        if (Connected(tilemap, position + new Vector3Int(1, 0, 0)))
            mask |= 2;
        if (Connected(tilemap, position + new Vector3Int(0, -1, 0)))
            mask |= 4;
        if (Connected(tilemap, position + new Vector3Int(-1, 0, 0)))
            mask |= 8;
        return mask;
    }

    bool Connected(ITilemap tilemap, Vector3Int position)
    {
        TileBase other = tilemap.GetTile(position);
        TerrainTile terrain = other as TerrainTile;
        return terrain != null && definition != null && definition.SameGroup(terrain.definition);
    }
}
