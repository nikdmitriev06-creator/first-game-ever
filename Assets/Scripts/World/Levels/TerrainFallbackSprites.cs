using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Last-resort diamond when a surface has no imported sprite yet.
/// Real rooms use the placeholder sheets under Art/World/Tiles, which an artist replaces in place.
/// </summary>
public static class TerrainFallbackSprites
{
    const int Width = 64;
    const int Height = 32;

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Cache.Clear();
    }

    public static Sprite Get(Color color, string id)
    {
        string key = string.IsNullOrEmpty(id) ? "missing" : id;
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        var pixels = new Color32[Width * Height];
        Color32 fill = color;
        Color32 edge = new Color32(
            (byte)(fill.r / 2),
            (byte)(fill.g / 2),
            (byte)(fill.b / 2),
            255);
        for (int py = 0; py < Height; py++)
        {
            for (int px = 0; px < Width; px++)
            {
                float nx = (px + 0.5f - 32f) / 32f;
                float ny = (py + 0.5f - 16f) / 16f;
                float diamond = Mathf.Abs(nx) + Mathf.Abs(ny);
                if (diamond > 1f)
                    continue;

                pixels[py * Width + px] = diamond > 0.86f ? edge : fill;
            }
        }

        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Width, Height), new Vector2(0.5f, 0.5f), 32f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        Cache[key] = sprite;
        return sprite;
    }
}
