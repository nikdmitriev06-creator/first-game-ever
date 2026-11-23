using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A plant, grass tuft, or sheet of water. Motion is applied by one shared tick, not by this object's Update.
/// Characters should not use it.
/// </summary>
public class MotionSprite : MonoBehaviour
{
    public enum Kind
    {
        Grass,
        Leaves,
        Plant,
        Water,
        Waterfall
    }

    public Kind kind = Kind.Leaves;
    public float amount = 1f;

    Quaternion _base;
    float _phase;
    bool _posed;

    void OnEnable()
    {
        _base = transform.localRotation;
        _phase = (GetEntityId().GetHashCode() & 31) * 0.2f;
        _posed = true;
        if (kind == Kind.Water || kind == Kind.Waterfall)
            EnvironmentMotion.PaintWater(GetComponent<SpriteRenderer>(), kind == Kind.Waterfall);
        EnvironmentMotion.Register(this);
    }

    void OnDisable()
    {
        EnvironmentMotion.Unregister(this);
        if (_posed)
            transform.localRotation = _base;
    }

    public void Pose(float time, float sway)
    {
        if (kind == Kind.Water || kind == Kind.Waterfall)
            return;

        float degrees = kind == Kind.Grass ? 5f : kind == Kind.Plant ? 2.2f : 1.15f;
        float speed = kind == Kind.Grass ? 1.7f : 0.85f;
        float angle = Mathf.Sin(time * speed + _phase) * degrees * amount * sway;
        transform.localRotation = _base * Quaternion.Euler(0f, 0f, angle);
    }
}

/// <summary>
/// One list of swaying props and the shared water materials. Disabled motion restores the resting pose.
/// </summary>
public static class EnvironmentMotion
{
    static readonly List<MotionSprite> Live = new List<MotionSprite>();
    static Material _water;
    static Material _falls;

    public static float Intensity = 1f;
    public static bool Enabled = true;
    public static int ActiveCount => Live.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Live.Clear();
        _water = null;
        _falls = null;
        Intensity = 1f;
        Enabled = true;
    }

    public static void Register(MotionSprite sprite)
    {
        if (sprite != null && !Live.Contains(sprite))
            Live.Add(sprite);
    }

    public static void Unregister(MotionSprite sprite)
    {
        Live.Remove(sprite);
    }

    public static void Tick(float time)
    {
        float sway = Enabled ? Mathf.Clamp01(Intensity) : 0f;
        for (int i = Live.Count - 1; i >= 0; i--)
        {
            MotionSprite sprite = Live[i];
            if (sprite == null)
            {
                Live.RemoveAt(i);
                continue;
            }

            sprite.Pose(time, sway);
        }
    }

    public static void PaintWater(SpriteRenderer renderer, bool waterfall)
    {
        if (renderer == null)
            return;

        Material material = waterfall ? Falls() : Water();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static Material Water()
    {
        if (_water != null)
            return _water;

        _water = Make("Game/WaterSprite", 0.35f, 0.03f);
        return _water;
    }

    static Material Falls()
    {
        if (_falls != null)
            return _falls;

        _falls = Make("Game/WaterSprite", 1.4f, 0.02f);
        return _falls;
    }

    static Material Make(string shaderName, float speed, float sway)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
            return null;

        var material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetFloat("_Speed", speed);
        material.SetFloat("_Sway", sway);
        return material;
    }
}
