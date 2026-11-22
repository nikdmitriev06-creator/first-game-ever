using UnityEngine;

/// <summary>
/// The impact kinds the game already understands. Loaded from Resources when those assets exist.
/// </summary>
public static class ImpactLibrary
{
    public static ImpactType Chop { get; private set; }
    public static ImpactType Mine { get; private set; }
    public static ImpactType Smash { get; private set; }
    public static ImpactType Slash { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Chop = null;
        Mine = null;
        Smash = null;
        Slash = null;
    }

    public static void Ensure()
    {
        if (Chop != null && Mine != null && Smash != null && Slash != null)
            return;

        ImpactType[] loaded = Resources.LoadAll<ImpactType>("GameContent");
        Chop = Find(loaded, "chop");
        Mine = Find(loaded, "mine");
        Smash = Find(loaded, "smash");
        Slash = Find(loaded, "slash");
    }

    static ImpactType Find(ImpactType[] loaded, string id)
    {
        if (loaded != null)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                ImpactType type = loaded[i];
                if (type != null && type.Id == id)
                    return type;
            }
        }

        ImpactType created = ScriptableObject.CreateInstance<ImpactType>();
        created.Configure(id);
        created.hideFlags = HideFlags.HideAndDontSave;
        return created;
    }
}
