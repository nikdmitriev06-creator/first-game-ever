using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime lookup for modifier assets. Gameplay registers the assets it loaded.
/// The library does not own item state.
/// </summary>
public static class ModifierLibrary
{
    static readonly List<ModifierDefinition> Known = new List<ModifierDefinition>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Known.Clear();
    }

    public static void Clear()
    {
        Known.Clear();
    }

    public static void Register(ModifierDefinition modifier)
    {
        if (modifier == null || string.IsNullOrEmpty(modifier.Id))
            return;

        for (int i = 0; i < Known.Count; i++)
        {
            if (Known[i] != null && Known[i].Id == modifier.Id)
            {
                Known[i] = modifier;
                return;
            }
        }

        Known.Add(modifier);
    }

    public static ModifierDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < Known.Count; i++)
        {
            ModifierDefinition modifier = Known[i];
            if (modifier != null && modifier.Id == id)
                return modifier;
        }

        return null;
    }

    public static List<ModifierDefinition> Collect(IEnumerable<string> ids, ModifierScope scope)
    {
        var found = new List<ModifierDefinition>();
        if (ids == null)
            return found;

        foreach (string id in ids)
        {
            ModifierDefinition modifier = Find(id);
            if (modifier != null && modifier.Scope == scope)
                found.Add(modifier);
        }

        return found;
    }
}
