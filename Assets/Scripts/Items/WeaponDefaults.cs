/// <summary>
/// Starting numbers for every weapon. The sword and the bow match the player prefab.
/// A new weapon is another profile here, not another damage formula.
/// </summary>
public static class WeaponDefaults
{
    public static WeaponProfile For(string id)
    {
        if (id == ItemIds.Sword)
            return Sword();
        if (id == ItemIds.Bow)
            return Bow();
        if (id == ItemIds.Spear)
            return Spear();
        if (id == ItemIds.Glaive)
            return Glaive();
        if (id == ItemIds.ShortSword)
            return ShortSword();
        if (id == ItemIds.Greatsword)
            return Greatsword();
        if (id == ItemIds.Staff)
            return Staff();
        if (id == ItemIds.Crossbow)
            return Crossbow();

        return null;
    }

    public static WeaponProfile Sword()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Melee,
            Damage = 12f,
            AttackSpeed = 1.92f,
            Range = 2.8f,
            Stamina = 10f,
            CritChance = 0.08f,
            CritMultiplier = 1.4f,
            DamageTypeId = "slashing",
            TypedDamage = DamageType.Slashing,
            ArcDegrees = 90f,
            Windup = 0.26f,
            Hitbox = PaintedSwordBox()
        };
    }

    public static WeaponProfile Bow()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Projectile,
            Damage = 9f,
            AttackSpeed = 2.63f,
            Range = 52f,
            Stamina = 6f,
            CritChance = 0.12f,
            CritMultiplier = 1.4f,
            ProjectileSpeed = 34f,
            DamageTypeId = "piercing",
            TypedDamage = DamageType.Piercing,
            Knockback = 1.2f
        };
    }

    public static WeaponProfile Spear()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Melee,
            Damage = 11f,
            AttackSpeed = 1.7f,
            Range = 4.8f,
            Stamina = 8f,
            CritChance = 0.06f,
            CritMultiplier = 1.45f,
            DamageTypeId = "piercing",
            TypedDamage = DamageType.Piercing,
            ArcDegrees = 18f,
            Knockback = 3.2f,
            Windup = 0.18f
        };
    }

    public static WeaponProfile Glaive()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Melee,
            Damage = 15f,
            AttackSpeed = 1.05f,
            Range = 4.2f,
            Stamina = 12f,
            CritChance = 0.05f,
            CritMultiplier = 1.4f,
            DamageTypeId = "slashing",
            TypedDamage = DamageType.Slashing,
            ArcDegrees = 160f,
            Knockback = 5f,
            Windup = 0.32f
        };
    }

    public static WeaponProfile ShortSword()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Melee,
            Damage = 7f,
            AttackSpeed = 3.15f,
            Range = 1.85f,
            Stamina = 5f,
            CritChance = 0.22f,
            CritMultiplier = 1.7f,
            DamageTypeId = "slashing",
            TypedDamage = DamageType.Slashing,
            ArcDegrees = 50f,
            Knockback = 0.8f,
            Windup = 0.12f,
            Hitbox = new WeaponHitbox
            {
                SpriteHalfLength = 0f,
                SpriteHalfWidth = 0f,
                SpriteSide = 0f,
                HalfLength = 0.925f,
                HalfWidth = 0.38f,
                Along = 0.925f
            }
        };
    }

    public static WeaponProfile Greatsword()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Melee,
            Damage = 26f,
            AttackSpeed = 0.7f,
            Range = 3.5f,
            Stamina = 16f,
            CritChance = 0.1f,
            CritMultiplier = 1.6f,
            DamageTypeId = "slashing",
            TypedDamage = DamageType.Slashing,
            ArcDegrees = 125f,
            Knockback = 11f,
            Windup = 0.48f,
            Hitbox = new WeaponHitbox
            {
                SpriteHalfLength = 0f,
                SpriteHalfWidth = 0f,
                SpriteSide = 0f,
                HalfLength = 1.75f,
                HalfWidth = 1.05f,
                Along = 1.75f
            }
        };
    }

    public static WeaponProfile Staff()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Magic,
            Damage = 14f,
            AttackSpeed = 1.25f,
            Range = 30f,
            Stamina = 0f,
            CritChance = 0.05f,
            CritMultiplier = 1.5f,
            ProjectileSpeed = 18f,
            DamageTypeId = "fire",
            TypedDamage = DamageType.Fire,
            ManaCost = 18f,
            Knockback = 2f
        };
    }

    public static WeaponProfile Crossbow()
    {
        return new WeaponProfile
        {
            Kind = AttackKind.Projectile,
            Damage = 22f,
            AttackSpeed = 0.85f,
            Range = 46f,
            Stamina = 8f,
            CritChance = 0.08f,
            CritMultiplier = 1.5f,
            ProjectileSpeed = 42f,
            DamageTypeId = "piercing",
            TypedDamage = DamageType.Piercing,
            Reload = 1.45f,
            Knockback = 6f
        };
    }

    static WeaponHitbox PaintedSwordBox()
    {
        return new WeaponHitbox
        {
            SpriteHalfLength = 102f,
            SpriteHalfWidth = 33f,
            SpriteAlong = 0f,
            SpriteSide = -2.8f
        };
    }
}
