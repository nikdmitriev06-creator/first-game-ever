using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel sprites for the cabin and its room. There is no painted house sheet in the project,
/// so the planks, logs, door, and roof are drawn here and kept point-filtered.
/// </summary>
public static class HouseArt
{
    public const float PixelsPerUnit = 32f;

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 Outline = new Color32(54, 32, 18, 255);
    static readonly Color32 Seam = new Color32(72, 44, 24, 255);
    static readonly Color32 Knot = new Color32(92, 56, 30, 255);
    static readonly Color32 KnotRing = new Color32(196, 150, 92, 255);

    static readonly Color32[] Planks =
    {
        new Color32(176, 122, 68, 255),
        new Color32(154, 104, 56, 255),
        new Color32(190, 138, 78, 255),
        new Color32(138, 92, 48, 255),
        new Color32(166, 114, 62, 255),
        new Color32(148, 98, 52, 255)
    };

    static readonly Color32[] Logs =
    {
        new Color32(168, 112, 60, 255),
        new Color32(146, 96, 50, 255),
        new Color32(182, 126, 70, 255),
        new Color32(128, 84, 44, 255),
        new Color32(158, 104, 54, 255),
        new Color32(174, 118, 64, 255)
    };

    static Sprite _body;
    static Sprite _roof;
    static Sprite _shadow;
    static Sprite _quad;
    static Sprite _door;
    static Sprite _window;
    static Sprite _hearth;
    static Sprite _rug;
    static Sprite _roomBounds;
    static readonly Dictionary<int, Sprite> Boards = new Dictionary<int, Sprite>();
    static readonly Dictionary<int, Sprite> Beams = new Dictionary<int, Sprite>();

    public static Sprite CabinBody()
    {
        if (_body == null)
            _body = PaintBody();
        return _body;
    }

    public static Sprite CabinRoof()
    {
        if (_roof == null)
            _roof = PaintRoof();
        return _roof;
    }

    public static Sprite CabinShadow()
    {
        if (_shadow == null)
            _shadow = PaintShadow();
        return _shadow;
    }

    public static Sprite RoomBounds(float worldWidth, float worldHeight)
    {
        if (_roomBounds != null)
            return _roomBounds;

        int width = Mathf.Max(1, Mathf.RoundToInt(worldWidth * 2f));
        int height = Mathf.Max(1, Mathf.RoundToInt(worldHeight * 2f));
        var pixels = new Color32[width * height];
        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = white;
        _roomBounds = Finish(pixels, width, height, new Vector2(0.5f, 0.5f), 2f);
        return _roomBounds;
    }

    public static Sprite UnitQuad()
    {
        if (_quad != null)
            return _quad;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        _quad = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _quad.hideFlags = HideFlags.HideAndDontSave;
        return _quad;
    }

    public static Sprite FloorBoard(int width, int variant)
    {
        width = Mathf.Clamp(width, 8, 220);
        variant = Mathf.Abs(variant) % Planks.Length;
        int key = width * 16 + variant;
        if (Boards.TryGetValue(key, out Sprite cached))
            return cached;

        const int height = 18;
        var pixels = new Color32[width * height];
        Color32 wood = Planks[variant];
        Color32 lit = Scale(wood, 1.16f);
        Color32 shade = Scale(wood, 0.72f);
        Color32 grain = Scale(wood, 0.84f);
        Fill(pixels, width, height, 0, 0, width, height, wood);
        Fill(pixels, width, height, 0, height - 3, width, 2, lit);
        Fill(pixels, width, height, 0, 0, width, 2, Seam);
        Fill(pixels, width, height, 0, 2, width, 1, shade);

        for (int x = 2; x < width - 2; x++)
        {
            int grainRow = 6 + ((x + variant * 3) % 5 == 0 ? 1 : 0);
            if (((x * 3 + variant) & 7) == 0)
                Pixel(pixels, width, height, x, grainRow, grain);
            if (((x + variant * 5) % 11) == 0)
                Pixel(pixels, width, height, x, 10, shade);
        }

        int knotX = 8 + (variant * 17) % Mathf.Max(1, width - 16);
        FillEllipse(pixels, width, height, knotX, 9, 3, 2, KnotRing);
        FillEllipse(pixels, width, height, knotX, 9, 1, 1, Knot);
        Pixel(pixels, width, height, 3, 8, shade);
        Pixel(pixels, width, height, width - 4, 8, shade);

        Sprite sprite = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        Boards[key] = sprite;
        return sprite;
    }

