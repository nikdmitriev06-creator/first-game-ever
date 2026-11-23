using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The surfaces a designer can paint. Rooms store their own references; this is the palette.
/// </summary>
[CreateAssetMenu(fileName = "TerrainLibrary", menuName = "Game/Levels/Terrain Library")]
public class TerrainLibrary : ScriptableObject
{
    [SerializeField] TerrainDefinition[] terrains = System.Array.Empty<TerrainDefinition>();

    public IReadOnlyList<TerrainDefinition> All => terrains ?? System.Array.Empty<TerrainDefinition>();

    public void SetTerrains(TerrainDefinition[] next)
    {
        terrains = next ?? System.Array.Empty<TerrainDefinition>();
    }

    public TerrainDefinition Find(string terrainId)
    {
        if (string.IsNullOrEmpty(terrainId) || terrains == null)
            return null;

        for (int i = 0; i < terrains.Length; i++)
        {
            TerrainDefinition terrain = terrains[i];
            if (terrain != null && terrain.Id == terrainId)
                return terrain;
        }

        return null;
    }
}
