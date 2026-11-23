using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode checks for presentation: effects, biomes, light data, sort and sound buses.
/// Run with Unity -executeMethod AtmosphereChecks.Run
/// </summary>
public static class AtmosphereChecks
{
    [MenuItem("Game/Проверить атмосферу")]
    public static void Run()
    {
        int failed = 0;
        try
        {
            AtmosphereContent.EnsureAssets();
            BiomeLibrary.ResetStatics();
            BiomeLibrary.Ensure();
            Fx.ResetStatics();
            SoundBus.ResetStatics();
            DecorEvents.ResetStatics();
            Daylight.Reset();
            CheckBiomes(ref failed);
            CheckEffects(ref failed);
            CheckSound(ref failed);
            CheckSort(ref failed);
            CheckEvents(ref failed);
            CheckDaylight(ref failed);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("Atmosphere check crashed: " + error);
        }
        finally
        {
            Fx.ResetStatics();
            SoundBus.ResetStatics();
            DecorEvents.ResetStatics();
            Daylight.Reset();
            BiomeLibrary.ResetStatics();
        }

        if (failed == 0)
            Debug.Log("Atmosphere checks passed.");
        else
            Debug.LogError("Atmosphere checks failed: " + failed);

        if (Application.isBatchMode)
            EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    static void CheckBiomes(ref int failed)
    {
        BiomeProfile forest = BiomeLibrary.Get("forest");
        BiomeProfile cave = BiomeLibrary.Get("cave");
        BiomeProfile rocky = BiomeLibrary.Get("rocky");
        BiomeProfile swamp = BiomeLibrary.Get("swamp");
        BiomeProfile ruins = BiomeLibrary.Get("ruins");
        Check(forest != null && cave != null && rocky != null && swamp != null && ruins != null, "five biomes resolve", ref failed);
        Check(cave.Brightness < forest.Brightness, "a cave is dimmer than a forest", ref failed);
        Check(rocky.Brightness > forest.Brightness, "rock is brighter than the forest", ref failed);
        Check(swamp.Water && swamp.Fireflies && swamp.Critters, "the swamp keeps water, flies and critters", ref failed);
        Check(cave.CaveDust && cave.Waterfall && !cave.Birds, "the cave keeps dust and a falls hook", ref failed);
        Check(ruins.Leaves && ruins.Fireflies, "ruins keep leaves and flies", ref failed);
        Check(forest.SkyAt(1f).maxColorComponent < forest.SkyAt(0f).maxColorComponent, "night sky is darker than day", ref failed);
        Check(forest.Terrains != null && forest.Terrains.Length > 0, "forest points at existing terrains", ref failed);
        Check(forest.Resources != null && forest.Creatures != null, "forest points at the existing pools", ref failed);
        Check(cave.Decor != null && cave.Vegetation != null, "empty decor hooks are arrays, not errors", ref failed);

        LootTable ore = ScriptableObject.CreateInstance<LootTable>();
        LootTable rock = ScriptableObject.CreateInstance<LootTable>();
        ore.Configure("loot.copper_vein", System.Array.Empty<LootDrop>());
        rock.Configure("loot.rock", System.Array.Empty<LootDrop>());
        ImpactLibrary.Ensure();
        Check(WorldPresentation.ForStrike(ImpactLibrary.Mine, null, ore) == FxEvent.Ore, "a copper table uses the ore cue", ref failed);
        Check(WorldPresentation.ForStrike(ImpactLibrary.Mine, null, rock) == FxEvent.Mine, "a rock table stays stone", ref failed);
        Check(WorldPresentation.ForStrike(ImpactLibrary.Chop, null, null) == FxEvent.Chop, "a chop stays wood", ref failed);
        Object.DestroyImmediate(ore);
        Object.DestroyImmediate(rock);
    }

    static void CheckEffects(ref int failed)
    {
        Color fire = Fx.RecipeFor(FxEvent.Hurt, DamageType.Fire).Color;
        Color cold = Fx.RecipeFor(FxEvent.Hurt, DamageType.Cold).Color;
        Check(fire != cold, "fire and cold hurts differ", ref failed);
        Check(Fx.RecipeFor(FxEvent.Magic, DamageType.None).Prefab == null, "magic has no fake prefab", ref failed);

        Fx.AllowInEditMode = true;
        Fx.Play(FxEvent.Hit, Vector3.zero, DamageType.Fire);
        Check(Fx.ActiveCount > 0, "a hit rents pooled bits", ref failed);
        int rented = Fx.ActiveCount;
        Fx.Play(FxEvent.Death, Vector3.zero);
        Check(Fx.ActiveCount > rented, "a second event adds bits instead of leaking a new system", ref failed);
        WorldPresentation.ClearTransient();
        Check(Fx.ActiveCount == 0, "leaving a room returns every bit", ref failed);
    }

    static void CheckSound(ref int failed)
    {
        SoundBus.SetVolume(SoundGroup.Master, 0.5f);
        SoundBus.SetVolume(SoundGroup.Sfx, 0.5f);
        SoundBus.SetVolume(SoundGroup.Music, 1f);
        SoundBus.SetVolume(SoundGroup.Ambient, 0.25f);
        Check(Mathf.Abs(SoundBus.Volume(SoundGroup.Sfx) - 0.25f) < 0.001f, "sfx is master times sfx", ref failed);
        Check(Mathf.Abs(SoundBus.Volume(SoundGroup.Music) - 0.5f) < 0.001f, "music ignores the sfx slider", ref failed);
        Check(Mathf.Abs(SoundBus.Volume(SoundGroup.Ambient) - 0.125f) < 0.001f, "ambient has its own slider", ref failed);
        SoundBus.Muted = true;
        Check(SoundBus.Volume(SoundGroup.Sfx) == 0f, "mute silences every lane", ref failed);
        SoundBus.Muted = false;
        Check(!SoundBus.HasClip(FxEvent.Bow), "a missing bow clip is silence", ref failed);
        SoundBus.AllowInEditMode = true;
        int before = SoundBus.PlayedCount;
        SoundBus.Play(FxEvent.Bow, Vector3.zero);
        Check(SoundBus.PlayedCount == before, "playing a missing clip does not count", ref failed);

        AudioClip tone = AudioClip.Create("sfx_hit_test", 32, 1, 8000, false);
        SoundBus.Register(FxEvent.Hit, tone);
        SoundBus.Register(FxEvent.Hit, tone);
        SoundBus.Play(FxEvent.Hit, Vector3.zero);
        SoundBus.Play(FxEvent.Hit, Vector3.zero);
        Check(SoundBus.PlayedCount == before + 1, "the same hit cannot stack inside its cooldown", ref failed);
        Check(SoundBus.GroupFor(FxEvent.Step) == SoundGroup.Sfx, "footsteps are sfx", ref failed);
        Check(SoundBus.GroupFor(FxEvent.Water) == SoundGroup.Ambient, "water is ambient", ref failed);
        Object.DestroyImmediate(tone);
    }

    static void CheckSort(ref int failed)
    {
        Check(IsoSort.Order(0f) > IsoSort.Order(2f), "a lower foot draws in front", ref failed);
        Check(IsoSort.Stable(1f, 1) != IsoSort.Stable(1f, 2), "two props on one line do not share an order", ref failed);
        Check(IsoSort.Order(-1f) <= IsoSort.ActorMax && IsoSort.Order(1f) >= IsoSort.ActorMin, "actors stay inside the sort band", ref failed);
        Check(IsoSort.FloorOrder < IsoSort.ActorMin, "the floor stays under every actor", ref failed);
    }

    static void CheckEvents(ref int failed)
    {
        DecorEvents.Enabled = true;
        DecorEvents.MaxAlive = 2;
        Check(DecorEvents.Admit(), "the first event is allowed", ref failed);
        Check(DecorEvents.Admit(), "the second event is allowed", ref failed);
        Check(!DecorEvents.Admit(), "a third event waits", ref failed);
        DecorEvents.ReleaseOne();
        Check(DecorEvents.Admit(), "a finished event frees a slot", ref failed);
        DecorEvents.Enabled = false;
        Check(!DecorEvents.Admit(), "events can be switched off", ref failed);
    }

    static void CheckDaylight(ref int failed)
    {
        Daylight.Reset();
        float night = Daylight.Night;
        Daylight.Tick(5f);
        Check(Mathf.Abs(Daylight.Night - night) < 0.0001f, "the clock stays still until cycle is enabled", ref failed);
        Daylight.Cycle = true;
        Daylight.DayLength = 10f;
        Daylight.Tick(5f);
        Check(Daylight.Night > 0.2f, "an enabled cycle advances night", ref failed);
    }

    static void Check(bool condition, string label, ref int failed)
    {
        if (condition)
            return;

        failed++;
        Debug.LogError("Atmosphere check failed: " + label);
    }
}
