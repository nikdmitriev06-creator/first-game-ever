using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A point light for a fire, a torch or a magical prop. It does not change sprite sorting.
/// The director ticks the flicker so each light does not run its own Update.
/// </summary>
public class LocalLight : MonoBehaviour
{
    public enum Role
    {
        Fire,
        Torch,
        Magic
    }

    [SerializeField] Role role = Role.Fire;
    [SerializeField] Color color = new Color(1f, 0.58f, 0.24f, 1f);
    [SerializeField] float radius = 3.2f;
    [SerializeField] float intensity = 0.85f;
    [SerializeField] bool flicker = true;

    Light2D _light;
    float _phase;

    public bool Live { get; private set; } = true;

    public void Configure(Role next, Color tint, float reach, float power, bool pulse)
    {
        role = next;
        color = tint;
        radius = reach;
        intensity = power;
        flicker = pulse;
        Apply();
    }

    void Awake()
    {
        _phase = Random.Range(0f, 6.28f);
        Apply();
    }

    void OnEnable()
    {
        AtmosphereDirector.Register(this);
    }

    void OnDisable()
    {
        AtmosphereDirector.Unregister(this);
    }

    public void SetLive(bool value)
    {
        Live = value;
        if (_light != null)
            _light.enabled = value && Application.isPlaying;
    }

    public void Tick(float time)
    {
        if (_light == null || !Live)
            return;

        float pulse = flicker ? 0.84f + 0.16f * Mathf.Sin(time * (role == Role.Torch ? 9f : 6.5f) + _phase) : 1f;
        _light.intensity = intensity * pulse;
    }

    void Apply()
    {
        if (!Application.isPlaying)
            return;

        _light = GetComponent<Light2D>();
        if (_light == null)
            _light = gameObject.AddComponent<Light2D>();

        _light.lightType = Light2D.LightType.Point;
        _light.blendStyleIndex = 1;
        _light.color = color;
        _light.intensity = intensity;
        _light.pointLightInnerRadius = radius * 0.28f;
        _light.pointLightOuterRadius = radius;
        _light.falloffIntensity = 0.6f;
        _light.enabled = Live;
    }
}
