using UnityEngine;

/// <summary>
/// Clears short-lived presentation when the player changes room, and maps a hit onto one effect.
/// Gameplay rules stay on the target. This only chooses a cue.
/// </summary>
public static class WorldPresentation
{
    public static void Warm()
    {
        BiomeLibrary.Ensure();
        Fx.Warm();
        SoundBus.Warm();
    }

    public static void ClearTransient()
    {
        Fx.ReleaseAll();
        DecorEvents.ClearActive();
        DamageNumbers.Clear();
    }

    public static FxEvent ForStrike(ImpactType type, MaterialDefinition material, LootTable loot)
    {
        ImpactLibrary.Ensure();
        if (type != null && type.Same(ImpactLibrary.Chop))
            return FxEvent.Chop;

        if (type != null && type.Same(ImpactLibrary.Mine))
            return OreTable(loot) ? FxEvent.Ore : FxEvent.Mine;

        if (material != null && material.Id == "wood")
            return FxEvent.Chop;

        if (type != null && type.Same(ImpactLibrary.Smash))
            return FxEvent.Break;

        return FxEvent.Hit;
    }

    public static bool OreTable(LootTable loot)
    {
        if (loot == null || string.IsNullOrEmpty(loot.Id))
            return false;

        string id = loot.Id.ToLowerInvariant();
        return id.Contains("ore") || id.Contains("copper");
    }
}
