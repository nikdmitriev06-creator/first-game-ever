using UnityEngine;

/// <summary>
/// Builtin fonts on some machines have no Cyrillic, so labels fall back to English.
/// </summary>
public static class UiGlyphs
{
    static Font _font;

    public static Font Font
    {
        get
        {
            if (_font != null)
                return _font;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return _font;
        }
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
        for (int i = 0; i < sample.Length; i++)
        {
            if (!font.HasCharacter(sample[i]))
                return false;
        }

        return true;
    }
}
