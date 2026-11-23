using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode checks for damage types, resistances, statuses and modifiers.
/// Run with Unity -executeMethod CombatChecks.Run
/// </summary>
public static class CombatChecks
{
    [MenuItem("Game/Проверить бой")]
    public static void Run()
    {
        int failed = 0;
        var created = new List<Object>();
        try
        {
            GameContentBuilder.EnsureAssets();
            ItemCatalog.Reload();
            ModifierLibrary.Clear();
            if (ProgressionService.Run != null)
                ProgressionService.Run.ClearTemporaryModifiers();
            ImpactLibrary.ResetStatics();
            MaterialLibrary.ResetStatics();
            ImpactLibrary.Ensure();
            MaterialLibrary.Ensure();

            CheckWeapons(created, ref failed);
            CheckMixedAndResist(created, ref failed);
            CheckCrit(created, ref failed);
            CheckStatuses(created, ref failed);
            CheckModifiers(created, ref failed);
            CheckPlainEnemy(created, ref failed);
            CheckLoadout(created, ref failed);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("Combat check crashed: " + error);
        }
        finally
        {
            if (ProgressionService.Run != null)
                ProgressionService.Run.ClearTemporaryModifiers();
            ModifierLibrary.Clear();
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
        }

        if (failed == 0)
            Debug.Log("Combat checks passed.");
        else
            Debug.LogError("Combat checks failed: " + failed);

        if (Application.isBatchMode)
            EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    static void CheckWeapons(List<Object> created, ref int failed)
    {
        ItemDefinition sword = ItemCatalog.Find(ItemIds.Sword);
        ItemDefinition bow = ItemCatalog.Find(ItemIds.Bow);
        Check(sword != null && sword.Weapon != null, "sword asset has a profile", ref failed);
        Check(bow != null && bow.Weapon != null, "bow asset has a profile", ref failed);
        if (sword == null || bow == null)
            return;

        WeaponProfile swordProfile = sword.ResolvedWeapon;
        WeaponProfile bowProfile = bow.ResolvedWeapon;
        Check(Approximately(swordProfile.Damage, 12f) && Approximately(swordProfile.AttackSpeed, 1.92f) && Approximately(swordProfile.Range, 2.8f), "sword numbers stay on the profile (" + swordProfile.Damage + " / " + swordProfile.AttackSpeed + " / " + swordProfile.Range + ")", ref failed);
        Check(swordProfile.TypedDamage == DamageType.Slashing, "sword is slashing", ref failed);
        Check(Approximately(bowProfile.Damage, 9f) && Approximately(bowProfile.ProjectileSpeed, 34f) && Approximately(bowProfile.AttackSpeed, 2.63f), "bow numbers stay on the profile", ref failed);
        Check(bowProfile.TypedDamage == DamageType.Piercing && bowProfile.Kind == AttackKind.Projectile, "bow is a piercing shot", ref failed);

        AttackPlan swordPlan = AttackResolver.Resolve(sword, null, null);
        AttackPlan bowPlan = AttackResolver.Resolve(bow, null, null);
        Check(swordPlan.PrimaryType == DamageType.Slashing && Approximately(Part(swordPlan, DamageType.Slashing), 12f), "sword plan is 12 slashing", ref failed);
        Check(bowPlan.PrimaryType == DamageType.Piercing && Approximately(Part(bowPlan, DamageType.Piercing), 9f), "bow plan is 9 piercing", ref failed);
        Check(bowPlan.ProjectileCount == 1, "bow starts with one arrow", ref failed);

        AttackPlan legacyShot = AttackResolver.Resolve(MakeProfileItem(created, AttackKind.Projectile, "physical", DamageType.None, 9f), null, null);
        AttackPlan legacySwing = AttackResolver.Resolve(MakeProfileItem(created, AttackKind.Melee, "physical", DamageType.None, 12f), null, null);
        Check(legacyShot.PrimaryType == DamageType.Piercing, "legacy physical shot is piercing", ref failed);
        Check(legacySwing.PrimaryType == DamageType.Slashing, "legacy physical swing is slashing", ref failed);
    }

    static void CheckMixedAndResist(List<Object> created, ref int failed)
    {
        ItemDefinition mixed = MakeProfileItem(created, AttackKind.Melee, "slashing", DamageType.Slashing, 20f);
        mixed.Weapon.BonusDamage = new[] { new DamageComponent(DamageType.Fire, 10f), new DamageComponent(DamageType.Cold, 10f) };
        AttackPlan plan = AttackResolver.Resolve(mixed, null, null);
        CombatStrike strike = CombatStrike.Build(plan, 0.9f, 1f);
        Check(Approximately(strike.Total, 40f), "mixed attack keeps every component", ref failed);
        Check(Approximately(Amount(strike, DamageType.Slashing), 20f), "mixed slash is intact", ref failed);
        Check(Approximately(Amount(strike, DamageType.Fire), 10f), "mixed fire is intact", ref failed);

        var resists = new List<DamageResistance>
        {
            new DamageResistance { Type = DamageType.Fire, Amount = 0.5f },
            new DamageResistance { Type = DamageType.Cold, Amount = -0.5f }
        };
        float fireOnly = DamageMath.AfterResist(new[]
        {
            new DamageComponent(DamageType.Slashing, 20f),
            new DamageComponent(DamageType.Fire, 10f)
        }, new[] { resists[0] });
        Check(Approximately(fireOnly, 25f), "fire resist does not change slash", ref failed);

        float coldOnly = DamageMath.AfterResist(new[]
        {
            new DamageComponent(DamageType.Slashing, 20f),
            new DamageComponent(DamageType.Cold, 10f)
        }, new[] { resists[1] });
        Check(Approximately(coldOnly, 35f), "cold vulnerability does not change slash", ref failed);

        float blocked = DamageMath.AfterResist(new[] { new DamageComponent(DamageType.Slashing, 12f) }, new[]
        {
            new DamageResistance { Type = DamageType.Slashing, Amount = 5f }
        });
        Check(Approximately(blocked, 0f), "huge resist cannot deal negative damage", ref failed);

        Enemy enemy = MakeEnemy(created);
        enemy.Stats.resistances = new[] { resists[0] };
        var hit = new Impact(ImpactLibrary.Slash, 30f, Vector2.zero, Vector2.right, null, strike);
        ImpactResult result = enemy.Receive(hit);
        Check(result.Outcome == ImpactOutcome.Damaged, "mixed hit lands", ref failed);
        Check(Approximately(enemy.Health, 100f - DamageMath.AfterResist(strike.Parts, enemy.Stats.resistances)), "enemy health uses per-type resist", ref failed);

        Enemy immune = MakeEnemy(created);
        immune.Stats.resistances = new[] { new DamageResistance { Type = DamageType.Slashing, Amount = 5f } };
        CombatStrike slash = CombatStrike.Flat(DamageType.Slashing, 12f, null);
        immune.Receive(new Impact(ImpactLibrary.Slash, 12f, Vector2.zero, Vector2.right, null, slash));
        Check(Approximately(immune.Health, 100f) && !immune.Dead, "blocked slash leaves the enemy alive", ref failed);
    }

    static void CheckCrit(List<Object> created, ref int failed)
    {
        ItemDefinition item = MakeProfileItem(created, AttackKind.Melee, "slashing", DamageType.Slashing, 20f);
        item.Weapon.BonusDamage = new[] { new DamageComponent(DamageType.Fire, 10f) };
        item.Weapon.CritChance = 1f;
        item.Weapon.CritMultiplier = 2f;
        AttackPlan plan = AttackResolver.Resolve(item, null, null);
        CombatStrike crit = CombatStrike.Build(plan, 0f, 1f);
        CombatStrike plain = CombatStrike.Build(plan, 1f, 1f);
        Check(crit.Crit && Approximately(crit.Total, 60f), "crit doubles every component", ref failed);
        Check(!plain.Crit && Approximately(plain.Total, 30f), "roll outside the chance is not a crit", ref failed);

        float dealt = DamageMath.AfterResist(crit.Parts, new[]
        {
            new DamageResistance { Type = DamageType.Fire, Amount = 0.5f }
        });
        Check(Approximately(dealt, 50f), "crit happens before the fire resist", ref failed);

        CombatStrike weak = CombatStrike.Build(plan, 1f, 0.5f);
        Check(Approximately(Amount(weak, DamageType.Slashing), 10f), "weakness cuts physical damage", ref failed);
        Check(Approximately(Amount(weak, DamageType.Fire), 10f), "weakness leaves elemental damage", ref failed);
    }

    static void CheckStatuses(List<Object> created, ref int failed)
    {
        var state = new StatusState();
        Check(state.TryApply(StatusSpec.Bleed(1f, 2f, 5f, 1f), 0f), "bleed applies", ref failed);
        Check(Approximately(state.Tick(1f), 2f), "bleed ticks on its interval", ref failed);
        Check(state.TryApply(StatusSpec.Bleed(1f, 5f, 3f, 1f), 0f), "stronger bleed refreshes", ref failed);
        Check(state.TryGet(StatusKind.Bleed, out float remaining, out float power) && Approximately(remaining, 3f) && Approximately(power, 5f), "refresh keeps the higher power", ref failed);
        state.TryApply(StatusSpec.Bleed(1f, 1f, 8f, 1f), 0f);
        Check(state.TryGet(StatusKind.Bleed, out remaining, out power) && Approximately(remaining, 8f) && Approximately(power, 5f), "weaker reapply does not lower power", ref failed);
        state.Clear();

        Check(state.TryApply(StatusSpec.Bleed(1f, 4f, 4f, 1f), 0f), "timed bleed applies", ref failed);
        float bled = 0f;
        for (int i = 0; i < 4; i++)
            bled += state.Tick(1f);
        Check(Approximately(bled, 16f) && state.Count == 0, "bleed ends when the duration ends", ref failed);
        Check(Approximately(state.Tick(1f), 0f), "ended bleed deals nothing", ref failed);

        Check(state.TryApply(StatusSpec.Weakness(1f, 0.25f, 2f), 0f), "weakness applies", ref failed);
        Check(Approximately(state.OutgoingPhysicalMultiplier, 0.75f), "weakness scales physical output", ref failed);
        state.Tick(2f);
        Check(state.Count == 0 && Approximately(state.OutgoingPhysicalMultiplier, 1f), "weakness ends cleanly", ref failed);
        Check(!state.TryApply(StatusSpec.Bleed(0.2f, 4f, 4f, 1f), 0.9f), "low chance can miss", ref failed);
        state.SetImmune(StatusKind.Bleed, true);
        Check(!state.TryApply(StatusSpec.Bleed(1f, 4f, 4f, 1f), 0f), "immunity blocks a status", ref failed);

        Enemy enemy = MakeEnemy(created);
        float baseDamage = enemy.Stats.damage;
        var bleeding = CombatStrike.Flat(DamageType.Slashing, 10f, null);
        bleeding.Statuses.Add(StatusSpec.Bleed(1f, 5f, 4f, 1f));
        enemy.Receive(new Impact(ImpactLibrary.Slash, 10f, Vector2.zero, Vector2.right, null, bleeding));
        StatusHost host = enemy.GetComponent<StatusHost>();
        Check(host != null && host.ActiveCount == 1 && Approximately(enemy.Health, 90f), "hit applies bleed and the swing damage", ref failed);
        if (host != null)
        {
            Check(Approximately(host.Advance(1f), 5f), "host bleed hurts the enemy", ref failed);
            Check(Approximately(enemy.Health, 85f), "bleed comes off health", ref failed);
        }

        Enemy weakened = MakeEnemy(created);
        StatusHost weakHost = weakened.gameObject.AddComponent<StatusHost>();
        weakHost.TryApply(StatusSpec.Weakness(1f, 0.4f, 5f), 0f);
        Check(Approximately(weakened.ContactDamage(), baseDamage * 0.6f), "weak bite is smaller", ref failed);
        Check(Approximately(weakened.Stats.damage, baseDamage), "weakness does not rewrite base damage", ref failed);
        weakHost.Clear();
        Check(Approximately(weakened.ContactDamage(), baseDamage), "clearing weakness restores the bite", ref failed);

        Enemy dying = MakeEnemy(created);
        var doomed = CombatStrike.Flat(DamageType.Piercing, 20f, null);
        doomed.Statuses.Add(StatusSpec.Bleed(1f, 9f, 6f, 1f));
        dying.Receive(new Impact(ImpactLibrary.Slash, 20f, Vector2.zero, Vector2.right, null, doomed));
        StatusHost dyingHost = dying.GetComponent<StatusHost>();
        Check(dyingHost != null && dyingHost.ActiveCount == 1, "doomed enemy is bleeding", ref failed);
        dying.TakeDamage(1000f);
        Check(dying.Dead && (dyingHost == null || dyingHost.ActiveCount == 0), "death clears statuses", ref failed);
        if (dyingHost != null)
            Check(Approximately(dyingHost.Advance(1f), 0f), "a dead enemy does not keep bleeding", ref failed);
    }

    static void CheckModifiers(List<Object> created, ref int failed)
    {
        ItemDefinition sword = ItemCatalog.Find(ItemIds.Sword);
        ItemDefinition bow = ItemCatalog.Find(ItemIds.Bow);
        if (sword == null || bow == null)
        {
            Check(false, "catalog weapons missing", ref failed);
            return;
        }

        var speed = Track(created, Modifier("rune.speed", ModifierScope.Run, ModifierStat.AttackSpeedAdd, 0.5f, null));
        var doubled = Track(created, Modifier("rune.double", ModifierScope.Item, ModifierStat.DamageMultiplier, 2f, null));
        var physical = Track(created, Modifier("rune.phys", ModifierScope.Item, ModifierStat.PhysicalDamageAdd, 5f, null));
        var elemental = Track(created, Modifier("rune.fire", ModifierScope.Item, ModifierStat.ElementalDamageAdd, 10f, "fire"));
        var life = Track(created, Modifier("rune.life", ModifierScope.Item, ModifierStat.LifeOnHit, 8f, null));
        var extra = Track(created, Modifier("rune.arrows", ModifierScope.Run, ModifierStat.ExtraProjectiles, 2f, null));
        var bleed = Track(created, Modifier("rune.bleed", ModifierScope.Item, ModifierStat.OnHitStatus, 1f, "bleed"));
        var bleedPower = Track(created, Modifier("rune.bleed.power", ModifierScope.Item, ModifierStat.StatusPowerAdd, 2f, "bleed"));
        var typed = Track(created, Modifier("rune.override", ModifierScope.Item, ModifierStat.DamageTypeOverride, 0f, "fire"));
        var range = Track(created, Modifier("rune.range", ModifierScope.Item, ModifierStat.RangeAdd, 1f, null));
        var chance = Track(created, Modifier("rune.chance", ModifierScope.Item, ModifierStat.StatusChanceAdd, 0.1f, "bleed"));
        ModifierLibrary.Register(speed);
        ModifierLibrary.Register(doubled);
        ModifierLibrary.Register(physical);
        ModifierLibrary.Register(elemental);
        ModifierLibrary.Register(life);
        ModifierLibrary.Register(extra);
        ModifierLibrary.Register(bleed);
        ModifierLibrary.Register(bleedPower);
        ModifierLibrary.Register(typed);
        ModifierLibrary.Register(range);
        ModifierLibrary.Register(chance);

        var hand = new GameObject("Combat Hand");
        created.Add(hand);
        LevitatingSword blade = hand.AddComponent<LevitatingSword>();
        HeldBow heldBow = hand.AddComponent<HeldBow>();
        Check(blade.TryPreview(out AttackPlan bare) && Approximately(bare.AttackSpeed, 1.92f), "sword preview uses the profile speed", ref failed);
        Check(Approximately(blade.CurrentAttackInterval(), 1f / 1.92f), "sword interval uses that speed", ref failed);

        ProgressionService.Run.AddTemporaryModifier(speed.Id);
        ProgressionService.Run.AddTemporaryModifier(extra.Id);
        Check(blade.TryPreview(out AttackPlan hasted) && Approximately(hasted.AttackSpeed, 2.42f), "run speed rune reaches the sword", ref failed);
        Check(Approximately(blade.CurrentAttackInterval(), 1f / 2.42f), "sword interval follows the rune", ref failed);
        Check(heldBow.TryPreview(out AttackPlan volley) && volley.ProjectileCount == 3, "extra arrows reach the bow", ref failed);
        ProgressionService.Run.ClearTemporaryModifiers();
        Check(blade.TryPreview(out AttackPlan reset) && Approximately(reset.AttackSpeed, 1.92f), "clearing the run drops the rune", ref failed);

        ItemInstance stack = ItemInstance.Create(ItemIds.Sword, 1, -1, new[] { physical.Id, elemental.Id, life.Id, bleed.Id, bleedPower.Id, range.Id });
        AttackPlan tuned = AttackResolver.Resolve(sword, stack, new[] { physical.Id });
        Check(Approximately(Part(tuned, DamageType.Slashing), 17f), "physical rune adds slash damage", ref failed);
        Check(Approximately(Part(tuned, DamageType.Fire), 10f), "fire rune adds a fire component", ref failed);
        Check(Approximately(tuned.LifeOnHit, 8f), "life on hit is on the plan", ref failed);
        Check(Approximately(tuned.Range, 3.8f), "range rune extends the plan", ref failed);
        Check(tuned.Payload.IndexOfStatus(StatusKind.Bleed) >= 0, "bleed rune is on the plan", ref failed);
        StatusSpec applied = tuned.Payload.Statuses[tuned.Payload.IndexOfStatus(StatusKind.Bleed)];
        Check(Approximately(applied.Power, 5f) && Approximately(applied.Chance, 1f), "bleed power rune strengthens the status", ref failed);
        AttackPlan runIgnoresItem = AttackResolver.Resolve(sword, null, new[] { physical.Id });
        Check(Approximately(Part(runIgnoresItem, DamageType.Slashing), 12f), "item rune is ignored on the run list", ref failed);
        ItemInstance doubledStack = ItemInstance.Create(ItemIds.Sword, 1, -1, new[] { doubled.Id });
        AttackPlan twice = AttackResolver.Resolve(sword, doubledStack, null);
        Check(Approximately(Part(twice, DamageType.Slashing), 24f), "damage multiplier scales the slash component", ref failed);

        ItemInstance chanceStack = ItemInstance.Create(ItemIds.Sword, 1, -1, new[] { bleed.Id, chance.Id });
        AttackPlan chancy = AttackResolver.Resolve(sword, chanceStack, null);
        StatusSpec chancyBleed = chancy.Payload.Statuses[chancy.Payload.IndexOfStatus(StatusKind.Bleed)];
        Check(Approximately(chancyBleed.Chance, 1f), "chance stays clamped", ref failed);

        ItemDefinition custom = MakeProfileItem(created, AttackKind.Melee, "slashing", DamageType.Slashing, 12f);
        custom.Weapon.OnHit = new[] { StatusSpec.Bleed(0.15f, 3f, 4f, 0.5f) };
        ItemInstance chanceOnly = ItemInstance.Create(custom.Id, 1, -1, new[] { chance.Id });
        AttackPlan raised = AttackResolver.Resolve(custom, chanceOnly, null);
        Check(Approximately(raised.Payload.Statuses[0].Chance, 0.25f), "status chance rune adds to the profile chance", ref failed);

        ItemInstance converted = ItemInstance.Create(ItemIds.Sword, 1, -1, new[] { typed.Id });
        AttackPlan fireSword = AttackResolver.Resolve(sword, converted, null);
        Check(fireSword.PrimaryType == DamageType.Fire && Approximately(Part(fireSword, DamageType.Fire), 12f), "type override changes the slash component", ref failed);
        Check(Part(fireSword, DamageType.Slashing) == 0f, "overridden slash is gone", ref failed);

        var owner = new GameObject("Life Owner");
        created.Add(owner);
        PlayerVitals vitals = owner.AddComponent<PlayerVitals>();
        vitals.SetHealth(50f);
        Enemy target = MakeEnemy(created);
        CombatStrike healing = CombatStrike.Build(tuned, 1f, 1f);
        healing.Owner = vitals;
        target.Receive(new Impact(ImpactLibrary.Slash, healing.Total, Vector2.zero, Vector2.right, vitals, healing));
        Check(Approximately(vitals.Health, 58f), "life on hit restores the attacker", ref failed);
        Check(target.Health < 100f, "the same hit still damages", ref failed);
    }

    static void CheckPlainEnemy(List<Object> created, ref int failed)
    {
        Enemy enemy = MakeEnemy(created);
        Check(enemy.GetComponent<StatusHost>() == null, "a normal enemy has no status host", ref failed);
        ImpactResult hit = enemy.Receive(new Impact(ImpactLibrary.Slash, 12f, Vector2.zero, Vector2.right, null));
        Check(hit.Outcome == ImpactOutcome.Damaged && Approximately(enemy.Health, 88f), "a hit without a strike still hurts (hp " + enemy.Health + " max " + enemy.Stats.maxHealth + " " + hit.Outcome + " " + hit.Applied + ")", ref failed);
        ImpactResult chop = enemy.Receive(new Impact(ImpactLibrary.Chop, 12f, Vector2.zero, Vector2.right, null));
        Check(chop.Outcome == ImpactOutcome.Ignored && Approximately(enemy.Health, 88f), "a chop still ignores a creature", ref failed);

        Enemy plain = MakeEnemy(created);
        CombatStrike slash = CombatStrike.Flat(DamageType.Slashing, 12f, null);
        plain.Receive(new Impact(ImpactLibrary.Slash, 12f, Vector2.zero, Vector2.right, null, slash));
        Check(Approximately(plain.Health, 88f) && plain.GetComponent<StatusHost>() == null, "no resist and no status is a normal hit", ref failed);
    }

    static void CheckLoadout(List<Object> created, ref int failed)
    {
        ItemCatalog.Reload();
        string[] ids =
        {
            ItemIds.Sword, ItemIds.Bow, ItemIds.Spear, ItemIds.Glaive,
            ItemIds.ShortSword, ItemIds.Greatsword, ItemIds.Staff, ItemIds.Crossbow
        };
        for (int i = 0; i < ids.Length; i++)
        {
            ItemDefinition item = ItemCatalog.Find(ids[i]);
            WeaponProfile profile = item != null ? item.ResolvedWeapon : null;
            Check(item != null && profile != null && !WeaponProfile.IsBlank(profile), "weapon profile " + ids[i], ref failed);
        }

        WeaponProfile sword = ItemCatalog.Find(ItemIds.Sword).ResolvedWeapon;
        WeaponProfile bow = ItemCatalog.Find(ItemIds.Bow).ResolvedWeapon;
        WeaponProfile spear = ItemCatalog.Find(ItemIds.Spear).ResolvedWeapon;
        WeaponProfile glaive = ItemCatalog.Find(ItemIds.Glaive).ResolvedWeapon;
        WeaponProfile dagger = ItemCatalog.Find(ItemIds.ShortSword).ResolvedWeapon;
        WeaponProfile heavy = ItemCatalog.Find(ItemIds.Greatsword).ResolvedWeapon;
        WeaponProfile staff = ItemCatalog.Find(ItemIds.Staff).ResolvedWeapon;
        WeaponProfile crossbow = ItemCatalog.Find(ItemIds.Crossbow).ResolvedWeapon;
        Check(spear.Range > sword.Range && spear.ArcDegrees < 30f && spear.TypedDamage == DamageType.Piercing, "spear is a long narrow thrust", ref failed);
        Check(glaive.ArcDegrees > 140f && glaive.Range > sword.Range && glaive.AttackSpeed < sword.AttackSpeed, "glaive is a wide slower swing", ref failed);
        Check(dagger.AttackSpeed > sword.AttackSpeed && dagger.Range < sword.Range && dagger.Damage < sword.Damage, "short sword is faster and shorter", ref failed);
        Check(heavy.Damage > sword.Damage && heavy.Knockback > glaive.Knockback && heavy.AttackSpeed < glaive.AttackSpeed, "greatsword hits harder and shoves more", ref failed);
        Check(staff.Kind == AttackKind.Magic && staff.ManaCost > 0f && staff.TypedDamage == DamageType.Fire, "staff is a fire spell", ref failed);
        Check(crossbow.Reload > bow.Reload && crossbow.Damage > bow.Damage && crossbow.TypedDamage == DamageType.Piercing, "crossbow reloads and hits harder than the bow", ref failed);
        Check(!WeaponLoadout.UsesProfileHand(ItemIds.Sword) && !WeaponLoadout.UsesProfileHand(ItemIds.Bow), "sword and bow keep their own presenters", ref failed);
        Check(WeaponLoadout.UsesProfileHand(ItemIds.Spear) && WeaponLoadout.UsesProfileHand(ItemIds.Staff), "other weapons share one hand", ref failed);

        Vector2 origin = Vector2.zero;
        Vector2 aim = Vector2.right;
        Check(MeleeReach.Contains(origin, aim, new Vector2(4f, 0f), spear.Range, spear.ArcDegrees), "spear reaches straight ahead", ref failed);
        Check(!MeleeReach.Contains(origin, aim, new Vector2(3f, 2f), spear.Range, spear.ArcDegrees), "spear misses a wide angle", ref failed);
        Check(MeleeReach.Contains(origin, aim, new Vector2(2f, 2f), glaive.Range, glaive.ArcDegrees), "glaive includes that wide angle", ref failed);

        AttackPlan staffPlan = AttackResolver.Resolve(ItemCatalog.Find(ItemIds.Staff), null, null);
        AttackPlan bowPlan = AttackResolver.Resolve(ItemCatalog.Find(ItemIds.Bow), null, null);
        Check(staffPlan.ManaCost > 0f && staffPlan.PrimaryType == DamageType.Fire, "staff plan spends mana on fire", ref failed);
        Check(bowPlan.PrimaryType == DamageType.Piercing && bowPlan.ManaCost <= 0f, "bow stays a physical shot", ref failed);

        var payer = new GameObject("Mana Payer");
        created.Add(payer);
        PlayerVitals vitals = payer.AddComponent<PlayerVitals>();
        vitals.SetMana(20f);
        Check(!vitals.TrySpendMana(21f) && Approximately(vitals.Mana, 20f), "mana spend fails when short", ref failed);
        Check(vitals.TrySpendMana(18f) && Approximately(vitals.Mana, 2f), "mana spend subtracts and stays positive", ref failed);
        vitals.RecoverMana(0.2f);
        Check(Approximately(vitals.Mana, 2f), "mana does not regen during the delay", ref failed);
        vitals.RecoverMana(1f);
        vitals.RecoverMana(1f);
        Check(vitals.Mana > 2.5f && vitals.Mana <= vitals.MaxMana, "mana regens after the delay", ref failed);

        Enemy shoved = MakeEnemy(created);
        CombatStrike heavyHit = CombatStrike.Flat(DamageType.Slashing, 4f, null);
        heavyHit.Knockback = 9f;
        shoved.Receive(new Impact(ImpactLibrary.Slash, 4f, Vector2.zero, Vector2.right, null, heavyHit));
        Check(shoved.PendingKnockback > 5f && shoved.Health < 100f, "a heavy hit shoves and still deals damage", ref failed);

        var rune = Track(created, Modifier("rune.spear", ModifierScope.Item, ModifierStat.DamageAdd, 3f, null));
        ModifierLibrary.Register(rune);
        ItemInstance spearStack = ItemInstance.Create(ItemIds.Spear, 1, -1, new[] { rune.Id });
        AttackPlan poked = AttackResolver.Resolve(ItemCatalog.Find(ItemIds.Spear), spearStack, null);
        AttackResolver.Resolve(ItemCatalog.Find(ItemIds.Sword), null, null);
        Check(spearStack.ModifierIds.Count == 1 && Approximately(Part(poked, DamageType.Piercing), spear.Damage + 3f), "switching weapons leaves the spear rune in place", ref failed);
    }

    static Enemy MakeEnemy(List<Object> created)
    {
        var go = new GameObject("Combat Enemy");
        created.Add(go);
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.FillHealth();
        return enemy;
    }

    static ItemDefinition MakeProfileItem(List<Object> created, AttackKind kind, string legacyId, DamageType typed, float damage)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        if (created != null)
            created.Add(item);

        item.Configure("test." + legacyId + "." + kind + "." + damage, "Test", "Test", 1, ItemCategory.Weapon, PlacementZone.None, null, 0f, 1, 0.4f, null);
        item.AssignWeapon(new WeaponProfile
        {
            Kind = kind,
            Damage = damage,
            AttackSpeed = 1f,
            Range = 3f,
            Stamina = 0f,
            CritChance = 0f,
            CritMultiplier = 1.4f,
            DamageTypeId = legacyId,
            TypedDamage = typed
        });
        return item;
    }

    static ModifierDefinition Modifier(string id, ModifierScope scope, ModifierStat stat, float amount, string text)
    {
        ModifierDefinition modifier = ScriptableObject.CreateInstance<ModifierDefinition>();
        modifier.Configure(id, scope, stat, amount, text);
        return modifier;
    }

    static ModifierDefinition Track(List<Object> created, ModifierDefinition modifier)
    {
        created.Add(modifier);
        return modifier;
    }

    static float Part(AttackPlan plan, DamageType type)
    {
        if (plan.Payload == null)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < plan.Payload.Parts.Count; i++)
        {
            if (plan.Payload.Parts[i].Type == type)
                sum += plan.Payload.Parts[i].Amount;
        }

        return sum;
    }

    static float Amount(CombatStrike strike, DamageType type)
    {
        float sum = 0f;
        if (strike == null)
            return 0f;

        for (int i = 0; i < strike.Parts.Count; i++)
        {
            if (strike.Parts[i].Type == type)
                sum += strike.Parts[i].Amount;
        }

        return sum;
    }

    static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.001f;
    }

    static void Check(bool condition, string label, ref int failed)
    {
        if (condition)
            return;

        failed++;
        Debug.LogError("Combat check failed: " + label);
    }
}