    public static Sprite Log(int width, int variant)
    {
        width = Mathf.Clamp(width, 12, 900);
        variant = Mathf.Abs(variant) % Logs.Length;
        int key = width * 16 + variant;
        if (Beams.TryGetValue(key, out Sprite cached))
            return cached;

        const int height = 14;
        var pixels = new Color32[width * height];
        Color32 wood = Logs[variant];
        Color32 lit = Scale(wood, 1.18f);
        Color32 shade = Scale(wood, 0.68f);
        Color32 deep = Scale(wood, 0.5f);

        for (int y = 0; y < height; y++)
        {
            int inset = y < 2 || y > height - 3 ? 2 : (y < 4 || y > height - 5 ? 1 : 0);
            Fill(pixels, width, height, inset, y, width - inset * 2, 1, y < 2 ? Outline : wood);
        }

        Fill(pixels, width, height, 2, 2, width - 4, height - 4, wood);
        Fill(pixels, width, height, 3, height - 5, width - 6, 2, lit);
        Fill(pixels, width, height, 3, 2, width - 6, 2, shade);
        Fill(pixels, width, height, 2, 1, width - 4, 1, Outline);
        Fill(pixels, width, height, 2, height - 2, width - 4, 1, Outline);

        for (int x = 8; x < width - 8; x += 7)
        {
            if (((x + variant) & 3) == 0)
                Pixel(pixels, width, height, x, 7, shade);
        }

        int knotX = 18 + (variant * 29) % Mathf.Max(1, width - 40);
        FillEllipse(pixels, width, height, knotX, 7, 3, 2, KnotRing);
        Pixel(pixels, width, height, knotX, 7, Knot);
        if (width > 80)
        {
            int knotB = knotX + 36 + variant * 7;
            if (knotB < width - 16)
            {
                FillEllipse(pixels, width, height, knotB, 6, 2, 2, shade);
                Pixel(pixels, width, height, knotB, 6, Knot);
            }
        }

        DrawEnd(pixels, width, height, 7, deep, lit);
        DrawEnd(pixels, width, height, width - 8, deep, lit);
        Fill(pixels, width, height, 4, 4, 3, height - 8, deep);
        Fill(pixels, width, height, width - 7, 4, 3, height - 8, deep);

        Sprite sprite = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        Beams[key] = sprite;
        return sprite;
    }

    public static Sprite InteriorDoor()
    {
        if (_door != null)
            return _door;

        const int width = 78;
        const int height = 104;
        var pixels = new Color32[width * height];
        Fill(pixels, width, height, 0, 0, width, height, new Color32(28, 18, 12, 255));
        Fill(pixels, width, height, 0, 0, 8, height, Outline);
        Fill(pixels, width, height, width - 8, 0, 8, height, Outline);
        Fill(pixels, width, height, 0, height - 8, width, 8, Outline);
        Fill(pixels, width, height, 6, 0, width - 12, 8, new Color32(92, 60, 32, 255));

        Fill(pixels, width, height, 10, 10, 24, height - 20, new Color32(132, 86, 46, 255));
        Fill(pixels, width, height, 12, height - 22, 20, 8, new Color32(168, 116, 64, 255));
        Fill(pixels, width, height, 12, 18, 20, 6, new Color32(112, 72, 38, 255));
        Fill(pixels, width, height, 12, 48, 20, 6, new Color32(112, 72, 38, 255));
        Fill(pixels, width, height, 28, 36, 3, 8, new Color32(214, 176, 72, 255));
        Fill(pixels, width, height, 36, 12, 34, height - 24, new Color32(18, 12, 10, 255));

        _door = Finish(pixels, width, height, new Vector2(0.5f, 0f));
        return _door;
    }

