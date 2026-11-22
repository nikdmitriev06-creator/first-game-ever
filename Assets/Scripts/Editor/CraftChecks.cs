using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch check for crafting, placement rules, and a single drop on break.
/// Run with Unity -executeMethod CraftChecks.Run
/// </summary>
public static class CraftChecks
{
    public static void Run()
    {
        int failed = 0;
        GameObject player = null;
        GameObject station = null;
        GameObject place = null;
        GameObject benchObject = null;
        GameObject chairObject = null;
        try
        {
            GameContentBuilder.EnsureAssets();
            ReceiverRegistry.ResetStatics();
            ImpactLibrary.ResetStatics();
            MaterialLibrary.ResetStatics();
            LootLibrary.ResetStatics();
            PolicyLibrary.ResetStatics();
            ImpactLibrary.Ensure();
            MaterialLibrary.Ensure();
            PolicyLibrary.Ensure();
            CraftingStation.ResetStatics();
            PlaceableObject.ResetStatics();
            PlacementBlocker.ResetStatics();
            PlacedFurniture.ResetStatics();
            LocationContext.ResetStatics();
            GameplayGate.ResetStatics();
            ItemCatalog.Reload();

            Check(ItemCatalog.Active != null && ItemCatalog.Active.IsUsable, "catalog usable", ref failed);
            ItemDefinition axe = ItemCatalog.Find(ItemIds.Axe);
            Check(axe != null && axe.HasTool && axe.ResolvedTool.Impact.Same(ImpactLibrary.Chop), "axe profile is chop", ref failed);
            Check(File.Exists("Assets/Resources/Placeables/Workbench.prefab"), "workbench prefab", ref failed);
            Check(File.Exists("Assets/Resources/Placeables/Chair.prefab"), "chair prefab", ref failed);

            RecipeDefinition benchRecipe = ItemCatalog.Active.FindRecipe("recipe.workbench");
            RecipeDefinition chairRecipe = ItemCatalog.Active.FindRecipe("recipe.chair");
            Check(benchRecipe != null && !benchRecipe.NeedsStation, "workbench is hand craft", ref failed);
            Check(chairRecipe != null && chairRecipe.NeedsStation && chairRecipe.StationId == CraftingStation.WorkbenchId, "chair needs station", ref failed);
            Check(benchRecipe.Ingredients.Length == 1 && benchRecipe.Ingredients[0].Count == 10, "workbench costs 10", ref failed);

            player = new GameObject("Craft Check Player");
            PlayerInventory inventory = AddInventory(player);
            CraftingSystem crafting = player.GetComponent<CraftingSystem>();

            inventory.TryAdd(ItemIds.Wood, 9);
            Check(!crafting.TryCraft(benchRecipe), "9 wood does not craft", ref failed);
            Check(inventory.CountOf(ItemIds.Wood) == 9, "9 wood unchanged", ref failed);
            Check(inventory.CountOf(ItemIds.Workbench) == 0, "no bench yet", ref failed);

            inventory.TryAdd(ItemIds.Wood, 1);
            Check(crafting.CanCraft(benchRecipe), "10 wood can craft", ref failed);
            Check(crafting.TryCraft(benchRecipe), "craft bench", ref failed);
            Check(inventory.CountOf(ItemIds.Wood) == 0, "wood spent", ref failed);
            Check(inventory.CountOf(ItemIds.Workbench) == 1, "bench gained", ref failed);

            inventory.SetOpen(true);
            Check(GameplayGate.MenuOpen, "menu blocks play", ref failed);
            inventory.SetOpen(false);
            Check(!GameplayGate.MenuOpen, "menu releases play", ref failed);

            GameObject full = new GameObject("Full Pack");
            PlayerInventory packed = AddInventory(full);
            CraftingSystem packedCraft = full.GetComponent<CraftingSystem>();
            packed.TryAdd(ItemIds.Wood, 20);
            packed.SetOpen(true);
            packed.ClickSlot(0);
            for (int i = 0; i < PlayerInventory.SlotCount + 2; i++)
                packed.TryAdd(ItemIds.Sword, 1);
            Check(packed.CountOf(ItemIds.Wood) == 20, "packed still has wood", ref failed);
            Check(packed.CursorCount == 20, "wood sits on the cursor", ref failed);
            Check(!packedCraft.TryCraft(benchRecipe), "no room does not craft", ref failed);
            Check(packed.CountOf(ItemIds.Wood) == 20, "wood kept when full", ref failed);
            Check(packed.CountOf(ItemIds.Workbench) == 0, "no bench when full", ref failed);
            Object.DestroyImmediate(full);

            GameObject chairPlayer = new GameObject("Chair Crafter");
            PlayerInventory chairBag = AddInventory(chairPlayer);
            CraftingSystem chairCraft = chairPlayer.GetComponent<CraftingSystem>();
            chairBag.TryAdd(ItemIds.Wood, 10);
            Check(!chairCraft.CanCraft(chairRecipe), "chair hidden without station", ref failed);
            var shown = new System.Collections.Generic.List<RecipeDefinition>();
            chairCraft.CollectVisible(shown);
            Check(!shown.Exists(recipe => recipe != null && recipe.Id == "recipe.chair"), "chair not listed", ref failed);

            station = new GameObject("Station");
            station.transform.position = chairPlayer.transform.position;
            CraftingStation nearby = station.AddComponent<CraftingStation>();
            nearby.Configure(CraftingStation.WorkbenchId, 2.8f);
            nearby.Join();
            Check(chairCraft.CanCraft(chairRecipe), "chair crafts beside station", ref failed);
            Check(chairCraft.TryCraft(chairRecipe), "craft chair", ref failed);
            Check(chairBag.CountOf(ItemIds.Wood) == 0, "chair spent wood", ref failed);
            Check(chairBag.CountOf(ItemIds.Chair) == 1, "chair gained", ref failed);

            chairBag.TryAdd(ItemIds.Wood, 10);
            station.transform.position = new Vector3(30f, 0f, 0f);
            Check(!chairCraft.CanCraft(chairRecipe), "chair stops out of range", ref failed);
            shown.Clear();
            chairCraft.CollectVisible(shown);
            Check(!shown.Exists(recipe => recipe != null && recipe.Id == "recipe.chair"), "chair leaves the list", ref failed);
            Object.DestroyImmediate(chairPlayer);

            place = MakeArea("Home Meadow");
            LocationContext home = place.GetComponent<LocationContext>();
            home.Configure(RegionKind.Home, LocationKind.Outdoor, place.GetComponent<PlayArea>());
            LocationContext.Use(home);
            ItemDefinition benchItem = ItemCatalog.Find(ItemIds.Workbench);
            ItemDefinition chairItem = ItemCatalog.Find(ItemIds.Chair);
            Vector2 feet = Vector2.zero;
            Vector2 spot = new Vector2(4f, 1f);
            Check(PlacementRules.Evaluate(benchItem, spot, feet, 0.95f) == PlaceReject.None, "bench places outdoors at home", ref failed);
            Check(PlacementRules.Evaluate(chairItem, spot, feet, 0.95f) == PlaceReject.Zone, "chair refused outdoors", ref failed);
            home.Configure(RegionKind.Wilderness, LocationKind.Outdoor, place.GetComponent<PlayArea>());
            Check(PlacementRules.Evaluate(benchItem, spot, feet, 0.95f) == PlaceReject.Zone, "bench refused in the wild", ref failed);
            home.Configure(RegionKind.Home, LocationKind.Indoor, place.GetComponent<PlayArea>());
            Check(PlacementRules.Evaluate(chairItem, spot, feet, 0.95f) == PlaceReject.None, "chair places indoors", ref failed);
            Check(PlacementRules.Evaluate(benchItem, spot, feet, 0.95f) == PlaceReject.None, "bench places indoors", ref failed);
            Check(PlacementRules.Evaluate(benchItem, feet, feet, 0.95f) == PlaceReject.Player, "not on the player", ref failed);

            int before = CountDrops(ItemIds.Chair);
            chairObject = PlaceableObject.Create(chairItem, new Vector2(6f, 2f), place.transform, false, null, 0).gameObject;
            Destructible chairBody = chairObject.GetComponent<Destructible>();
            Impact chop = new Impact(ImpactLibrary.Chop, 1f, chairBody.Origin, Vector2.right, null);
            chairBody.Receive(chop);
            Check(chairObject.GetComponent<PlaceableObject>().Health == 1, "chair took one hit", ref failed);
            Check(CountDrops(ItemIds.Chair) == before, "no drop yet", ref failed);
            chairBody.Receive(chop);
            chairBody.Receive(chop);
            Check(CountDrops(ItemIds.Chair) == before + 1, "one chair dropped", ref failed);

            int benches = CountDrops(ItemIds.Workbench);
            benchObject = PlaceableObject.Create(benchItem, new Vector2(-6f, 2f), place.transform, false, null, 0).gameObject;
            Destructible benchBody = benchObject.GetComponent<Destructible>();
            PlaceableObject other = PlaceableObject.Create(benchItem, new Vector2(8f, -6f), place.transform, false, null, 0);
            IImpactReceiver found = ReceiverRegistry.FindBest(new Vector2(-6f, 2f), new Vector2(-6f, 3f), 3.6f);
            Check(ReferenceEquals(found, benchBody), "one nearest target", ref failed);
            Check(!ReferenceEquals(found, other.GetComponent<Destructible>()), "not both targets", ref failed);
            Impact heavy = new Impact(ImpactLibrary.Chop, 3f, benchBody.Origin, Vector2.up, null);
            benchBody.Receive(heavy);
            benchBody.Receive(heavy);
            Check(CountDrops(ItemIds.Workbench) == benches + 1, "one bench dropped", ref failed);
            Check(other.Health == benchItem.Durability, "other bench untouched", ref failed);

            CheckImpactRules(ref failed);
            CheckControlStack(ref failed);
            CheckMining(ref failed);
            CheckFurnace(ref failed);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("Craft check crashed: " + error);
        }
        finally
        {
            if (player != null)
                Object.DestroyImmediate(player);
            if (station != null)
                Object.DestroyImmediate(station);
            if (benchObject != null)
                Object.DestroyImmediate(benchObject);
            if (chairObject != null)
                Object.DestroyImmediate(chairObject);
            if (place != null)
                Object.DestroyImmediate(place);
            WorldItem[] drops = Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i] != null)
                    Object.DestroyImmediate(drops[i].gameObject);
            }
        }

        if (failed == 0)
            Debug.Log("Craft checks passed.");
        else
            Debug.LogError("Craft checks failed: " + failed);

        EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    static void CheckImpactRules(ref int failed)
    {
        ImpactLibrary.Ensure();
        MaterialLibrary.Ensure();
        var shared = new GameObject("Shared Material");
        Destructible first = Destructible.Create(shared, MaterialLibrary.Wood, 3f, 2f, 1f);
        var other = new GameObject("Other Wood");
        Destructible second = Destructible.Create(other, MaterialLibrary.Wood, 3f, 2f, 1f);
        Impact chop = new Impact(ImpactLibrary.Chop, 1f, Vector2.zero, Vector2.right, null);
        first.Receive(chop);
        Check(Mathf.Approximately(first.Integrity, 2f), "first tree lost one", ref failed);
        Check(Mathf.Approximately(second.Integrity, 3f), "shared material keeps its own integrity", ref failed);

        var rock = new GameObject("Rock");
        Destructible stone = Destructible.Create(rock, MaterialLibrary.Stone, 4f, 0.7f, 0.8f);
        Check(stone.Receive(chop).Outcome == ImpactOutcome.Ignored, "axe ignores stone", ref failed);
        Check(Mathf.Approximately(stone.Integrity, 4f), "stone integrity unchanged", ref failed);
        Impact mine = new Impact(ImpactLibrary.Mine, 1f, Vector2.zero, Vector2.right, null);
        Check(stone.Receive(mine).Outcome == ImpactOutcome.Damaged, "pick damages stone", ref failed);
        var trunk = new GameObject("Trunk");
        Destructible wood = Destructible.Create(trunk, MaterialLibrary.Wood, 3f, 2f, 1f);
        Check(wood.Receive(mine).Outcome == ImpactOutcome.Ignored, "pick ignores wood", ref failed);

        var seat = new GameObject("Seat");
        Destructible furniture = Destructible.Create(seat, MaterialLibrary.Furniture, 2f, 0.6f, 0.9f);
        Impact smash = new Impact(ImpactLibrary.Smash, 2f, Vector2.zero, Vector2.down, null);
        Check(furniture.Receive(smash).Outcome == ImpactOutcome.Broken, "hammer breaks furniture", ref failed);
        Check(wood.Receive(smash).Outcome == ImpactOutcome.Ignored, "hammer ignores a living tree", ref failed);

        var plain = new GameObject("Decor");
        Check(plain.GetComponent<Destructible>() == null, "object without destructible has no body", ref failed);
        ReceiverRegistry.CollectNear(Vector2.zero, 5f, new System.Collections.Generic.List<IImpactReceiver>());

        Object.DestroyImmediate(shared);
        Object.DestroyImmediate(other);
        Object.DestroyImmediate(rock);
        Object.DestroyImmediate(trunk);
        Object.DestroyImmediate(seat);
        Object.DestroyImmediate(plain);
    }

    static void CheckControlStack(ref int failed)
    {
        GameplayGate.ResetStatics();
        ControlState state = GameplayGate.EnsureState();
        state.Push("inventory");
        Check(!state.Allows(PlayerAction.Move), "inventory blocks movement", ref failed);
        state.Push("world-menu");
        state.Pop("world-menu");
        Check(!state.Allows(PlayerAction.Move), "inventory still blocks after the other menu closes", ref failed);
        Check(state.Allows(PlayerAction.DropItem), "drop stays available", ref failed);
        state.Pop("inventory");
        Check(state.Allows(PlayerAction.Move), "movement returns", ref failed);
        Check(state.Allows(PlayerAction.UseHand), "tools return", ref failed);
    }

    static void CheckMining(ref int failed)
    {
        var created = new List<Object>();
        try
        {
            ImpactLibrary.Ensure();
            MaterialLibrary.Ensure();
            LootLibrary.Ensure();
            ItemDefinition pick = ItemCatalog.Find(ItemIds.Pickaxe);
            ItemDefinition axe = ItemCatalog.Find(ItemIds.Axe);
            ItemDefinition sword = ItemCatalog.Find(ItemIds.Sword);
            ItemDefinition stone = ItemCatalog.Find(ItemIds.Stone);
            ItemDefinition ore = ItemCatalog.Find(ItemIds.CopperOre);
            bool pickReady = pick != null && pick.HasTool && pick.ResolvedTool.Impact != null && pick.ResolvedTool.Impact.Same(ImpactLibrary.Mine);
            Check(pickReady && pick.ResolvedTool.Gesture == ToolGesture.Swing, "pickaxe is a swing tool", ref failed);
            Check(pickReady, "pickaxe impact is mine", ref failed);
            Check(pickReady && pick.ResolvedTool.Power > 0f, "pickaxe power comes from the profile", ref failed);
            Check(axe != null && axe.HasTool && axe.ResolvedTool.Impact.Same(ImpactLibrary.Chop), "axe stays chop", ref failed);
            Check(sword != null && !sword.HasTool, "sword is not a mining tool", ref failed);
            Check(stone != null && stone.MaxStack == ItemIds.ResourceStack, "stone stacks", ref failed);
            Check(ore != null && ore.MaxStack == ItemIds.ResourceStack, "ore stacks", ref failed);
            if (!pickReady || axe == null || axe.ResolvedTool == null || stone == null || ore == null)
                return;
            Check(ItemArt.Pickaxe() != null && ItemArt.Pickaxe() != ItemArt.Axe(), "pickaxe icon differs from the axe", ref failed);
            Check(ItemArt.Stone() != null && ItemArt.CopperOre() != null && ItemArt.Stone() != ItemArt.CopperOre(), "stone and ore icons differ", ref failed);
            Check(File.Exists("Assets/Resources/GameContent/Item_Pickaxe.asset"), "pickaxe asset", ref failed);
            Check(File.Exists("Assets/Resources/GameContent/Item_Stone.asset"), "stone asset", ref failed);
            Check(File.Exists("Assets/Resources/GameContent/Item_CopperOre.asset"), "ore asset", ref failed);
            Check(File.Exists("Assets/Resources/GameContent/Loot_Rock.asset"), "rock loot asset", ref failed);
            Check(File.Exists("Assets/Resources/GameContent/Impact_Mine.asset"), "mine impact asset", ref failed);

            var hand = new GameObject("Pick Hand");
            created.Add(hand);
            Axe swing = hand.AddComponent<Axe>();
            PlayerInventory handBag = hand.AddComponent<PlayerInventory>();
            handBag.TryAdd(ItemIds.Pickaxe, 1);
            Check(swing.IsHeld && swing.ItemId == ItemIds.Pickaxe, "selected pickaxe is in the hand", ref failed);
            Check(swing.GetComponent<SpriteRenderer>().sprite == ItemArt.Pickaxe(), "hand shows the pickaxe", ref failed);
            handBag.Container.Set(0, ItemIds.Axe, 1);
            handBag.TryAdd(ItemIds.Wood, 1);
            Check(swing.IsHeld && swing.ItemId == ItemIds.Axe, "axe still uses the same swing", ref failed);
            Check(swing.GetComponent<SpriteRenderer>().sprite == ItemArt.Axe(), "hand shows the axe again", ref failed);
            handBag.Container.Set(0, ItemIds.Sword, 1);
            handBag.TryAdd(ItemIds.Wood, 1);
            Check(!swing.IsHeld, "sword does not hold the swing tool", ref failed);

            var kitHost = new GameObject("Starting Kit");
            created.Add(kitHost);
            PlayerInventory kit = kitHost.AddComponent<PlayerInventory>();
            kit.GiveStartingItems();
            Check(kit.SlotAt(2) == ItemIds.Axe && kit.SlotAt(3) == ItemIds.Pickaxe && kit.CountAt(3) == 1, "pickaxe starts in the hotbar", ref failed);

            CheckLootRolls(stone, ore, ref failed);
            CheckRockTable(ref failed);

            LootTable table = ScriptableObject.CreateInstance<LootTable>();
            created.Add(table);
            table.Configure("test.rock", new[]
            {
                new LootDrop { Item = stone, Min = 1, Max = 2, Chance = 1f },
                new LootDrop { Item = ore, Min = 1, Max = 1, Chance = 0.15f }
            });

            var miss = new ScriptedLoot().Count(1).Chance(0.99f);
            HarvestNode first = MakeNode(table, miss, HarvestNode.DefaultIntegrity);
            HarvestNode second = MakeNode(table, null, HarvestNode.DefaultIntegrity);
            created.Add(first.gameObject);
            created.Add(second.gameObject);
            float full = second.Body.MaxIntegrity;
            Impact chop = new Impact(axe.ResolvedTool.Impact, axe.ResolvedTool.Power, Vector2.zero, Vector2.right, null);
            for (int i = 0; i < 6; i++)
                first.Body.Receive(chop);
            Check(Mathf.Approximately(first.Body.Integrity, full), "axe does not mine the rock", ref failed);
            Check(Mathf.Approximately(second.Body.Integrity, full), "untouched rock keeps its integrity", ref failed);
            Impact slash = new Impact(ImpactLibrary.Slash, 4f, Vector2.zero, Vector2.right, null);
            Check(first.Body.Receive(slash).Outcome == ImpactOutcome.Ignored, "slash does not mine the rock", ref failed);
            Check(Mathf.Approximately(first.Body.Integrity, full), "slash leaves the rock", ref failed);

            float loss = Mathf.Max(1f, Mathf.Ceil(pick.ResolvedTool.Power));
            int hits = Mathf.Max(1, Mathf.CeilToInt(full / loss));
            var mine = new Impact(pick.ResolvedTool.Impact, pick.ResolvedTool.Power, Vector2.zero, Vector2.up, null);
            int stoneBefore = CountDrops(ItemIds.Stone);
            int oreBefore = CountDrops(ItemIds.CopperOre);
            for (int i = 0; i < hits - 1; i++)
            {
                ImpactResult step = first.Body.Receive(mine);
                Check(step.Outcome == ImpactOutcome.Damaged, "pick chips the rock", ref failed);
            }

            Check(first.Body.Integrity > 0f, "rock still stands before the last hit", ref failed);
            Check(Mathf.Approximately(second.Body.Integrity, full), "the other rock did not share the damage", ref failed);
            Check(CountDrops(ItemIds.Stone) == stoneBefore, "no stone before the break", ref failed);
            var witnessHost = new GameObject("Drop Witness");
            created.Add(witnessHost);
            PlayerInventory witness = AddInventory(witnessHost);
            int bagBefore = witness.CountOf(ItemIds.Stone);
            ImpactResult broken = first.Body.Receive(mine);
            Check(broken.Outcome == ImpactOutcome.Broken, "pick breaks the rock", ref failed);
            Check(witness.CountOf(ItemIds.Stone) == bagBefore, "a break does not fill the bag", ref failed);
            Check(CountDrops(ItemIds.Stone) == stoneBefore + 1, "stone drops on the ground", ref failed);
            Check(CountDrops(ItemIds.CopperOre) == oreBefore, "ore misses the scripted roll", ref failed);
            WorldItem dropped = FindDrop(ItemIds.Stone);
            Check(dropped != null && dropped.StackCount == 1, "guaranteed stone uses the rolled count", ref failed);
            int stack = dropped.StackCount;
            dropped.CollectFrom(witness);
            Check(witness.CountOf(ItemIds.Stone) == bagBefore + stack, "the dropped stone can be picked up", ref failed);
            Check(dropped == null, "picked up stone leaves the ground", ref failed);
            Check(Mathf.Approximately(second.Body.Integrity, full), "breaking one rock leaves the other", ref failed);

            var hit = new ScriptedLoot().Count(2).Chance(0f);
            HarvestNode rich = MakeNode(table, hit, HarvestNode.DefaultIntegrity);
            created.Add(rich.gameObject);
            for (int i = 0; i < hits; i++)
                rich.Body.Receive(mine);
            Check(CountDrops(ItemIds.CopperOre) == oreBefore + 1, "ore hits the scripted roll", ref failed);
            WorldItem oreDrop = FindDrop(ItemIds.CopperOre);
            Check(oreDrop != null && oreDrop.StackCount == 1, "ore amount comes from the table", ref failed);
            WorldItem richStone = FindDrop(ItemIds.Stone);
            Check(richStone != null && richStone.StackCount == 2, "stone amount comes from the table", ref failed);

            LootTable woodTable = ScriptableObject.CreateInstance<LootTable>();
            created.Add(woodTable);
            woodTable.Configure("test.other", new[]
            {
                new LootDrop { Item = ItemCatalog.Find(ItemIds.Wood), Min = 1, Max = 1, Chance = 1f }
            });
            int woodBefore = CountDrops(ItemIds.Wood);
            int stoneNow = CountDrops(ItemIds.Stone);
            HarvestNode custom = MakeNode(woodTable, null, 1f);
            created.Add(custom.gameObject);
            custom.Body.Receive(mine);
            Check(CountDrops(ItemIds.Wood) == woodBefore + 1, "a node drops its own table", ref failed);
            Check(CountDrops(ItemIds.Stone) == stoneNow, "a node does not invent stone", ref failed);

            CheckCarry(ref failed, created);
        }
        finally
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
        }
    }

    static void CheckLootRolls(ItemDefinition stone, ItemDefinition ore, ref int failed)
    {
        ItemDefinition wood = ItemCatalog.Find(ItemIds.Wood);
        LootTable table = ScriptableObject.CreateInstance<LootTable>();
        table.Configure("test.rolls", new[]
        {
            new LootDrop { Item = stone, Min = 2, Max = 2, Chance = 1f },
            new LootDrop { Item = ore, Min = 1, Max = 1, Chance = 0f },
            new LootDrop { Item = wood, Min = 3, Max = 5, Chance = 0.5f }
        });
        var grants = new List<LootGrant>();
        table.Roll(grants, new ScriptedLoot().Chance(0.1f).Count(4));
        Check(grants.Count == 2 && grants[0].ItemId == ItemIds.Stone && grants[0].Count == 2, "guaranteed stone does not spend a roll", ref failed);
        Check(grants[1].ItemId == ItemIds.Wood && grants[1].Count == 4, "a later entry keeps its own roll", ref failed);

        LootTable rare = ScriptableObject.CreateInstance<LootTable>();
        rare.Configure("test.rare", new[]
        {
            new LootDrop { Item = stone, Min = 1, Max = 1, Chance = 1f },
            new LootDrop { Item = ore, Min = 1, Max = 1, Chance = 0.15f }
        });
        rare.Roll(grants, new ScriptedLoot().Chance(0.149f));
        Check(grants.Count == 2 && grants[1].ItemId == ItemIds.CopperOre, "ore drops below its chance", ref failed);
        rare.Roll(grants, new ScriptedLoot().Chance(0.15f));
        Check(grants.Count == 1 && grants[0].ItemId == ItemIds.Stone, "ore skips at its chance", ref failed);

        LootTable seeded = ScriptableObject.CreateInstance<LootTable>();
        seeded.Configure("test.seed", new[]
        {
            new LootDrop { Item = stone, Min = 1, Max = 3, Chance = 1f },
            new LootDrop { Item = ore, Min = 1, Max = 2, Chance = 0.15f },
            new LootDrop { Item = wood, Min = 1, Max = 1, Chance = 0.4f }
        });
        var left = new List<LootGrant>();
        var right = new List<LootGrant>();
        var first = new SeededLootRandom(42);
        var second = new SeededLootRandom(42);
        bool same = true;
        for (int i = 0; i < 40; i++)
        {
            seeded.Roll(left, first);
            seeded.Roll(right, second);
            if (!SameGrants(left, right))
                same = false;
        }

        Check(same, "the same loot seed matches", ref failed);
        Object.DestroyImmediate(table);
        Object.DestroyImmediate(rare);
        Object.DestroyImmediate(seeded);
    }

    static void CheckRockTable(ref int failed)
    {
        LootLibrary.Ensure();
        LootTable table = LootLibrary.Rock;
        Check(table != null, "forest rocks have a loot table", ref failed);
        if (table == null)
            return;

        bool stone = false;
        bool ore = false;
        LootDrop[] drops = table.Drops;
        for (int i = 0; i < drops.Length; i++)
        {
            string id = drops[i].ResolvedId;
            if (id == ItemIds.Stone && drops[i].Chance >= 1f && drops[i].Min >= 1 && drops[i].Max >= drops[i].Min)
                stone = true;
            if (id == ItemIds.CopperOre && drops[i].Chance > 0f && drops[i].Chance < 1f && drops[i].Min >= 1)
                ore = true;
        }

        Check(stone, "rock table always includes stone", ref failed);
        Check(ore, "rock table may include ore", ref failed);
    }

    static void CheckCarry(ref int failed, List<Object> created)
    {
        var host = new GameObject("Full Pockets");
        created.Add(host);
        PlayerInventory packed = AddInventory(host);
        for (int i = 0; i < PlayerInventory.SlotCount; i++)
            packed.TryAdd(ItemIds.Sword, 1);

        WorldItem refused = WorldDropper.Spawn(ItemIds.Stone, 2, Vector2.zero, Vector2.zero, 0f);
        created.Add(refused.gameObject);
        refused.CollectFrom(null);
        Check(refused.StackCount == 2, "a missing bag leaves the stack", ref failed);
        refused.CollectFrom(packed);
        Check(packed.CountOf(ItemIds.Stone) == 0 && refused.StackCount == 2, "a full bag leaves the stack", ref failed);

        var partialHost = new GameObject("Partial Pockets");
        created.Add(partialHost);
        PlayerInventory partial = AddInventory(partialHost);
        partial.Container.Set(0, ItemIds.Stone, ItemIds.ResourceStack - 1);
        for (int i = 1; i < PlayerInventory.SlotCount; i++)
            partial.Container.Set(i, ItemIds.Sword, 1);
        WorldItem pile = WorldDropper.Spawn(ItemIds.Stone, 3, Vector2.right, Vector2.right, 0f);
        created.Add(pile.gameObject);
        pile.CollectFrom(partial);
        Check(partial.CountOf(ItemIds.Stone) == ItemIds.ResourceStack, "a stack fills the remaining room", ref failed);
        Check(pile.StackCount == 2, "what does not fit stays on the ground", ref failed);
    }

    static void CheckFurnace(ref int failed)
    {
        var created = new List<Object>();
        try
        {
            ItemDefinition furnace = ItemCatalog.Find(ItemIds.Furnace);
            RecipeDefinition recipe = ItemCatalog.Active.FindRecipe("recipe.furnace");
            Check(furnace != null && furnace.IsPlaceable && !furnace.IsStation, "furnace places and does not smelt", ref failed);
            Check(furnace != null && furnace.Prefab != null, "furnace prefab", ref failed);
            Check(File.Exists("Assets/Resources/Placeables/Furnace.prefab"), "furnace prefab file", ref failed);
            Check(ItemArt.Furnace() != null && ItemArt.WorldSprite(ItemIds.Furnace) == ItemArt.Furnace(), "furnace sprite", ref failed);
            Check(recipe != null && recipe.NeedsStation && recipe.StationId == CraftingStation.WorkbenchId, "furnace needs a workbench", ref failed);
            Check(ItemCatalog.Active.FindRecipe("recipe.workbench") != null && ItemCatalog.Active.FindRecipe("recipe.chair") != null, "old recipes remain", ref failed);
            int stoneCost = IngredientCount(recipe, ItemIds.Stone);
            int woodCost = IngredientCount(recipe, ItemIds.Wood);
            Check(stoneCost == 20 && woodCost == 10, "furnace costs 20 stone and 10 wood", ref failed);
            if (furnace == null || recipe == null)
                return;

            var crafterHost = new GameObject("Furnace Crafter");
            created.Add(crafterHost);
            PlayerInventory bag = AddInventory(crafterHost);
            CraftingSystem crafting = crafterHost.GetComponent<CraftingSystem>();
            bag.TryAdd(ItemIds.Stone, stoneCost);
            bag.TryAdd(ItemIds.Wood, woodCost);
            var shown = new List<RecipeDefinition>();
            crafting.CollectVisible(shown);
            Check(!crafting.CanCraft(recipe), "furnace hidden away from a workbench", ref failed);
            Check(!shown.Exists(entry => entry != null && entry.Id == "recipe.furnace"), "furnace is not listed away from a workbench", ref failed);
            Check(!crafting.TryCraft(recipe), "furnace is not crafted away from a workbench", ref failed);
            Check(bag.CountOf(ItemIds.Stone) == stoneCost && bag.CountOf(ItemIds.Wood) == woodCost && bag.CountOf(ItemIds.Furnace) == 0, "a refused craft spends nothing", ref failed);

            var stationHost = new GameObject("Furnace Station");
            created.Add(stationHost);
            stationHost.transform.position = crafterHost.transform.position;
            CraftingStation station = stationHost.AddComponent<CraftingStation>();
            station.Configure(CraftingStation.WorkbenchId, 2.8f);
            station.Join();
            bag.TryConsume(ItemIds.Stone, 1);
            Check(!crafting.TryCraft(recipe), "short stone does not craft", ref failed);
            Check(bag.CountOf(ItemIds.Stone) == stoneCost - 1 && bag.CountOf(ItemIds.Wood) == woodCost && bag.CountOf(ItemIds.Furnace) == 0, "short stone spends nothing", ref failed);
            bag.TryAdd(ItemIds.Stone, 1);
            bag.TryConsume(ItemIds.Wood, 1);
            Check(!crafting.TryCraft(recipe), "short wood does not craft", ref failed);
            Check(bag.CountOf(ItemIds.Stone) == stoneCost && bag.CountOf(ItemIds.Wood) == woodCost - 1 && bag.CountOf(ItemIds.Furnace) == 0, "short wood spends nothing", ref failed);
            bag.TryAdd(ItemIds.Wood, 1);
            shown.Clear();
            crafting.CollectVisible(shown);
            Check(shown.Exists(entry => entry != null && entry.Id == "recipe.furnace"), "furnace is listed beside a workbench", ref failed);
            Check(crafting.TryCraft(recipe), "furnace crafts beside a workbench", ref failed);
            Check(bag.CountOf(ItemIds.Stone) == 0 && bag.CountOf(ItemIds.Wood) == 0 && bag.CountOf(ItemIds.Furnace) == 1, "furnace craft spends both costs", ref failed);

            bag.TryAdd(ItemIds.Stone, stoneCost);
            bag.TryAdd(ItemIds.Wood, woodCost);
            stationHost.transform.position = new Vector3(40f, 0f, 0f);
            Check(!crafting.TryCraft(recipe), "furnace stops out of range", ref failed);
            Check(bag.CountOf(ItemIds.Furnace) == 1 && bag.CountOf(ItemIds.Stone) == stoneCost && bag.CountOf(ItemIds.Wood) == woodCost, "an out of range craft spends nothing", ref failed);

            var packedHost = new GameObject("Packed Furnace");
            created.Add(packedHost);
            PlayerInventory packed = AddInventory(packedHost);
            CraftingSystem packedCraft = packedHost.GetComponent<CraftingSystem>();
            var packedStationHost = new GameObject("Packed Station");
            created.Add(packedStationHost);
            packedStationHost.transform.position = packedHost.transform.position;
            CraftingStation packedStation = packedStationHost.AddComponent<CraftingStation>();
            packedStation.Configure(CraftingStation.WorkbenchId, 2.8f);
            packedStation.Join();
            packed.TryAdd(ItemIds.Stone, stoneCost + 1);
            packed.SetOpen(true);
            packed.ClickSlot(0);
            packed.TryAdd(ItemIds.Wood, woodCost);
            packed.ClickSlot(0);
            for (int i = 0; i < PlayerInventory.SlotCount - 1; i++)
                packed.TryAdd(ItemIds.Sword, 1);
            Check(packedCraft.DenyReason(recipe) == CraftDeny.Space, "a full bag refuses the furnace", ref failed);
            Check(!packedCraft.TryCraft(recipe), "a full bag does not craft the furnace", ref failed);
            Check(packed.CountOf(ItemIds.Stone) == stoneCost + 1 && packed.CountOf(ItemIds.Wood) == woodCost && packed.CountOf(ItemIds.Furnace) == 0, "a full bag keeps both costs", ref failed);

            var placerHost = new GameObject("Furnace Placer");
            created.Add(placerHost);
            PlayerInventory placer = AddInventory(placerHost);
            PlacementSystem placement = placerHost.AddComponent<PlacementSystem>();
            placer.TryAdd(ItemIds.Furnace, 1);
            GameObject area = MakeArea("Furnace Meadow");
            created.Add(area);
            LocationContext home = area.GetComponent<LocationContext>();
            home.Configure(RegionKind.Home, LocationKind.Outdoor, area.GetComponent<PlayArea>());
            LocationContext.Use(home);
            Vector2 blocked = placerHost.transform.position;
            Check(!placement.TryPlaceAt(blocked), "furnace refuses the player's feet", ref failed);
            Check(placer.CountOf(ItemIds.Furnace) == 1, "a blocked place keeps the furnace", ref failed);
            home.Configure(RegionKind.Wilderness, LocationKind.Outdoor, area.GetComponent<PlayArea>());
            Check(!placement.TryPlaceAt(new Vector2(14f, -10f)), "furnace refuses the wild", ref failed);
            Check(placer.CountOf(ItemIds.Furnace) == 1, "a refused zone keeps the furnace", ref failed);
            home.Configure(RegionKind.Home, LocationKind.Outdoor, area.GetComponent<PlayArea>());
            Check(placement.TryPlaceAt(new Vector2(14f, -10f)), "furnace places at home", ref failed);
            Check(placer.CountOf(ItemIds.Furnace) == 0, "a placed furnace leaves the bag", ref failed);
            placer.TryAdd(ItemIds.Furnace, 1);
            Check(!placement.TryPlaceAt(new Vector2(14f, -10f)), "furnace refuses an occupied cell", ref failed);
            Check(placer.CountOf(ItemIds.Furnace) == 1, "an occupied cell keeps the furnace", ref failed);
        }
        finally
        {
            PlaceableObject[] placed = Object.FindObjectsByType<PlaceableObject>(FindObjectsInactive.Include);
            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i] != null && placed[i].ItemId == ItemIds.Furnace)
                    Object.DestroyImmediate(placed[i].gameObject);
            }

            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
        }
    }

    static HarvestNode MakeNode(LootTable table, ILootRandom random, float integrity)
    {
        var go = new GameObject("Rock");
        HarvestNode node = go.AddComponent<HarvestNode>();
        node.Configure(MaterialLibrary.Stone, integrity, 0.7f, 0.85f, table);
        LootDropper dropper = go.GetComponent<LootDropper>();
        if (dropper != null && random != null)
            dropper.UseRandom(random);
        return node;
    }

    static int IngredientCount(RecipeDefinition recipe, string id)
    {
        int total = 0;
        Ingredient[] cost = recipe != null ? recipe.Ingredients : null;
        if (cost == null)
            return 0;

        for (int i = 0; i < cost.Length; i++)
        {
            if (cost[i].ItemId == id)
                total += cost[i].Count;
        }

        return total;
    }

    static bool SameGrants(List<LootGrant> left, List<LootGrant> right)
    {
        if (left.Count != right.Count)
            return false;

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i].ItemId != right[i].ItemId || left[i].Count != right[i].Count)
                return false;
        }

        return true;
    }

    static WorldItem FindDrop(string id)
    {
        WorldItem[] drops = Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include);
        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i] != null && drops[i].StackId == id)
                return drops[i];
        }

        return null;
    }

    static PlayerInventory AddInventory(GameObject host)
    {
        PlayerInventory inventory = host.AddComponent<PlayerInventory>();
        // Batch edit mode does not run Awake, so the same components are added here.
        if (host.GetComponent<CraftingSystem>() == null)
            host.AddComponent<CraftingSystem>();
        return inventory;
    }

    static GameObject MakeArea(string name)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixels(new Color[16]);
        texture.Apply();
        renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 1f);
        go.transform.localScale = new Vector3(40f, 30f, 1f);
        PlayArea area = go.AddComponent<PlayArea>();
        area.CacheBounds();
        LocationContext context = go.AddComponent<LocationContext>();
        context.Configure(RegionKind.Home, LocationKind.Outdoor, area);
        return go;
    }

    static int CountDrops(string id)
    {
        int count = 0;
        WorldItem[] drops = Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i] != null && drops[i].name == "Dropped " + id)
                count++;
        }

        return count;
    }

    static void Check(bool condition, string label, ref int failed)
    {
        if (condition)
        {
            Debug.Log("OK " + label);
            return;
        }

        failed++;
        Debug.LogError("FAIL " + label);
    }

    sealed class ScriptedLoot : ILootRandom
    {
        readonly Queue<float> _chances = new Queue<float>();
        readonly Queue<int> _counts = new Queue<int>();

        public ScriptedLoot Chance(float value)
        {
            _chances.Enqueue(value);
            return this;
        }

        public ScriptedLoot Count(int value)
        {
            _counts.Enqueue(value);
            return this;
        }

        public float Next01()
        {
            return _chances.Count > 0 ? _chances.Dequeue() : 1f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (_counts.Count == 0 || maxExclusive <= minInclusive)
                return minInclusive;

            int value = _counts.Dequeue();
            if (value < minInclusive)
                return minInclusive;
            if (value >= maxExclusive)
                return maxExclusive - 1;
            return value;
        }
    }
}
