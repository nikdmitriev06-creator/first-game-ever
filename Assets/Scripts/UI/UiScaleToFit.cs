using UnityEngine;

/// <summary>
/// Shrinks a screen-space panel until it fits its parent and an optional cap.
/// The scale is applied once per size change. It does not run a layout pass every frame.
/// </summary>
public class UiScaleToFit : MonoBehaviour
{
    public float Margin = 32f;
    public float MaxWidth;
    public float MaxHeight;
    public float MinScale = 0.62f;

    RectTransform _rect;

    void OnEnable()
    {
        Apply();
    }

    void LateUpdate()
    {
        if (Apply())
            enabled = false;
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    public bool Apply()
    {
        if (_rect == null)
            _rect = transform as RectTransform;
        if (_rect == null)
            return false;

        RectTransform parent = _rect.parent as RectTransform;
        if (parent == null || parent.rect.width < 2f || parent.rect.height < 2f)
            return false;

        float width = _rect.sizeDelta.x;
        float height = _rect.sizeDelta.y;
        if (width < 1f || height < 1f)
            return false;

        float limitW = MaxWidth > 1f ? Mathf.Min(MaxWidth, parent.rect.width - Margin * 2f) : parent.rect.width - Margin * 2f;
        float limitH = MaxHeight > 1f ? Mathf.Min(MaxHeight, parent.rect.height - Margin * 2f) : parent.rect.height - Margin * 2f;
        if (limitW < 1f || limitH < 1f)
            return false;

        float scale = Mathf.Min(1f, limitW / width, limitH / height);
        if (scale < MinScale && MinScale * width <= limitW + 0.5f && MinScale * height <= limitH + 0.5f)
            scale = MinScale;

        if (Mathf.Abs(_rect.localScale.x - scale) > 0.001f)
            _rect.localScale = new Vector3(scale, scale, 1f);

        return true;
    }
}
