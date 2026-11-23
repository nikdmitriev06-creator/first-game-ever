using System;
using TMPro;
using UnityEngine;

/// <summary>
/// One face for every label. Pixel art stays on the sprites.
/// The letters are a signed-distance field, so the canvas scale does not pixelate them.
/// Builtin and older system fonts may lack Cyrillic. <see cref="Pick"/> then uses English.
/// Replace the face by changing <see cref="Source"/> — every label reads it from here.
/// </summary>
public static class UiGlyphs
{
    const int SampleSize = 90;

    static Font _font;
    static TMP_FontAsset _face;
    static Material _outline;
    static bool _looked;
    static bool _runtimeFace;
    static bool _warned;

    public enum Wrap
    {
        Single,
        Ellipsis,
        Paragraph
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _font = null;
        _face = null;
        _outline = null;
        _looked = false;
        _runtimeFace = false;
        _warned = false;
    }

    public static Font Source
    {
        get
        {
            if (_font == null)
                _font = OpenSource();
            return _font;
        }
    }

    public static Font Font => Source;

    public static TMP_FontAsset Face
    {
        get
        {
            if (!_looked)
            {
                _looked = true;
                _face = OpenFace();
            }

            return _face;
        }
    }

    public static void KeepCrisp()
    {
        if (_face == null)
            return;

        Texture2D[] atlases = _face.atlasTextures;
        if (atlases == null)
            return;

        for (int i = 0; i < atlases.Length; i++)
        {
            if (atlases[i] != null && atlases[i].filterMode != FilterMode.Bilinear)
                atlases[i].filterMode = FilterMode.Bilinear;
        }
    }

    public static void Fit(Canvas canvas)
    {
        UiScreen.Apply(canvas);
        if (canvas != null && canvas.GetComponent<UiGlyphsDriver>() == null)
            canvas.gameObject.AddComponent<UiGlyphsDriver>();
    }

    public static TMP_Text Place(GameObject host, string value, float size, TextAnchor anchor, Color color, Wrap wrap = Wrap.Single, bool outline = false, bool autoFit = false)
    {
        if (host == null)
            return null;

        TextMeshProUGUI text = host.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = host.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset face = Face;
        if (face != null)
            text.font = face;

        text.fontSize = Mathf.Max(1f, size);
        text.alignment = ToAlignment(anchor);
        text.color = color;
        text.text = value ?? string.Empty;
        text.raycastTarget = false;
        text.richText = false;
        text.enableAutoSizing = autoFit;
        if (autoFit)
        {
            text.fontSizeMin = Mathf.Max(10f, size * 0.72f);
            text.fontSizeMax = Mathf.Max(text.fontSizeMin, size);
        }

        if (wrap == Wrap.Paragraph)
        {
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }
        else if (wrap == Wrap.Ellipsis)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
        else
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        text.extraPadding = outline;
        if (outline)
        {
            Material material = OutlineMaterial();
            if (material != null)
                text.fontSharedMaterial = material;
        }

        Prepare(text, text.text);
        return text;
    }

    public static void Prepare(TMP_Text text, string sample)
    {
        if (text == null)
            return;

        TMP_FontAsset face = Face;
        if (face != null && text.font != face)
            text.font = face;

        if (face != null && !string.IsNullOrEmpty(sample))
            face.TryAddCharacters(sample, false);

        KeepCrisp();
    }

    public static string Pick(string russian, string english)
    {
        if (string.IsNullOrEmpty(russian))
            return english ?? string.Empty;

        return Supports(russian) || string.IsNullOrEmpty(english) ? russian : english;
    }

    public static bool Supports(string sample)
    {
        if (string.IsNullOrEmpty(sample))
            return false;

        if (Face == null)
            return false;

        if (!_runtimeFace)
        {
            for (int i = 0; i < sample.Length; i++)
            {
                char c = sample[i];
                if (c > 127 && c != '—' && c != '×' && c != '–')
                    return false;
            }

            return true;
        }

        Font font = Source;
        if (font == null)
            return false;

        for (int i = 0; i < sample.Length; i++)
        {
            char c = sample[i];
            if (c == ' ' || (c >= '0' && c <= '9') || c == '—' || c == '-' || c == '\n')
                continue;
            if (!font.HasCharacter(c))
                return false;
        }

        return true;
    }

    static Font OpenSource()
    {
        Font os = TrySystem("Segoe UI");
        if (os != null && os.HasCharacter('Я'))
            return os;

        Font arial = TrySystem("Arial");
        if (arial != null && arial.HasCharacter('Я'))
            return arial;

        Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtin == null)
            builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return os != null ? os : builtin != null ? builtin : arial;
    }

    static Font TrySystem(string name)
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(name, SampleSize);
        }
        catch
        {
            return null;
        }
    }

    static TMP_FontAsset OpenFace()
    {
        Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (shader == null)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogError("TextMesh Pro essentials are missing. Use Tools/UI/Import TMP Essentials, then play again.");
            }

            return null;
        }

        TMP_FontAsset face = TryFamily("Segoe UI");
        if (face == null)
            face = TryFamily("Arial");
        if (face != null)
        {
            _runtimeFace = true;
            face.name = "Ui Face";
            face.hideFlags = HideFlags.HideAndDontSave;
            if (face.material != null)
                face.material.hideFlags = HideFlags.HideAndDontSave;

            const string warmup = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюяABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 —×.,:!?-+/[]()";
            face.TryAddCharacters(warmup, false);
            KeepCrisp();
            return face;
        }

        if (TMP_Settings.instance != null)
            return TMP_Settings.defaultFontAsset;

        return null;
    }

    static TMP_FontAsset TryFamily(string family)
    {
        try
        {
            return TMP_FontAsset.CreateFontAsset(family, "Regular", SampleSize);
        }
        catch (Exception error)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("UI font " + family + " was not created. " + error.Message);
            }

            return null;
        }
    }

    static Material OutlineMaterial()
    {
        if (_outline != null)
            return _outline;

        TMP_FontAsset face = Face;
        if (face == null || face.material == null)
            return null;

        _outline = new Material(face.material);
        _outline.name = "Ui Face Outline";
        _outline.hideFlags = HideFlags.HideAndDontSave;
        _outline.SetFloat("_OutlineWidth", 0.14f);
        _outline.SetColor("_OutlineColor", new Color(0.06f, 0.04f, 0.03f, 0.92f));
        return _outline;
    }

    static TextAlignmentOptions ToAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}
