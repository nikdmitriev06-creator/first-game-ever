using UnityEngine;

/// <summary>
/// Built-in materials. Wood, furniture, and stone share definitions and keep separate integrity on each body.
/// </summary>
public static class MaterialLibrary
{
    public static MaterialDefinition Wood { get; private set; }
    public static MaterialDefinition Furniture { get; private set; }
    public static MaterialDefinition Stone { get; private set; }
    public static MaterialDefinition Creature { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Wood = null;
        Furniture = null;
        Stone = null;
        Creature = null;
    }

    public static void Ensure()
    {
        ImpactLibrary.Ensure();
        if (Wood != null && Furniture != null && Stone != null && Creature != null)
            return;

        MaterialDefinition[] loaded = Resources.LoadAll<MaterialDefinition>("GameContent");
        Wood = Find(loaded, "wood") ?? Make("wood", ChopOnly(), new Color(0.93f, 0.78f, 0.46f, 1f));
        Furniture = Find(loaded, "furniture") ?? Make("furniture", ChopAndSmash(), new Color(0.86f, 0.62f, 0.32f, 1f));
        Stone = Find(loaded, "stone") ?? Make("stone", MineAndSmash(), new Color(0.75f, 0.76f, 0.78f, 1f));
        Creature = Find(loaded, "creature") ?? Make("creature", SlashOnly(), new Color(0.55f, 0.75f, 0.38f, 1f));
    }

    public static MaterialDefinition Named(string id)
    {
        Ensure();
        if (id == "wood")
            return Wood;
        if (id == "furniture")
            return Furniture;
        if (id == "stone")
            return Stone;
        if (id == "creature")
            return Creature;

        return null;
    }

    public static MaterialDefinition ForPlaceable(ItemDefinition item)
    {
        Ensure();
        if (item != null && item.BreakMaterial != null)
            return item.BreakMaterial;

        return Furniture;
    }

    static MaterialDefinition Find(MaterialDefinition[] loaded, string id)
    {
        if (loaded == null)
            return null;

        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].Id == id)
                return loaded[i];
        }

        return null;
    }

    static MaterialDefinition Make(string id, ImpactReaction[] reactions, Color debris)
    {
        MaterialDefinition material = ScriptableObject.CreateInstance<MaterialDefinition>();
        material.Configure(id, reactions, debris);
        material.hideFlags = HideFlags.HideAndDontSave;
        return material;
    }

    static ImpactReaction[] ChopOnly()
    {
        return new[] { new ImpactReaction { Type = ImpactLibrary.Chop, Multiplier = 1f } };
    }

    static ImpactReaction[] ChopAndSmash()
    {
        return new[]
        {
            new ImpactReaction { Type = ImpactLibrary.Chop, Multiplier = 1f },
            new ImpactReaction { Type = ImpactLibrary.Smash, Multiplier = 1f }
        };
    }

    static ImpactReaction[] MineAndSmash()
    {
        return new[]
        {
            new ImpactReaction { Type = ImpactLibrary.Mine, Multiplier = 1f },
            new ImpactReaction { Type = ImpactLibrary.Smash, Multiplier = 1f }
        };
    }

    static ImpactReaction[] SlashOnly()
    {
        return new[] { new ImpactReaction { Type = ImpactLibrary.Slash, Multiplier = 1f } };
    }
}