    public static Sprite Window()
    {
        if (_window != null)
            return _window;

        const int width = 44;
        const int height = 36;
        var pixels = new Color32[width * height];
        Fill(pixels, width, height, 0, 0, width, height, Outline);
        Fill(pixels, width, height, 3, 3, width - 6, height - 6, new Color32(186, 206, 188, 255));
        Fill(pixels, width, height, width / 2 - 1, 3, 3, height - 6, Outline);
        Fill(pixels, width, height, 3, height / 2 - 1, width - 6, 3, Outline);
        Fill(pixels, width, height, 6, height - 10, 8, 4, new Color32(226, 236, 220, 255));
        Fill(pixels, width, height, width - 16, 8, 6, 3, new Color32(140, 160, 148, 255));
        _window = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        return _window;
    }

    public static Sprite Hearth()
    {
        if (_hearth != null)
            return _hearth;

        const int width = 72;
        const int height = 64;
        var pixels = new Color32[width * height];
        var stone = new Color32(122, 116, 108, 255);
        var stoneDark = new Color32(78, 74, 68, 255);
        var stoneLit = new Color32(168, 162, 150, 255);
        Fill(pixels, width, height, 4, 0, width - 8, 16, stoneDark);
        Fill(pixels, width, height, 0, 14, width, 18, stone);
        Fill(pixels, width, height, 2, 28, 14, 32, stone);
        Fill(pixels, width, height, width - 16, 28, 14, 32, stone);
        Fill(pixels, width, height, 4, 30, 10, 26, stoneLit);
        Fill(pixels, width, height, 16, 18, width - 32, 22, new Color32(48, 28, 18, 255));
        FillEllipse(pixels, width, height, width / 2, 26, 10, 6, new Color32(214, 96, 32, 255));
        FillEllipse(pixels, width, height, width / 2 - 4, 30, 5, 4, new Color32(236, 176, 48, 255));
        Fill(pixels, width, height, 8, 46, width - 16, 6, stoneDark);
        _hearth = Finish(pixels, width, height, new Vector2(0.5f, 0f));
        return _hearth;
    }

    public static Sprite Rug()
    {
        if (_rug != null)
            return _rug;

        const int width = 90;
        const int height = 50;
        var pixels = new Color32[width * height];
        FillEllipse(pixels, width, height, width / 2, height / 2, 42, 20, new Color32(112, 48, 36, 255));
        FillEllipse(pixels, width, height, width / 2, height / 2, 34, 14, new Color32(148, 72, 44, 255));
        for (int x = 16; x < width - 16; x += 6)
            Fill(pixels, width, height, x, 16, 2, 18, new Color32(92, 36, 28, 255));
        _rug = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        return _rug;
    }

    static void DrawEnd(Color32[] pixels, int width, int height, int cx, Color32 ring, Color32 core)
    {
        FillEllipse(pixels, width, height, cx, height / 2, 5, 4, ring);
        FillEllipse(pixels, width, height, cx, height / 2, 2, 2, core);
    }

    static Sprite PaintShadow()
    {
        const int width = 300;
        const int height = 48;
        var pixels = new Color32[width * height];
        FillEllipse(pixels, width, height, width / 2, 16, 130, 14, new Color32(20, 32, 16, 90));
        return Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
    }

