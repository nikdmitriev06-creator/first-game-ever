using UnityEngine;

/// <summary>
/// Pixel icons for items that are not a painted weapon sheet.
/// The sword and the bow keep the sprite on their own renderer.
/// </summary>
public static class ItemArt
{
    const float PixelsPerUnit = 32f;

    static Sprite _axe;
    static Sprite _pickaxe;
    static Sprite _wood;
    static Sprite _stone;
    static Sprite _copperOre;
    static Sprite _workbench;
    static Sprite _chair;
    static Sprite _furnace;
    static Sprite _shadow;

    public static Sprite Icon(string id)
    {
        if (id == ItemIds.Axe)
            return Axe();
        if (id == ItemIds.Pickaxe)
            return Pickaxe();
        if (id == ItemIds.Wood)
            return Wood();
        if (id == ItemIds.Stone)
            return Stone();
        if (id == ItemIds.CopperOre)
            return CopperOre();
        if (id == ItemIds.Workbench)
            return Workbench();
        if (id == ItemIds.Chair)
            return Chair();
        if (id == ItemIds.Furnace)
            return Furnace();

        return null;
    }

    public static Sprite WorldSprite(string id)
    {
        if (id == ItemIds.Workbench)
            return Workbench();
        if (id == ItemIds.Chair)
            return Chair();
        if (id == ItemIds.Furnace)
            return Furnace();

        return null;
    }

    public static Sprite Axe()
    {
        if (_axe != null)
            return _axe;

        const int size = 40;
        var pixels = new Color32[size * size];
        var handle = new Color32(122, 74, 38, 255);
        var handleLit = new Color32(156, 102, 54, 255);
        var grip = new Color32(78, 46, 26, 255);
        var metal = new Color32(176, 184, 188, 255);
        var metalLit = new Color32(226, 230, 228, 255);
        var metalDark = new Color32(96, 104, 112, 255);

        // Handle points to the right. The head is centered on it, so a turn keeps the blade true.
        FillRect(pixels, size, 5, 18, 22, 4, handle);
        FillRect(pixels, size, 5, 18, 22, 1, handleLit);
        FillRect(pixels, size, 4, 17, 3, 6, grip);

        FillRect(pixels, size, 26, 16, 3, 8, metalDark);
        FillRect(pixels, size, 29, 8, 6, 24, metal);
        FillRect(pixels, size, 29, 8, 2, 24, metalLit);
        FillRect(pixels, size, 34, 11, 3, 18, metalDark);
        FillRect(pixels, size, 36, 14, 2, 12, metal);

        _axe = Finish(pixels, size, size, new Vector2(0.16f, 0.5f));
        return _axe;
    }

    public static Sprite Pickaxe()
    {
        if (_pickaxe != null)
            return _pickaxe;

        const int size = 40;
        var pixels = new Color32[size * size];
        var handle = new Color32(122, 74, 38, 255);
        var handleLit = new Color32(156, 102, 54, 255);
        var grip = new Color32(78, 46, 26, 255);
        var metal = new Color32(150, 168, 176, 255);
        var metalLit = new Color32(214, 226, 230, 255);
        var metalDark = new Color32(72, 86, 98, 255);

        FillRect(pixels, size, 5, 18, 24, 4, handle);
        FillRect(pixels, size, 5, 18, 24, 1, handleLit);
        FillRect(pixels, size, 4, 17, 3, 6, grip);
        FillRect(pixels, size, 26, 16, 4, 8, metalDark);
        FillRect(pixels, size, 29, 10, 3, 20, metal);
        FillRect(pixels, size, 29, 10, 1, 20, metalLit);
        FillRect(pixels, size, 30, 6, 2, 5, metalLit);
        FillRect(pixels, size, 31, 4, 1, 3, metal);
        FillRect(pixels, size, 30, 29, 2, 5, metalDark);
        FillRect(pixels, size, 31, 33, 1, 3, metalDark);

        _pickaxe = Finish(pixels, size, size, new Vector2(0.16f, 0.5f));
        return _pickaxe;
    }

    public static Sprite Stone()
    {
        if (_stone != null)
            return _stone;

        const int width = 26;
        const int height = 18;
        var pixels = new Color32[width * height];
        FillEllipse(pixels, width, height, 13, 8, 10, 6, new Color32(92, 90, 86, 255), 1);
        FillEllipse(pixels, width, height, 13, 9, 8, 4, new Color32(142, 140, 132, 255), 2);
        FillEllipse(pixels, width, height, 10, 11, 3, 2, new Color32(186, 184, 174, 255), 3);
        _stone = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        return _stone;
    }

