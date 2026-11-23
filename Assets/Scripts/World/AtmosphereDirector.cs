using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Optional clock for a future day and night. It does not move unless Cycle is turned on,
/// and it never asks the player to sleep, eat or drink.
/// </summary>
public static class Daylight
{
    public static float Night;
    public static bool Cycle;
    public static float DayLength = 480f;

    public static void Reset()
    {
        Night = 0f;
        Cycle = false;
        DayLength = 480f;
    }

    public static void Tick(float deltaTime)
    {
        if (!Cycle || DayLength <= 1f || deltaTime <= 0f)
            return;

        Night = Mathf.Repeat(Night + deltaTime / DayLength, 1f);
    }
}

/// <summary>
/// One place that tints the global 2D light and the camera void from the current biome.
/// Local lights register here so a handful of fires flicker in this tick, not in their own.
/// </summary>
public class AtmosphereDirector : MonoBehaviour
{
    public static AtmosphereDirector Active { get; private set; }

    public static float Motion = 1f;
    public static bool MotionEnabled = true;
    public static bool LightingEnabled = true;

    static readonly List<LocalLight> Lights = new List<LocalLight>();
    static readonly Color InteriorSky = new Color(0.07f, 0.045f, 0.03f, 1f);
    static readonly Color InteriorLamp = new Color(0.72f, 0.55f, 0.36f, 1f);

    Light2D _global;
    Camera _camera;
    Color _sky;
    Color _lamp;
    float _intensity = 1f;
    float _scan;
    string _biomeId = "forest";
    bool _hidden;
    bool _ready;
    bool _placed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        if (Active != null)
            DestroyImmediate(Active.gameObject);

        Active = null;
        Lights.Clear();
        Motion = 1f;
        MotionEnabled = true;
        LightingEnabled = true;
        Daylight.Reset();
    }

    public static void Ensure()
    {
        if (Active != null)
            return;

        var go = new GameObject("Atmosphere");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<AtmosphereDirector>();
    }

    public static void Register(LocalLight light)
    {
        if (light != null && !Lights.Contains(light))
            Lights.Add(light);
    }

    public static void Unregister(LocalLight light)
    {
        Lights.Remove(light);
    }

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;
        BiomeLibrary.Ensure();
    }

    void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    void LateUpdate()
    {
        float delta = Time.deltaTime;
        Daylight.Tick(delta);
        _scan -= delta;
        if (_scan <= 0f)
        {
            _scan = 0.5f;
            Refresh();
        }

        float night = Daylight.Night;
        Color sky;
        Color lamp;
        float intensity;
        float motion;
        if (_hidden || !LightingEnabled)
        {
            sky = InteriorSky;
            lamp = InteriorLamp;
            intensity = 0.55f;
            motion = 0f;
        }
        else
        {
            BiomeProfile biome = BiomeLibrary.Get(_biomeId);
            sky = biome.SkyAt(night);
            lamp = biome.LightAt(night);
            intensity = biome.LightIntensity(night);
            motion = MotionEnabled ? biome.Motion : 0f;
        }

        float blend = 1f - Mathf.Exp(-delta / 0.45f);
        if (!_ready)
        {
            _sky = sky;
            _lamp = lamp;
            _intensity = intensity;
            _ready = true;
            blend = 1f;
        }
        else
        {
            _sky = Color.Lerp(_sky, sky, blend);
            _lamp = Color.Lerp(_lamp, lamp, blend);
            _intensity = Mathf.Lerp(_intensity, intensity, blend);
        }

        ApplyCamera();
        ApplyGlobal();
        TickLights();
        Motion = motion;
        Shader.SetGlobalFloat("_EnvSway", motion);
        EnvironmentMotion.Enabled = motion > 0.001f;
        EnvironmentMotion.Intensity = motion;
        EnvironmentMotion.Tick(Time.time);
        SoundBus.Tick();
    }

    void Refresh()
    {
        string id = BiomeLibrary.Current(out bool hidden);
        if (_placed && id == _biomeId && hidden == _hidden)
            return;

        _placed = true;

        _biomeId = string.IsNullOrEmpty(id) ? "forest" : id;
        _hidden = hidden;
        if (_hidden)
        {
            SoundBus.SetAmbient(null);
            return;
        }

        BiomeProfile biome = BiomeLibrary.Get(_biomeId);
        SoundBus.SetAmbient(biome.AmbientId);
        SoundBus.PlayMusic(biome.MusicId);
    }

    void ApplyCamera()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        _camera.backgroundColor = _sky;
    }

    void ApplyGlobal()
    {
        if (_global == null)
            _global = FindGlobal();
        if (_global == null)
            return;

        _global.color = _lamp;
        _global.intensity = _intensity;
    }

    void TickLights()
    {
        float time = Time.time;
        for (int i = Lights.Count - 1; i >= 0; i--)
        {
            LocalLight light = Lights[i];
            if (light == null)
            {
                Lights.RemoveAt(i);
                continue;
            }

            light.Tick(time);
        }
    }

    static Light2D FindGlobal()
    {
        Light2D[] lights = Object.FindObjectsByType<Light2D>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].lightType == Light2D.LightType.Global)
                return lights[i];
        }

        var go = new GameObject("Global Light 2D");
        go.hideFlags = HideFlags.HideAndDontSave;
        Light2D created = go.AddComponent<Light2D>();
        created.lightType = Light2D.LightType.Global;
        created.intensity = 1f;
        return created;
    }
}
