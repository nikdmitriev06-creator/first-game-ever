using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One regular font for every label. Glyphs stay at the size set on the Text
/// component and are filtered smoothly, so they do not follow the 32px art grid
/// or get redrawn as chunky game pixels when the window changes size.
/// Builtin fonts on some machines have no Cyrillic, so labels fall back to English.
/// </summary>
public static class UiGlyphs
{
    static Font _font;
    static bool _hooked;

    public static Font Font
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Hook(_font);
            return _font;
        }
    }

    /// <summary>
    /// Rasterize <paramref name="sample"/> at the label's real point size and keep
    /// the font texture smooth. Call this after assigning font and fontSize.
    /// </summary>
    public static void Prepare(Text text, string sample)
    {
        if (text == null)
            return;

        if (text.font == null)
            text.font = Font;

        Hook(text.font);
        if (text.font != null && !string.IsNullOrEmpty(sample))
            text.font.RequestCharactersInTexture(sample, Mathf.Max(1, text.fontSize), text.fontStyle);

        Smooth(text.font);
    }

    public static string Pick(string russian, string english)
    {
        if (string.IsNullOrEmpty(russian))
            return english ?? string.Empty;

        return Supports(russian) || string.IsNullOrEmpty(english) ? russian : english;
    }

    public static bool Supports(string sample)
    {
        Font font = Font;
        if (font == null || string.IsNullOrEmpty(sample))
            return false;

        font.RequestCharactersInTexture(sample, 64, FontStyle.Normal);
        Smooth(font);
        for (int i = 0; i < sample.Length; i++)
        {
            if (!font.HasCharacter(sample[i]))
                return false;
        }

        return true;
    }

    static void Hook(Font font)
    {
        Smooth(font);
        if (_hooked)
            return;

        _hooked = true;
        UnityEngine.Font.textureRebuilt += Smooth;
    }

    static void Smooth(Font font)
    {
        if (font == null)
            return;

        Material material = font.material;
        if (material != null && material.mainTexture != null)
            material.mainTexture.filterMode = FilterMode.Bilinear;
    }
}
