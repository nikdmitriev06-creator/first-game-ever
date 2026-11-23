using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps world art on the isometric pixel grid: 32 pixels per unit, point filter, no compression.
/// </summary>
public class TerrainSpriteImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Art/World/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        if (importer.spriteImportMode == SpriteImportMode.None)
            importer.spriteImportMode = SpriteImportMode.Single;
    }
}
