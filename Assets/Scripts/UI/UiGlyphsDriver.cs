using UnityEngine;

/// <summary>
/// Keeps the font atlas on a smooth filter after the canvas scale changes.
/// It does not rebuild labels and it does not search the scene.
/// </summary>
public class UiGlyphsDriver : MonoBehaviour
{
    Canvas _canvas;
    float _scale = -1f;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    void LateUpdate()
    {
        if (_canvas == null)
            return;

        float scale = _canvas.scaleFactor;
        if (scale < 0.01f || Mathf.Abs(scale - _scale) < 0.02f)
            return;

        _scale = scale;
        UiGlyphs.KeepCrisp();
    }
}