    public static Sprite CopperOre()
    {
        if (_copperOre != null)
            return _copperOre;

        const int width = 26;
        const int height = 18;
        var pixels = new Color32[width * height];
        FillEllipse(pixels, width, height, 13, 8, 10, 6, new Color32(74, 64, 58, 255), 4);
        FillEllipse(pixels, width, height, 13, 9, 8, 4, new Color32(112, 96, 86, 255), 5);
        FillEllipse(pixels, width, height, 9, 10, 3, 2, new Color32(196, 112, 48, 255), 6);
        FillEllipse(pixels, width, height, 16, 8, 2, 2, new Color32(214, 146, 72, 255), 7);
        FillRect(pixels, width, 12, 11, 3, 2, new Color32(168, 86, 36, 255));
        _copperOre = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        return _copperOre;
    }

    public static Sprite Furnace()
    {
        if (_furnace != null)
            return _furnace;

        const int width = 48;
        const int height = 44;
        var pixels = new Color32[width * height];
        var outline = new Color32(62, 60, 58, 255);
        var stone = new Color32(132, 128, 120, 255);
        var stoneLit = new Color32(176, 172, 160, 255);
        var stoneDark = new Color32(78, 74, 70, 255);
        var mouth = new Color32(28, 22, 20, 255);
        var fire = new Color32(214, 96, 32, 255);
        var ember = new Color32(240, 176, 64, 255);

        FillRect(pixels, width, 8, 2, 6, 12, outline);
        FillRect(pixels, width, 9, 2, 4, 12, stoneDark);
        FillRect(pixels, width, 34, 2, 6, 12, outline);
        FillRect(pixels, width, 35, 2, 4, 12, stoneDark);

        FillRect(pixels, width, 6, 12, 36, 22, outline);
        FillRect(pixels, width, 8, 14, 32, 18, stone);
        FillRect(pixels, width, 8, 28, 32, 3, stoneLit);
        FillEllipse(pixels, width, height, 24, 22, 10, 6, mouth, 2);
        FillEllipse(pixels, width, height, 24, 21, 7, 4, fire, 3);
        FillEllipse(pixels, width, height, 24, 22, 3, 2, ember, 4);

        FillRect(pixels, width, 30, 30, 8, 10, outline);
        FillRect(pixels, width, 31, 31, 6, 8, stoneDark);
        FillRect(pixels, width, 32, 36, 4, 3, stoneLit);

        _furnace = Finish(pixels, width, height, new Vector2(0.5f, 0.08f));
        return _furnace;
    }

    public static Sprite Wood()
    {
        if (_wood != null)
            return _wood;

        const int width = 30;
        const int height = 16;
        var pixels = new Color32[width * height];
        var bark = new Color32(138, 86, 42, 255);
        var barkLit = new Color32(176, 118, 58, 255);
        var barkDark = new Color32(92, 54, 28, 255);
        var ring = new Color32(214, 176, 112, 255);
        var ringCore = new Color32(168, 122, 70, 255);

        FillRect(pixels, width, 4, 3, 22, 10, bark);
        FillRect(pixels, width, 4, 3, 22, 3, barkLit);
        FillRect(pixels, width, 4, 11, 22, 2, barkDark);
        FillEllipse(pixels, width, height, 5, 8, 4, 5, ring, 2);
        FillEllipse(pixels, width, height, 5, 8, 2, 2, ringCore, 3);
        FillEllipse(pixels, width, height, 25, 8, 4, 5, ring, 4);
        FillEllipse(pixels, width, height, 25, 8, 2, 2, ringCore, 5);

        _wood = Finish(pixels, width, height, new Vector2(0.5f, 0.5f));
        return _wood;
    }

