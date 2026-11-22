using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mutable state of one carried stack. The definition stays shared and unchanged.
/// Resources stack. Tools, weapons and anything with wear or modifiers do not.
/// </summary>
[Serializable]
public sealed class ItemInstance
{
    [SerializeField] string instanceId;
    [SerializeField] string definitionId;
    [SerializeField] int quantity = 1;
    [SerializeField] int durability = -1;
    [SerializeField] List<string> modifierIds = new List<string>();

    public string InstanceId => instanceId;
    public string DefinitionId => definitionId;
    public int Quantity => quantity;
    public int Durability => durability;
    public IReadOnlyList<string> ModifierIds => modifierIds;
    public bool TracksDurability => durability >= 0;
    public bool HasModifiers => modifierIds != null && modifierIds.Count > 0;

    public static ItemInstance Create(string definitionId, int quantity)
    {
        int wear = -1;
        ItemDefinition definition = ItemCatalog.Find(definitionId);
        if (definition != null && definition.WearCapacity > 0)
            wear = definition.WearCapacity;

        return Create(definitionId, quantity, wear, null);
    }

    public static ItemInstance Create(string definitionId, int quantity, int durability, IList<string> modifiers)
    {
        var instance = new ItemInstance();
        instance.instanceId = Guid.NewGuid().ToString("N");
        instance.definitionId = definitionId;
        instance.quantity = Mathf.Max(1, quantity);
        instance.durability = durability;
        instance.modifierIds = CopyIds(modifiers);
        return instance;
    }

    public void SetQuantity(int value)
    {
        quantity = Mathf.Max(0, value);
    }

    public void SetDurability(int value)
    {
        if (durability < 0)
            return;

        durability = Mathf.Max(0, value);
    }

    public bool AddModifier(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (modifierIds == null)
            modifierIds = new List<string>();

        if (modifierIds.Contains(id))
            return false;

        modifierIds.Add(id);
        return true;
    }

    public bool RemoveModifier(string id)
    {
        if (modifierIds == null || string.IsNullOrEmpty(id))
            return false;

        return modifierIds.Remove(id);
    }

    public static bool CanMerge(ItemInstance left, ItemInstance right)
    {
        if (left == null || right == null)
            return false;

        if (left.definitionId != right.definitionId)
            return false;

        if (left.TracksDurability || right.TracksDurability)
            return false;

        if (left.HasModifiers || right.HasModifiers)
            return false;

        return ItemIds.Stacks(left.definitionId);
    }

    static List<string> CopyIds(IList<string> source)
    {
        var copy = new List<string>();
        if (source == null)
            return copy;

        for (int i = 0; i < source.Count; i++)
        {
            if (!string.IsNullOrEmpty(source[i]))
                copy.Add(source[i]);
        }

        return copy;
    }
}
