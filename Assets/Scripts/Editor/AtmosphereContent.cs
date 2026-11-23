using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes biome, VFX and sound profiles the inspector can edit.
/// Existing assets are left alone. Clips stay empty until a real file is dropped in.
/// </summary>
public static class AtmosphereContent
{
    const string Biomes = "Assets/Resources/Biomes";
    const string Vfx = "Assets/Resources/VFX";
    const string Audio = "Assets/Resources/Audio";

    [MenuItem("Game/Ensure Atmosphere Content")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureFolder("Assets/Resources");
        EnsureFolder(Biomes);
        EnsureFolder(Vfx);
        EnsureFolder(Audio);

        WriteBiome("forest", new Color(0.16f, 0.24f, 0.14f), 1f, true, true, false, true, false, false, false, true,
            Match("forest", "grass"),
            "Assets/Data/Levels/Pools/ForestResources.asset",
            "Assets/Data/Levels/Pools/ForestEnemies.asset");
        WriteBiome("cave", new Color(0.07f, 0.08f, 0.1f), 0.45f, false, false, false, false, true, false, true, false,
            Match("wall", "stone"), null, null);
        WriteBiome("rocky", new Color(0.28f, 0.26f, 0.22f), 1.05f, false, false, false, true, false, false, false, false,
            Match("rock", "cliff", "slope"), null, null);
        WriteBiome("swamp", new Color(0.12f, 0.16f, 0.1f), 0.75f, false, true, true, false, false, true, false, true,
            Match("mud", "water"), null, null);
        WriteBiome("ruins", new Color(0.2f, 0.16f, 0.14f), 0.85f, true, false, true, false, false, false, false, false,
            Match("road.stone", "sand", "wall"), null, null);

        WriteFx();
        WriteSound();
        WriteMaterial("WaterSprite", "Game/WaterSprite");
        WriteMaterial("SwaySprite", "Game/SwaySprite");
        AssetDatabase.SaveAssets();
    }

    static void WriteBiome(
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
        bool critters,
        TerrainDefinition[] terrains,
        string resourcesPath,
        string creaturesPath)
    {
        string path = Biomes + "/Biome_" + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<BiomeProfile>(path) != null)
            return;

        BiomeProfile profile = ScriptableObject.CreateInstance<BiomeProfile>();
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
        profile.Terrains = terrains ?? System.Array.Empty<TerrainDefinition>();
        profile.Decor = System.Array.Empty<GameObject>();
        profile.Vegetation = System.Array.Empty<GameObject>();
        if (!string.IsNullOrEmpty(resourcesPath))
            profile.Resources = AssetDatabase.LoadAssetAtPath<SpawnPoolDefinition>(resourcesPath);
        if (!string.IsNullOrEmpty(creaturesPath))
            profile.Creatures = AssetDatabase.LoadAssetAtPath<SpawnPoolDefinition>(creaturesPath);
        AssetDatabase.CreateAsset(profile, path);
    }

    static TerrainDefinition[] Match(params string[] needles)
    {
        string[] guids = AssetDatabase.FindAssets("t:TerrainDefinition", new[] { "Assets/Data/Levels/Terrains" });
        var found = new List<TerrainDefinition>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TerrainDefinition terrain = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(path);
            if (terrain == null || string.IsNullOrEmpty(terrain.Id))
                continue;

            string id = terrain.Id.ToLowerInvariant();
            for (int n = 0; n < needles.Length; n++)
            {
                if (id.IndexOf(needles[n], System.StringComparison.Ordinal) < 0)
                    continue;

                found.Add(terrain);
                break;
            }
        }

        return found.ToArray();
    }

    static void WriteFx()
    {
        const string path = Vfx + "/FxProfile.asset";
        if (AssetDatabase.LoadAssetAtPath<FxProfile>(path) != null)
            return;

        FxProfile profile = ScriptableObject.CreateInstance<FxProfile>();
        Fx.FillDefaults(profile);
        AssetDatabase.CreateAsset(profile, path);
    }

    static void WriteSound()
    {
        const string path = Audio + "/SoundProfile.asset";
        if (AssetDatabase.LoadAssetAtPath<SoundProfile>(path) != null)
            return;

        SoundProfile profile = ScriptableObject.CreateInstance<SoundProfile>();
        profile.MusicId = "explore";
        profile.Cues = new[]
        {
            Cue(FxEvent.Step, 0.1f),
            Cue(FxEvent.Chop, 0.08f),
            Cue(FxEvent.Mine, 0.08f),
            Cue(FxEvent.Ore, 0.08f),
            Cue(FxEvent.Hit, 0.06f),
            Cue(FxEvent.Hurt, 0.08f),
            Cue(FxEvent.Critical, 0.08f),
            Cue(FxEvent.Bow, 0.08f),
            Cue(FxEvent.Crossbow, 0.08f),
            Cue(FxEvent.Magic, 0.08f),
            Cue(FxEvent.Bleed, 0.2f),
            Cue(FxEvent.Death, 0.2f),
            Cue(FxEvent.Chest, 0.12f),
            Cue(FxEvent.Pickup, 0.08f),
            Cue(FxEvent.Rare, 0.12f),
            Cue(FxEvent.Craft, 0.12f),
            Cue(FxEvent.Break, 0.08f),
            Cue(FxEvent.Water, 0.4f, SoundGroup.Ambient),
            Cue(FxEvent.AmbientSting, 4f, SoundGroup.Ambient)
        };
        AssetDatabase.CreateAsset(profile, path);
    }

    static SoundCue Cue(FxEvent ev, float cooldown)
    {
        return Cue(ev, cooldown, SoundGroup.Sfx);
    }

    static SoundCue Cue(FxEvent ev, float cooldown, SoundGroup group)
    {
        return new SoundCue
        {
            Event = ev,
            Group = group,
            Clips = System.Array.Empty<AudioClip>(),
            Cooldown = cooldown,
            Spatial = group == SoundGroup.Sfx,
            Volume = 1f
        };
    }

    static void WriteMaterial(string file, string shaderName)
    {
        string path = Vfx + "/" + file + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            return;

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
            return;

        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
