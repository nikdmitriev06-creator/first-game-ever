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
}
