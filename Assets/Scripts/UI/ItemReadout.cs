using System.Collections.Generic;
using UnityEngine;

public struct ReadoutLine
{
    public string Text;
    public int Emphasis;
}

/// <summary>
/// Sentences for one item. Numbers come from the definition, the instance, and AttackResolver.
/// This class does not roll damage and does not invent a second set of weapon stats.
/// </summary>
public static class ItemReadout
{
    public const int Title = 1;
    public const int Rarity = 2;
    public const int Warn = 3;

    public static void Write(ItemDefinition item, ItemInstance stack, List<ReadoutLine> into)
    {
        into.Clear();
        if (item == null && stack == null)
            return;

        string name = item != null
            ? UiGlyphs.Pick(item.DisplayName, item.DisplayNameEnglish)
            : stack.DefinitionId;
        if (string.IsNullOrEmpty(name) && stack != null)
            name = stack.DefinitionId;
        Add(into, name, Title);

        if (item != null)
        {
            Add(into, RarityName(item.Rarity), Rarity);
            Add(into, CategoryName(item.Category), 0);
            if (!string.IsNullOrEmpty(item.Description))
                Add(into, item.Description, 0);
        }

        int count = stack != null ? stack.Quantity : 0;
        if (count > 1)
            Add(into, UiGlyphs.Pick("Количество: ", "Amount: ") + count, 0);

        if (stack != null && stack.TracksDurability && item != null && item.WearCapacity > 0)
            Add(into, UiGlyphs.Pick("Прочность: ", "Durability: ") + stack.Durability + "/" + item.WearCapacity, stack.Durability <= 0 ? Warn : 0);

        if (item != null && item.Category == ItemCategory.Weapon)
            WriteWeapon(item, stack, into);

        if (item != null && item.HasTool)
            WriteTool(item, into);

        if (item != null && item.Equipment != null && Mathf.Abs(item.Equipment.HealthBonus) > 0.01f)
            Add(into, UiGlyphs.Pick("Здоровье: ", "Health: ") + Signed(item.Equipment.HealthBonus), 0);

        if (item != null && item.HasConsumable && Mathf.Abs(item.Consumable.Magnitude) > 0.01f)
            Add(into, UiGlyphs.Pick("Эффект: ", "Effect: ") + item.Consumable.EffectId + " " + Trim(item.Consumable.Magnitude), 0);

        WriteModifiers(stack, into);
    }

    static void WriteWeapon(ItemDefinition item, ItemInstance stack, List<ReadoutLine> into)
    {
        AttackPlan plan = AttackResolver.Resolve(item, stack, AttackResolver.ActiveRunModifiers());
        AttackPayload payload = plan.Payload;
        bool listed = false;
        if (payload != null)
        {
            for (int i = 0; i < payload.Parts.Count; i++)
            {
                DamageComponent part = payload.Parts[i];
                if (part.Amount <= 0.01f || part.Type == DamageType.None)
                    continue;

                string group = DamageTypes.IsElemental(part.Type)
                    ? UiGlyphs.Pick("Стихия: ", "Element: ")
                    : UiGlyphs.Pick("Урон: ", "Damage: ");
                Add(into, group + DamageName(part.Type) + " " + Trim(part.Amount), 0);
                listed = true;
            }

            for (int i = 0; i < payload.Statuses.Count; i++)
            {
                StatusSpec status = payload.Statuses[i];
                if (status.Kind == StatusKind.None || status.Chance <= 0.001f)
                    continue;

                Add(into, StatusName(status.Kind) + " " + Mathf.RoundToInt(status.Chance * 100f) + "%", 0);
            }
        }

        if (!listed && plan.Damage > 0.01f)
            Add(into, UiGlyphs.Pick("Урон: ", "Damage: ") + DamageName(plan.PrimaryType) + " " + Trim(plan.Damage), 0);

        if (plan.AttackSpeed > 0.01f)
            Add(into, UiGlyphs.Pick("Скорость атаки: ", "Attack speed: ") + Trim(plan.AttackSpeed), 0);
        if (plan.Range > 0.01f)
            Add(into, UiGlyphs.Pick("Дальность: ", "Range: ") + Trim(plan.Range), 0);
        if (plan.ManaCost > 0.01f)
            Add(into, UiGlyphs.Pick("Мана: ", "Mana: ") + Trim(plan.ManaCost), 0);
        if (plan.Reload > 0.01f)
            Add(into, UiGlyphs.Pick("Перезарядка: ", "Reload: ") + Trim(plan.Reload), 0);
        if (plan.CritChance > 0.001f)
            Add(into, UiGlyphs.Pick("Крит: ", "Crit: ") + Mathf.RoundToInt(plan.CritChance * 100f) + "%", 0);
    }

