using System;
using UnityEngine;

/// <summary>
/// Home floor made of authored tiles in the meadow's colours.
/// Grass is one continuous field, so any two tiles meet on the same grass.
/// Flowers, dirt and stones stay inset, and are never sliced by the join.
/// </summary>
public static class HomeFloor
{
    const int Size = 64;
    const int Inset = 10;

    static readonly Color32 Grass = new Color32(136, 168, 56, 255);
    static readonly Color32 GrassShade = new Color32(128, 160, 52, 255);
    static readonly Color32 GrassLight = new Color32(144, 174, 62, 255);
    static readonly Color32 GrassDeep = new Color32(120, 152, 46, 255);
    static readonly Color32 Blade = new Color32(86, 122, 44, 255);
    static readonly Color32 Tuft = new Color32(58, 100, 38, 255);
    static readonly Color32 TuftLit = new Color32(96, 136, 48, 255);
    static readonly Color32 Dirt = new Color32(208, 160, 96, 255);
    static readonly Color32 DirtDark = new Color32(176, 128, 72, 255);
    static readonly Color32 DirtLight = new Color32(224, 184, 124, 255);
    static readonly Color32 PetalWhite = new Color32(242, 238, 220, 255);
    static readonly Color32 PetalYellow = new Color32(236, 198, 52, 255);
    static readonly Color32 FlowerCore = new Color32(186, 132, 32, 255);
    static readonly Color32 Rock = new Color32(138, 136, 128, 255);
    static readonly Color32 RockDark = new Color32(86, 84, 78, 255);
    static readonly Color32 RockLight = new Color32(186, 182, 170, 255);

