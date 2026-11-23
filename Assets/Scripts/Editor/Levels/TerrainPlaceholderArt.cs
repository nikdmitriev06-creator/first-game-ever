using System.IO;
using UnityEngine;

/// <summary>
/// Stand-in sheets so rooms can be painted before real tiles exist.
/// Each file is a 2:1 isometric sheet: 16 neighbour masks and 4 centre variants.
/// </summary>
public static class TerrainPlaceholderArt
{
    public const int TileWidth = 64;
    public const int TileHeight = 32;
    public const int Slices = 20;

    public static void WriteSheet(string path, Color color, int style)
    {
        int width = TileWidth * Slices;
        var pixels = new Color32[width * TileHeight];
        for (int slice = 0; slice < Slices; slice++)
        {
            int mask = slice < 16 ? slice : 15;
            int variant = slice < 16 ? 1 : slice - 14;
            PaintSlice(pixels, width, slice * TileWidth, mask, variant, style, color);
        }

        Write(path, pixels, width, TileHeight);
    }

    public static void WriteProp(string path, int width, int height, int kind)
    {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (InsideProp(x, y, width, height, kind))
                    pixels[y * width + x] = PropColor(x, y, kind);
            }
        }

        Write(path, pixels, width, height);
    }

    static void PaintSlice(Color32[] pixels, int stride, int origin, int mask, int variant, int style, Color color)
    {
        for (int y = 0; y < TileHeight; y++)
        {
            for (int x = 0; x < TileWidth; x++)
            {
                float nx = (x + 0.5f - 32f) / 32f;
                float ny = (y + 0.5f - 16f) / 16f;
                float diamond = Mathf.Abs(nx) + Mathf.Abs(ny);
                if (diamond > 1f)
                    continue;

                if (style == 20 && diamond > 0.42f && ((x + y + variant) % 5) != 0)
                    continue;

                Color32 ink = color;
                int noise = ((x * 17 + y * 31 + variant * 13 + style * 7) & 7) - 3;
                ink = Shift(ink, noise * 5);
                ink = Motif(ink, x, y, nx, ny, style, variant);
                if (diamond > 0.8f && Missing(mask, nx, ny))
                    ink = Shift(ink, -50);
                else if (diamond > 0.92f)
                    ink = Shift(ink, -24);

                pixels[y * stride + origin + x] = ink;
            }
        }
    }

    static Color32 Motif(Color32 ink, int x, int y, float nx, float ny, int style, int variant)
    {
        if (style == 6 && Mathf.Abs(ny) < 0.08f)
            return Shift(ink, -30);
        if (style == 7 && ((x / 8 + y / 4) & 1) == 0)
            return Shift(ink, 16);
        if (style == 8 && (x + y + variant) % 9 == 0)
            return Shift(ink, 20);
        if ((style == 10 || style == 11 || style == 12) && (y / 3) % 2 == 0)
            return Shift(ink, style == 12 ? -10 : 18);
        if (style == 18 && ny > 0.15f)
            return Shift(ink, 22);
        if (style == 19 && (x / 7) % 2 == 0)
            return Shift(ink, -18);
        if (style == 14 && diamondish(nx, ny) < 0.45f)
            return Shift(ink, -20);
        return ink;
    }

    static float diamondish(float nx, float ny)
    {
        return Mathf.Abs(nx) + Mathf.Abs(ny);
    }

    static bool Missing(int mask, float nx, float ny)
    {
        if (nx > 0.18f && ny > 0.18f && (mask & 2) == 0)
            return true;
        if (nx < -0.18f && ny > 0.18f && (mask & 1) == 0)
            return true;
        if (nx < -0.18f && ny < -0.18f && (mask & 8) == 0)
            return true;
        if (nx > 0.18f && ny < -0.18f && (mask & 4) == 0)
            return true;
        return false;
    }

    static bool InsideProp(int x, int y, int width, int height, int kind)
    {
        float nx = (x - width * 0.5f) / (width * 0.5f);
        float ny = (y - height * 0.2f) / (height * 0.8f);
        if (kind == 0)
        {
            bool crown = y > height * 0.28f && nx * nx + (ny - 0.35f) * (ny - 0.35f) < 0.55f;
            bool trunk = Mathf.Abs(x - width * 0.5f) < width * 0.12f && y < height * 0.42f && y > 2;
            return crown || trunk;
        }

        if (kind == 2)
            return nx * nx * 1.3f + (ny - 0.15f) * (ny - 0.15f) < 0.5f && y > 2;

        float rock = nx * nx + (y / (float)height - 0.25f) * (y / (float)height - 0.25f) * 3f;
        return rock < 0.7f && y > 1;
    }

    static Color32 PropColor(int x, int y, int kind)
    {
        if (kind == 0)
        {
            bool trunk = y < 28;
            return trunk ? new Color32(104, 66, 36, 255) : new Color32(62, 122, 48, 255);
        }

        if (kind == 1)
            return ((x + y) & 3) == 0 ? new Color32(150, 148, 140, 255) : new Color32(110, 108, 102, 255);

        if (kind == 2)
            return (x + y) % 5 == 0 ? new Color32(72, 150, 88, 255) : new Color32(126, 96, 70, 255);

        return new Color32(46, 110, 48, 255);
    }

    static Color32 Shift(Color32 color, int delta)
    {
        return new Color32(Clamp(color.r + delta), Clamp(color.g + delta), Clamp(color.b + delta), 255);
    }

    static byte Clamp(int value)
    {
        return (byte)Mathf.Clamp(value, 0, 255);
    }

    static void Write(string path, Color32[] pixels, int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }
}
