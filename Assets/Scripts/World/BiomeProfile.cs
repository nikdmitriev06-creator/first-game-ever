using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Look, light, sound and content hooks for one region.
/// Tiles, props and pools are references. A new biome is another asset, not another class.
/// Empty references mean the artist has not delivered that part yet.
/// </summary>
[CreateAssetMenu(fileName = "Biome", menuName = "Game/Biome")]
public class BiomeProfile : ScriptableObject
{
    public string Id = "forest";
    public Color Sky = new Color(0.16f, 0.22f, 0.14f, 1f);
    public Color NightSky = new Color(0.05f, 0.07f, 0.1f, 1f);
    public Color Tint = Color.white;
    public Color NightTint = new Color(0.55f, 0.62f, 0.85f, 1f);
    public float Brightness = 1f;
    public float NightBrightness = 0.35f;
    public Color Glow = new Color(1f, 0.72f, 0.35f, 0.35f);
    public float TransitionSeconds = 1.3f;
    public bool Leaves = true;
    public bool Grass = true;
    public bool Fireflies;
    public bool Birds = true;
    public bool CaveDust;
    public bool Water;
    public bool Waterfall;
    public bool Critters;
    public bool Events = true;
    public float Motion = 1f;
    public string AmbientId;
    public string MusicId;
    public TerrainDefinition[] Terrains = System.Array.Empty<TerrainDefinition>();
    public GameObject[] Decor = System.Array.Empty<GameObject>();
    public GameObject[] Vegetation = System.Array.Empty<GameObject>();
    public SpawnPoolDefinition Resources;
    public SpawnPoolDefinition Creatures;

    public Color SkyAt(float night)
    {
        night = Mathf.Clamp01(night);
        Color sky = Color.Lerp(Sky, NightSky, night);
        float lamp = Mathf.Lerp(Brightness, NightBrightness, night);
        sky *= Mathf.Clamp(lamp, 0.15f, 2f);
        sky.a = 1f;
        return sky;
    }

    public Color LightAt(float night)
    {
        night = Mathf.Clamp01(night);
        Color color = Color.Lerp(Tint, NightTint, night);
        color.a = 1f;
        return color;
    }

    public float LightIntensity(float night)
    {
        night = Mathf.Clamp01(night);
        return Mathf.Clamp(Mathf.Lerp(Brightness, NightBrightness, night), 0.2f, 2f);
    }
}

public static class BiomeLibrary
{
    static readonly Dictionary<string, BiomeProfile> Built = new Dictionary<string, BiomeProfile>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        foreach (var pair in Built)
        {
            if (pair.Value != null && (pair.Value.hideFlags & HideFlags.HideAndDontSave) != 0)
                Object.DestroyImmediate(pair.Value);
        }

        Built.Clear();
    }

    public static string Current(out bool hidden)
    {
        hidden = false;
        LocationContext place = LocationContext.Active;
        if (place == null)
            return "forest";

        if (!string.IsNullOrEmpty(place.BiomeId))
            return place.BiomeId;

        RoomDocument room = place.GetComponent<RoomDocument>();
        if (room != null)
        {
            if (room.Definition != null && !string.IsNullOrEmpty(room.Definition.BiomeId))
                return room.Definition.BiomeId;
            if (room.Level != null && !string.IsNullOrEmpty(room.Level.BiomeId))
                return room.Level.BiomeId;
        }

        if (place.Kind == LocationKind.Indoor)
        {
            hidden = true;
            return null;
        }

        return "forest";
    }

    public static BiomeProfile Get(string id)
    {
        Ensure();
        BiomeProfile profile;
        if (!string.IsNullOrEmpty(id) && Built.TryGetValue(id, out profile))
            return profile;

        return Built["forest"];
    }

    public static void Ensure()
    {
        if (Built.Count > 0)
            return;

        Add("forest", new Color(0.16f, 0.24f, 0.14f), 1f, true, true, false, true, false, false, false, true);
        Add("cave", new Color(0.07f, 0.08f, 0.1f), 0.45f, false, false, false, false, true, false, true, false);
        Add("rocky", new Color(0.28f, 0.26f, 0.22f), 1.05f, false, false, false, true, false, false, false, false);
        Add("swamp", new Color(0.12f, 0.16f, 0.1f), 0.75f, false, true, true, false, false, true, false, true);
        Add("ruins", new Color(0.2f, 0.16f, 0.14f), 0.85f, true, false, true, false, false, false, false, false);

        BiomeProfile[] loaded = Resources.LoadAll<BiomeProfile>("Biomes");
        if (loaded == null)
            return;

        for (int i = 0; i < loaded.Length; i++)
        {
            BiomeProfile asset = loaded[i];
            if (asset == null || string.IsNullOrEmpty(asset.Id))
                continue;

            BiomeProfile previous;
            if (Built.TryGetValue(asset.Id, out previous) && previous != null && previous != asset
                && (previous.hideFlags & HideFlags.HideAndDontSave) != 0)
                Object.DestroyImmediate(previous);

            Built[asset.Id] = asset;
        }
    }

    static void Add(
        string id,
        Color sky,
        float brightness,
        bool leaves,
        bool grass,
        bool fireflies,
        bool birds,
        bool dust,
        bool water,
        bool waterfall,
        bool critters)
    {
        BiomeProfile profile = ScriptableObject.CreateInstance<BiomeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        profile.Id = id;
        profile.Sky = sky;
        profile.NightSky = sky * 0.35f;
        profile.NightSky.a = 1f;
        profile.Brightness = brightness;
        profile.NightBrightness = Mathf.Clamp(brightness * 0.4f, 0.2f, 0.7f);
        profile.Tint = Color.Lerp(Color.white, sky, 0.35f);
        profile.Tint.a = 1f;
        profile.NightTint = new Color(0.5f, 0.58f, 0.82f, 1f);
        profile.Leaves = leaves;
        profile.Grass = grass;
        profile.Fireflies = fireflies;
        profile.Birds = birds;
        profile.CaveDust = dust;
        profile.Water = water;
        profile.Waterfall = waterfall;
        profile.Critters = critters;
        profile.Events = true;
        profile.Motion = dust ? 0.35f : 1f;
        profile.AmbientId = id;
        profile.MusicId = id;
        Built[id] = profile;
    }
}
