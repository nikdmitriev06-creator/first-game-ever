using UnityEngine;

/// <summary>
/// Darkens a harvest body as its integrity falls. Real crack sprites can replace the tint later.
/// </summary>
public class BreakWear : MonoBehaviour
{
    SpriteRenderer _renderer;
    Destructible _body;
    Color _healthy = Color.white;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _body = GetComponent<Destructible>();
        if (_renderer != null)
            _healthy = _renderer.color;
    }

    void OnEnable()
    {
        if (_body == null)
            _body = GetComponent<Destructible>();
        if (_body != null)
            _body.IntegrityChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (_body != null)
            _body.IntegrityChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_renderer == null || _body == null || _body.MaxIntegrity <= 0f)
            return;

        float ratio = Mathf.Clamp01(_body.Integrity / _body.MaxIntegrity);
        Color worn = new Color(_healthy.r * 0.45f, _healthy.g * 0.4f, _healthy.b * 0.36f, _healthy.a);
        _renderer.color = Color.Lerp(worn, _healthy, ratio);
    }
}
