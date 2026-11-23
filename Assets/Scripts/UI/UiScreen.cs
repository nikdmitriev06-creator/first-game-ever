using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space contract for every HUD canvas.
/// The world camera keeps its own pixel scale. These canvases do not.
///
/// Reference is 1920×1080. Scale With Screen Size uses Expand, so the canvas
/// is never smaller than that reference: 16:9 stays 1:1 in canvas units, and a
/// wider or taller window only adds space on the long side. Corner anchors then
/// keep their margins, and a layout that fits 1920×1080 fits the preview sizes.
///
/// Add a panel by parenting it to a canvas opened with <see cref="Mount"/> and
/// anchoring it to the corner or edge it belongs to. Do not place inventory,
/// hotbar, notices, or menus in world space. A bar that must sit on an enemy
/// stays a world sprite, not a child of these canvases.
/// </summary>
public static class UiScreen
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;
    public const float ReferencePixelsPerUnit = 100f;

    public static readonly Vector2Int[] PreviewResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3440, 1440),
        new Vector2Int(1024, 768),
        new Vector2Int(900, 700)
    };

    public static Canvas Mount(GameObject host, int sortingOrder)
    {
        if (host == null)
            return null;

        if (host.transform.parent != null)
            host.transform.SetParent(null, false);

        Canvas canvas = host.GetComponent<Canvas>();
        if (canvas == null)
            canvas = host.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = sortingOrder;
        canvas.overrideSorting = true;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        Apply(canvas);
        return canvas;
    }

    public static void Apply(Canvas canvas)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = ReferencePixelsPerUnit;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;

        if (canvas.GetComponent<UiGlyphsDriver>() == null)
            canvas.gameObject.AddComponent<UiGlyphsDriver>();
    }

    public static float ExpandScale(float screenWidth, float screenHeight)
    {
        if (screenWidth < 1f || screenHeight < 1f)
            return 1f;

        float width = screenWidth / ReferenceWidth;
        float height = screenHeight / ReferenceHeight;
        return Mathf.Min(width, height);
    }

    public static Vector2 CanvasUnits(float screenWidth, float screenHeight)
    {
        float scale = ExpandScale(screenWidth, screenHeight);
        if (scale < 0.0001f)
            return new Vector2(ReferenceWidth, ReferenceHeight);

        return new Vector2(screenWidth / scale, screenHeight / scale);
    }

    public static bool CoversReference(float screenWidth, float screenHeight)
    {
        Vector2 units = CanvasUnits(screenWidth, screenHeight);
        return units.x >= ReferenceWidth - 0.5f && units.y >= ReferenceHeight - 0.5f;
    }

    public static bool Fits(Vector2 canvasUnits, Vector2 element, float margin)
    {
        return element.x <= canvasUnits.x - margin * 2f && element.y <= canvasUnits.y - margin * 2f;
    }

    public static bool FitsAll(Vector2 element, float margin)
    {
        for (int i = 0; i < PreviewResolutions.Length; i++)
        {
            Vector2Int size = PreviewResolutions[i];
            if (!CoversReference(size.x, size.y))
                return false;
            if (!Fits(CanvasUnits(size.x, size.y), element, margin))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Top-left of a popup in screen pixels. Width and height are already in screen pixels.
    /// The box flips to the other side of the pointer when it would leave the screen.
    /// </summary>
    public static Vector2 TooltipTopLeft(Vector2 pointer, float width, float height, float screenWidth, float screenHeight, float margin, float gap)
    {
        float x = pointer.x + gap;
        float y = pointer.y;
        float maxX = screenWidth - margin;
        float maxY = screenHeight - margin;

        if (x + width > maxX)
            x = pointer.x - width - gap;
        if (x < margin)
            x = margin;
        if (x + width > maxX)
            x = Mathf.Max(margin, maxX - width);

        if (height >= screenHeight - margin * 2f)
            return new Vector2(x, maxY);

        if (y > maxY)
            y = maxY;
        if (y - height < margin)
            y = margin + height;
        if (y > maxY)
            y = maxY;

        return new Vector2(x, y);
    }
}
