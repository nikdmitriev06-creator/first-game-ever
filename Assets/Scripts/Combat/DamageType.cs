/// <summary>
/// One kind of damage. Physical and elemental kinds stay in this enum so a new kind is one value,
/// not a new string check in every weapon.
/// </summary>
public enum DamageType
{
    None = 0,
    Slashing = 1,
    Piercing = 2,
    Blunt = 3,
    Fire = 4,
    Cold = 5,
    Lightning = 6,
    Holy = 7,
    Dark = 8
}

/// <summary>
/// Names and groups for <see cref="DamageType"/>. Legacy item strings still parse.
/// "physical" is not a kind of its own: a melee profile becomes slashing, a shot becomes piercing.
/// </summary>
public static class DamageTypes
{
    public static bool IsPhysical(DamageType type)
    {
        return type == DamageType.Slashing || type == DamageType.Piercing || type == DamageType.Blunt;
    }

    public static bool IsElemental(DamageType type)
    {
        return type == DamageType.Fire
            || type == DamageType.Cold
            || type == DamageType.Lightning
            || type == DamageType.Holy
            || type == DamageType.Dark;
    }

    public static string ToId(DamageType type)
    {
        switch (type)
        {
            case DamageType.Slashing: return "slashing";
            case DamageType.Piercing: return "piercing";
            case DamageType.Blunt: return "blunt";
            case DamageType.Fire: return "fire";
            case DamageType.Cold: return "cold";
            case DamageType.Lightning: return "lightning";
            case DamageType.Holy: return "holy";
            case DamageType.Dark: return "dark";
            default: return "physical";
        }
    }

    public static bool TryParse(string id, out DamageType type)
    {
        type = DamageType.None;
        if (string.IsNullOrEmpty(id))
            return false;

        switch (id.Trim().ToLowerInvariant())
        {
            case "slash":
            case "slashing":
                type = DamageType.Slashing;
                return true;
            case "pierce":
            case "piercing":
            case "arrow":
                type = DamageType.Piercing;
                return true;
            case "blunt":
            case "crush":
            case "crushing":
                type = DamageType.Blunt;
                return true;
            case "fire":
                type = DamageType.Fire;
                return true;
            case "cold":
            case "ice":
                type = DamageType.Cold;
                return true;
            case "lightning":
            case "shock":
            case "electric":
                type = DamageType.Lightning;
                return true;
            case "holy":
                type = DamageType.Holy;
                return true;
            case "dark":
                type = DamageType.Dark;
                return true;
            case "physical":
                type = DamageType.Slashing;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Typed field wins. A blank or legacy "physical" id follows the attack kind.
    /// An explicit id such as "fire" stays that kind for every weapon.
    /// </summary>
    public static DamageType Resolve(string id, AttackKind kind, DamageType typed)
    {
        if (typed != DamageType.None)
            return typed;

        if (string.IsNullOrEmpty(id) || id.Trim().ToLowerInvariant() == "physical")
            return kind == AttackKind.Projectile ? DamageType.Piercing : DamageType.Slashing;

        if (TryParse(id, out DamageType parsed))
            return parsed;

        return kind == AttackKind.Projectile ? DamageType.Piercing : DamageType.Slashing;
    }
}