    static void WriteTool(ItemDefinition item, List<ReadoutLine> into)
    {
        ToolProfile tool = item.ResolvedTool;
        if (tool == null)
            return;

        if (tool.Power > 0.01f)
            Add(into, UiGlyphs.Pick("Сила: ", "Power: ") + Trim(tool.Power), 0);
        if (tool.Range > 0.01f)
            Add(into, UiGlyphs.Pick("Дальность: ", "Range: ") + Trim(tool.Range), 0);
        if (tool.Stamina > 0.01f)
            Add(into, UiGlyphs.Pick("Выносливость: ", "Stamina: ") + Trim(tool.Stamina), 0);
    }

    static void WriteModifiers(ItemInstance stack, List<ReadoutLine> into)
    {
        if (stack == null || !stack.HasModifiers)
            return;

        for (int i = 0; i < stack.ModifierIds.Count; i++)
        {
            string id = stack.ModifierIds[i];
            ModifierDefinition modifier = ModifierLibrary.Find(id);
            string label = modifier != null && !string.IsNullOrEmpty(modifier.TextId) ? modifier.TextId : id;
            if (!string.IsNullOrEmpty(label))
                Add(into, UiGlyphs.Pick("Руна: ", "Rune: ") + label, 0);
        }
    }

    public static string RarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return UiGlyphs.Pick("Необычный", "Uncommon");
            case ItemRarity.Rare: return UiGlyphs.Pick("Редкий", "Rare");
            case ItemRarity.VeryRare: return UiGlyphs.Pick("Очень редкий", "Very rare");
            default: return UiGlyphs.Pick("Обычный", "Common");
        }
    }

    public static string CategoryName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Resource: return UiGlyphs.Pick("Ресурс", "Resource");
            case ItemCategory.Tool: return UiGlyphs.Pick("Инструмент", "Tool");
            case ItemCategory.Weapon: return UiGlyphs.Pick("Оружие", "Weapon");
            case ItemCategory.Placeable: return UiGlyphs.Pick("Мебель", "Placeable");
            case ItemCategory.Equipment: return UiGlyphs.Pick("Экипировка", "Equipment");
            case ItemCategory.Consumable: return UiGlyphs.Pick("Расходник", "Consumable");
            default: return string.Empty;
        }
    }

    public static string DamageName(DamageType type)
    {
        switch (type)
        {
            case DamageType.Slashing: return UiGlyphs.Pick("рубящий", "slashing");
            case DamageType.Piercing: return UiGlyphs.Pick("колющий", "piercing");
            case DamageType.Blunt: return UiGlyphs.Pick("дробящий", "blunt");
            case DamageType.Fire: return UiGlyphs.Pick("огонь", "fire");
            case DamageType.Cold: return UiGlyphs.Pick("холод", "cold");
            case DamageType.Lightning: return UiGlyphs.Pick("молния", "lightning");
            case DamageType.Holy: return UiGlyphs.Pick("свет", "holy");
            case DamageType.Dark: return UiGlyphs.Pick("тьма", "dark");
            default: return string.Empty;
        }
    }

    public static string StatusName(StatusKind kind)
    {
        switch (kind)
        {
            case StatusKind.Bleed: return UiGlyphs.Pick("Кровотечение", "Bleed");
            case StatusKind.Weakness: return UiGlyphs.Pick("Слабость", "Weakness");
            default: return string.Empty;
        }
    }

    static void Add(List<ReadoutLine> into, string text, int emphasis)
    {
        if (string.IsNullOrEmpty(text))
            return;

        into.Add(new ReadoutLine { Text = text, Emphasis = emphasis });
    }

    static string Trim(float value)
    {
        return value.ToString("0.##");
    }

    static string Signed(float value)
    {
        string number = Trim(Mathf.Abs(value));
        return value >= 0f ? "+" + number : "-" + number;
    }
}