    static Sprite PaintBody()
    {
        const int width = 320;
        const int height = 208;
        var pixels = new Color32[width * height];
        var stone = new Color32(132, 124, 112, 255);
        var stoneDark = new Color32(78, 74, 68, 255);
        var stoneLit = new Color32(176, 168, 154, 255);

        FillEllipse(pixels, width, height, width / 2, 8, 70, 8, stoneDark);
        Fill(pixels, width, height, 108, 6, 104, 10, stone);
        Fill(pixels, width, height, 116, 12, 88, 8, stoneLit);
        Fill(pixels, width, height, 18, 22, width - 36, 16, stoneDark);
        Fill(pixels, width, height, 22, 26, width - 44, 8, stone);

        int logTop = 168;
        int y = 40;
        int index = 0;
        while (y < logTop)
        {
            Color32 wood = Logs[index % Logs.Length];
            Color32 lit = Scale(wood, 1.14f);
            Color32 shade = Scale(wood, 0.7f);
            Fill(pixels, width, height, 16, y, width - 32, 12, Outline);
            Fill(pixels, width, height, 18, y + 2, width - 36, 8, wood);
            Fill(pixels, width, height, 20, y + 7, width - 40, 2, lit);
            Fill(pixels, width, height, 20, y + 2, width - 40, 2, shade);
            int knot = 48 + ((index * 37) % (width - 96));
            FillEllipse(pixels, width, height, knot, y + 6, 3, 2, Scale(wood, 0.78f));
            Pixel(pixels, width, height, knot, y + 6, Knot);
            Fill(pixels, width, height, 20, y + 4, 8, 4, Scale(wood, 0.55f));
            Fill(pixels, width, height, width - 28, y + 4, 8, 4, Scale(wood, 0.55f));
            y += 14;
            index++;
        }

        Fill(pixels, width, height, 8, 36, 18, logTop - 36, new Color32(92, 60, 32, 255));
        Fill(pixels, width, height, width - 26, 36, 18, logTop - 36, new Color32(92, 60, 32, 255));
        Fill(pixels, width, height, 10, logTop - 8, 14, 4, Scale(Logs[0], 1.1f));
        Fill(pixels, width, height, width - 24, logTop - 8, 14, 4, Scale(Logs[0], 1.1f));

        const int doorW = 68;
        const int doorH = 96;
        int doorX = (width - doorW) / 2;
        int doorY = 28;
        Fill(pixels, width, height, doorX - 6, doorY, doorW + 12, doorH + 8, Outline);
        Fill(pixels, width, height, doorX, doorY + 4, doorW, doorH, new Color32(118, 74, 40, 255));
        Fill(pixels, width, height, doorX + 4, doorY + doorH - 10, doorW / 2 - 8, 6, new Color32(156, 108, 58, 255));
        Fill(pixels, width, height, doorX + doorW / 2, doorY + 8, 2, doorH - 12, new Color32(78, 48, 26, 255));
        Fill(pixels, width, height, doorX + 6, doorY + 16, doorW / 2 - 12, 5, new Color32(96, 60, 32, 255));
        Fill(pixels, width, height, doorX + doorW / 2 + 6, doorY + 16, doorW / 2 - 12, 5, new Color32(96, 60, 32, 255));
        Fill(pixels, width, height, doorX + 6, doorY + 46, doorW / 2 - 12, 5, new Color32(96, 60, 32, 255));
        Fill(pixels, width, height, doorX + doorW / 2 + 6, doorY + 46, doorW / 2 - 12, 5, new Color32(96, 60, 32, 255));
        Fill(pixels, width, height, doorX + doorW - 16, doorY + 48, 5, 8, new Color32(212, 170, 64, 255));
        Fill(pixels, width, height, doorX - 4, doorY + 20, 4, 10, new Color32(70, 70, 74, 255));
        Fill(pixels, width, height, doorX - 4, doorY + 62, 4, 10, new Color32(70, 70, 74, 255));

        PaintWindow(pixels, width, height, 58, 108);
        PaintWindow(pixels, width, height, width - 102, 108);

        return Finish(pixels, width, height, new Vector2(0.5f, 0f));
    }

