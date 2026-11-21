using UnityEngine;

/// <summary>
/// Pixel icons for items that are not a painted weapon sheet.
/// The sword and the bow keep the sprite on their own renderer.
/// </summary>
public static class ItemArt
{
    const float PixelsPerUnit = 32f;

    static Sprite _axe;
    static Sprite _wood;
    static Sprite _shadow;

    public static Sprite Icon(string id)
    {
        if (id == ItemIds.Axe)
            return Axe();
        if (id == ItemIds.Wood)
            return Wood();

        return GearIcon(id);
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

    static Sprite GearIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
        if (inventory == null)
            return null;

        IWeapon[] weapons = inventory.GetComponentsInChildren<IWeapon>(true);
        for (int i = 0; i < weapons.Length; i++)
        {
            IWeapon weapon = weapons[i];
            if (weapon != null && weapon.ItemId == id)
                return weapon.IconSprite;
        }

        Tool[] tools = inventory.GetComponentsInChildren<Tool>(true);
        for (int i = 0; i < tools.Length; i++)
        {
            Tool tool = tools[i];
            if (tool != null && tool.ItemId == id)
                return tool.IconSprite;
        }

        return null;
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