    public static Sprite Workbench()
    {
        if (_workbench != null)
            return _workbench;

        const int width = 52;
        const int height = 46;
        var pixels = new Color32[width * height];
        var outline = new Color32(62, 36, 18, 255);
        var leg = new Color32(102, 62, 32, 255);
        var legLit = new Color32(156, 102, 56, 255);
        var apron = new Color32(118, 72, 36, 255);
        var top = new Color32(186, 128, 70, 255);
        var topLit = new Color32(222, 170, 102, 255);
        var metal = new Color32(176, 184, 190, 255);
        var metalDark = new Color32(96, 104, 112, 255);

        FillRect(pixels, width, 12, 2, 5, 16, outline);
        FillRect(pixels, width, 13, 2, 3, 16, leg);
        FillRect(pixels, width, 13, 2, 1, 16, legLit);
        FillRect(pixels, width, 35, 2, 5, 16, outline);
        FillRect(pixels, width, 36, 2, 3, 16, leg);
        FillRect(pixels, width, 36, 2, 1, 16, legLit);
        FillRect(pixels, width, 18, 8, 4, 10, leg);
        FillRect(pixels, width, 31, 8, 4, 10, leg);

        FillRect(pixels, width, 8, 16, 36, 7, outline);
        FillRect(pixels, width, 10, 17, 32, 5, apron);
        FillRect(pixels, width, 10, 20, 32, 1, legLit);
        FillRect(pixels, width, 14, 12, 24, 3, outline);

        FillEllipse(pixels, width, height, 26, 27, 20, 8, outline, 1);
        FillEllipse(pixels, width, height, 26, 29, 18, 6, top, 2);
        FillEllipse(pixels, width, height, 24, 31, 12, 3, topLit, 3);
        FillRect(pixels, width, 30, 29, 9, 3, metal);
        FillRect(pixels, width, 30, 31, 9, 1, metalDark);
        FillRect(pixels, width, 33, 26, 2, 6, leg);

        _workbench = Finish(pixels, width, height, new Vector2(0.5f, 0.06f));
        return _workbench;
    }

    public static Sprite Chair()
    {
        if (_chair != null)
            return _chair;

        const int width = 36;
        const int height = 48;
        var pixels = new Color32[width * height];
        var outline = new Color32(62, 36, 18, 255);
        var wood = new Color32(168, 112, 60, 255);
        var lit = new Color32(214, 158, 92, 255);
        var seat = new Color32(186, 128, 70, 255);
        var seatLit = new Color32(214, 164, 102, 255);

        FillRect(pixels, width, 7, 2, 4, 40, outline);
        FillRect(pixels, width, 8, 2, 2, 40, wood);
        FillRect(pixels, width, 8, 2, 1, 40, lit);
        FillRect(pixels, width, 25, 2, 4, 40, outline);
        FillRect(pixels, width, 26, 2, 2, 40, wood);
        FillRect(pixels, width, 26, 2, 1, 40, lit);

        FillRect(pixels, width, 7, 36, 22, 5, outline);
        FillRect(pixels, width, 8, 37, 20, 3, lit);
        FillRect(pixels, width, 7, 29, 22, 4, outline);
        FillRect(pixels, width, 8, 30, 20, 2, wood);

        FillEllipse(pixels, width, height, 18, 20, 14, 6, outline, 4);
        FillEllipse(pixels, width, height, 18, 21, 12, 4, seat, 5);
        FillEllipse(pixels, width, height, 16, 23, 8, 2, seatLit, 6);

        FillRect(pixels, width, 8, 2, 3, 12, outline);
        FillRect(pixels, width, 25, 2, 3, 12, outline);
        FillRect(pixels, width, 11, 8, 14, 2, wood);

        _chair = Finish(pixels, width, height, new Vector2(0.5f, 0.05f));
        return _chair;
    }

    public static Sprite Shadow()
    {
        if (_shadow != null)
            return _shadow;

        const int size = 16;
        var pixels = new Color32[size * size];
        FillEllipse(pixels, size, size, 8, 8, 7, 4, new Color32(20, 28, 16, 140), 0);
        _shadow = Finish(pixels, size, size, new Vector2(0.5f, 0.5f));
        return _shadow;
    }

    static void FillRect(Color32[] pixels, int width, int x, int y, int w, int h, Color32 color)
    {
        for (int py = y; py < y + h; py++)
        {
            for (int px = x; px < x + w; px++)
            {
                if ((uint)px >= (uint)width || py < 0 || py * width >= pixels.Length)
                    continue;
                pixels[py * width + px] = color;
            }
        }
    }

    static void FillEllipse(Color32[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color32 color, int salt)
    {
        if (rx < 1 || ry < 1)
            return;

        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                    continue;

                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy > 1f)
                    continue;

                Color32 write = color;
                if (color.a == 255)
                {
                    int hash = (x * 17 + y * 31 + salt * 13) & 7;
                    if (hash == 0)
                        write = Scale(color, 0.78f);
                    else if (hash == 1)
                        write = Scale(color, 1.08f);
                }

                pixels[y * width + x] = write;
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
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, PixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