    // Empty grass is most of the bag. Decorated tiles are the exceptions, so two
    // neighbours are usually plain grass meeting plain grass, or a detail fading
    // out before the shared edge.
    static readonly int[] Bag =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10
    };

    static Color32[][] _masks;

    public static Sprite Build(Sprite reference, int seed)
    {
        if (reference == null)
            return null;

        EnsureMasks();
        int width = Mathf.RoundToInt(reference.rect.width);
        int height = Mathf.RoundToInt(reference.rect.height);
        if (width < Size || height < Size)
            return null;

        var pixels = new Color32[width * height];
        var rng = new System.Random(seed);
        for (int y = 0; y < height; y += Size)
        {
            for (int x = 0; x < width; x += Size)
                Paint(pixels, width, height, x, y, Bag[rng.Next(Bag.Length)]);
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            reference.pixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static void EnsureMasks()
    {
        if (_masks != null)
            return;

        _masks = new Color32[11][];
        for (int id = 0; id < _masks.Length; id++)
            _masks[id] = Mask(id);
    }

    static void Paint(Color32[] dest, int destW, int destH, int originX, int originY, int id)
    {
        Color32[] mask = _masks[id];
        int x1 = Mathf.Min(Size, destW - originX);
        int y1 = Mathf.Min(Size, destH - originY);
        for (int y = 0; y < y1; y++)
        {
            int row = (originY + y) * destW + originX;
            int maskRow = y * Size;
            for (int x = 0; x < x1; x++)
            {
                Color32 feature = mask[maskRow + x];
                dest[row + x] = feature.a == 0 ? WorldGrass(originX + x, originY + y) : feature;
            }
        }
    }

    static Color32 WorldGrass(int wx, int wy)
    {
        int h = Hash(wx >> 1, wy >> 1, 3);
        if (h % 29 == 0)
            return Blade;
        int pick = h & 7;
        if (pick == 0)
            return GrassShade;
        if (pick == 1)
            return GrassLight;
        if (pick == 2)
            return GrassDeep;
        return Grass;
    }

    static Color32[] Mask(int id)
    {
        var tile = new Color32[Size * Size];
        switch (id)
        {
            case 1:
                DrawTuft(tile, 22, 40);
                DrawTuft(tile, 36, 28);
                DrawTuft(tile, 44, 42);
                break;
            case 2:
                DrawTuft(tile, 26, 34);
                DrawTuft(tile, 40, 36);
                break;
            case 3:
                Flower(tile, 30, 34, true);
                Flower(tile, 42, 28, true);
                DrawTuft(tile, 24, 42);
                break;
            case 4:
                Flower(tile, 28, 30, false);
                Flower(tile, 40, 38, false);
                Flower(tile, 34, 44, false);
                break;
            case 5:
                Flower(tile, 26, 36, true);
                Flower(tile, 40, 32, false);
                DrawTuft(tile, 34, 44);
                break;
            case 6:
                DirtBlob(tile, 32, 34, 12, 8, 6);
                break;
            case 7:
                DirtBlob(tile, 32, 33, 16, 11, 7);
                break;
            case 8:
                DirtBlob(tile, 30, 34, 13, 9, 8);
                Pebble(tile, 38, 32);
                break;
            case 9:
                Pebble(tile, 26, 34);
                Pebble(tile, 40, 38);
                break;
            case 10:
                Pebble(tile, 34, 32);
                DrawTuft(tile, 24, 40);
                DrawTuft(tile, 44, 28);
                break;
        }

        return tile;
    }

    static void DrawTuft(Color32[] tile, int cx, int cy)
    {
        Put(tile, cx, cy + 2, Tuft);
        Put(tile, cx - 1, cy + 1, Tuft);
        Put(tile, cx, cy + 1, TuftLit);
        Put(tile, cx + 1, cy + 1, Tuft);
        Put(tile, cx - 1, cy, TuftLit);
        Put(tile, cx, cy, Blade);
        Put(tile, cx + 1, cy, TuftLit);
        Put(tile, cx, cy - 1, Tuft);
    }

    static void Flower(Color32[] tile, int cx, int cy, bool white)
    {
        Color32 petal = white ? PetalWhite : PetalYellow;
        Put(tile, cx, cy, white ? PetalYellow : FlowerCore);
        Put(tile, cx - 1, cy, petal);
        Put(tile, cx + 1, cy, petal);
        Put(tile, cx, cy - 1, petal);
        Put(tile, cx, cy + 1, petal);
        Put(tile, cx, cy + 2, TuftLit);
        Put(tile, cx, cy + 3, Tuft);
    }

    static void Pebble(Color32[] tile, int cx, int cy)
    {
        Put(tile, cx - 1, cy + 1, RockDark);
        Put(tile, cx, cy + 1, Rock);
        Put(tile, cx + 1, cy + 1, RockDark);
        Put(tile, cx - 2, cy, RockDark);
        Put(tile, cx - 1, cy, Rock);
        Put(tile, cx, cy, RockLight);
        Put(tile, cx + 1, cy, Rock);
        Put(tile, cx + 2, cy, RockDark);
        Put(tile, cx - 1, cy - 1, Rock);
        Put(tile, cx, cy - 1, RockLight);
        Put(tile, cx + 1, cy - 1, Rock);
        Put(tile, cx, cy - 2, RockDark);
    }

    static void DirtBlob(Color32[] tile, int cx, int cy, int rx, int ry, int salt)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                float ellipse = dx * dx + dy * dy;
                if (ellipse > 1f)
                    continue;

                int h = Hash(x, y, salt);
                Color32 color = ellipse > 0.75f ? DirtDark : (h % 5 == 0 ? DirtLight : Dirt);
                Put(tile, x, y, color);
            }
        }
    }

    static void Put(Color32[] tile, int x, int y, Color32 color)
    {
        if ((uint)x >= Size || (uint)y >= Size)
            return;
        if (Edge(x, y) < Inset)
            return;

        color.a = 255;
        tile[y * Size + x] = color;
    }

    static int Edge(int x, int y)
    {
        int dx = x < Size - 1 - x ? x : Size - 1 - x;
        int dy = y < Size - 1 - y ? y : Size - 1 - y;
        return dx < dy ? dx : dy;
    }

    static int Hash(int a, int b, int c)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263 + c * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            return h & 0x7fffffff;
        }
    }
}