    static void PaintWindow(Color32[] pixels, int width, int height, int x, int y)
    {
        const int w = 40;
        const int h = 32;
        Fill(pixels, width, height, x, y, w, h, Outline);
        Fill(pixels, width, height, x + 3, y + 3, w - 6, h - 6, new Color32(198, 214, 196, 255));
        Fill(pixels, width, height, x + w / 2 - 1, y + 3, 3, h - 6, Outline);
        Fill(pixels, width, height, x + 3, y + h / 2 - 1, w - 6, 3, Outline);
        Fill(pixels, width, height, x + 6, y + h - 10, 7, 3, new Color32(230, 238, 224, 255));
    }

    static Sprite PaintRoof()
    {
        const int width = 360;
        const int height = 132;
        var pixels = new Color32[width * height];
        var shingle = new Color32(146, 64, 38, 255);
        var shingleLit = new Color32(176, 92, 48, 255);
        var shingleDark = new Color32(96, 42, 26, 255);
        var edge = new Color32(62, 28, 18, 255);

        int rows = 12;
        for (int row = 0; row < rows; row++)
        {
            float t = row / (float)(rows - 1);
            int inset = Mathf.RoundToInt(Mathf.Lerp(8f, 118f, t));
            int y = 8 + row * 8;
            int span = width - inset * 2;
            Color32 fill = row % 2 == 0 ? shingle : shingleDark;
            Fill(pixels, width, height, inset, y, span, 10, edge);
            Fill(pixels, width, height, inset + 2, y + 2, span - 4, 6, fill);
            Fill(pixels, width, height, inset + 2, y + 6, span - 4, 2, shingleLit);
            int shift = row % 2 == 0 ? 0 : 8;
            for (int x = inset + 8 + shift; x < width - inset - 4; x += 16)
                Fill(pixels, width, height, x, y + 2, 2, 6, edge);
        }

        Fill(pixels, width, height, 4, 6, width - 8, 6, new Color32(48, 24, 16, 220));
        int cap = width / 2;
        Fill(pixels, width, height, cap - 28, height - 18, 56, 8, edge);
        Fill(pixels, width, height, cap - 24, height - 16, 48, 5, shingleLit);

        int chimneyX = width - 108;
        Fill(pixels, width, height, chimneyX, 70, 28, 52, new Color32(92, 48, 40, 255));
        Fill(pixels, width, height, chimneyX + 3, 74, 22, 44, new Color32(142, 78, 62, 255));
        Fill(pixels, width, height, chimneyX - 2, 116, 32, 8, new Color32(78, 40, 34, 255));
        Fill(pixels, width, height, chimneyX + 6, 96, 8, 4, new Color32(186, 112, 88, 255));
        Fill(pixels, width, height, chimneyX + 8, 122, 10, 6, new Color32(70, 70, 74, 180));

        return Finish(pixels, width, height, new Vector2(0.5f, 0f));
    }

    static void Pixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            return;
        pixels[y * width + x] = color;
    }

    static void Fill(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
    {
        int x1 = Mathf.Min(width, x + w);
        int y1 = Mathf.Min(height, y + h);
        int x0 = Mathf.Max(0, x);
        int y0 = Mathf.Max(0, y);
        for (int py = y0; py < y1; py++)
        {
            int row = py * width;
            for (int px = x0; px < x1; px++)
                pixels[row + px] = color;
        }
    }

    static void FillEllipse(Color32[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color32 color)
    {
        if (rx < 1 || ry < 1)
            return;

        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy > 1f)
                    continue;
                Pixel(pixels, width, height, x, y, color);
            }
        }
    }

    static Color32 Scale(Color32 color, float factor)
    {
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
            color.a);
    }

    static Sprite Finish(Color32[] pixels, int width, int height, Vector2 pivot)
    {
        return Finish(pixels, width, height, pivot, PixelsPerUnit);
    }

    static Sprite Finish(Color32[] pixels, int width, int height, Vector2 pivot, float pixelsPerUnit)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, pixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
