using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Logic checks for slots, notices, tooltips, craft reasons and the minimap sketch.
/// Run with Unity -executeMethod UiChecks.Run
/// </summary>
public static class UiChecks
{
    public static void Run()
    {
        int failed = 0;
        GameObject host = null;
        bool persist = ProgressionService.PersistInterface;
        ProgressionService.PersistInterface = false;
        int brokeCount = 0;
        System.Action<string> broke = _ => brokeCount++;
        ItemInstance.Broke += broke;
        int locationLevel = 0;
        System.Action<string, int> advanced = (_, level) => locationLevel = level;
        ProgressionService.LocationAdvanced += advanced;

        try
        {
            GameContentBuilder.EnsureAssets();
            ItemCatalog.Reload();
            Notices.ResetStatics();

            host = new GameObject("Ui Check Player");
            PlayerInventory bag = host.AddComponent<PlayerInventory>();
            CraftingSystem crafting = host.GetComponent<CraftingSystem>();
            if (crafting == null)
                crafting = host.AddComponent<CraftingSystem>();

            CheckSlots(bag, ref failed);
            CheckNotices(ref failed);
            CheckReadout(ref failed);
            CheckCraft(bag, crafting, ref failed);
            CheckMap(ref failed);
            CheckBreak(ref failed, ref brokeCount);
            CheckLocation(ref failed, ref locationLevel);
            CheckPreferences(ref failed);
            CheckLayout(ref failed);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("FAIL ui checks threw: " + error);
        }
        finally
        {
            ItemInstance.Broke -= broke;
            ProgressionService.LocationAdvanced -= advanced;
            ProgressionService.PersistInterface = persist;
            if (host != null)
                Object.DestroyImmediate(host);
            ProgressionService.EnableDisk();
            Notices.ResetStatics();
        }

        if (failed == 0)
            Debug.Log("UiChecks passed");
        else
            Debug.LogError("UiChecks failed: " + failed);

        EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    static void CheckSlots(PlayerInventory bag, ref int failed)
    {
        bag.SetOpen(true);
        var worn = ItemInstance.Create(ItemIds.Sword, 1, 4, null);
        var fresh = ItemInstance.Create(ItemIds.Sword, 1, 9, null);
        Check(!ItemInstance.CanMerge(worn, fresh), "different wear does not merge", ref failed);

        var runed = ItemInstance.Create(ItemIds.Wood, 4, -1, new[] { "spark" });
        var plain = ItemInstance.Create(ItemIds.Wood, 4, -1, null);
        Check(!ItemInstance.CanMerge(runed, plain), "a rune blocks merging", ref failed);
        Check(!ItemInstance.CanSplit(runed), "a rune is not split", ref failed);
        Check(!ItemInstance.CanMerge(ItemInstance.Create(ItemIds.Wood, 2), ItemInstance.Create(ItemIds.Stone, 2)), "different ids stay apart", ref failed);

        bag.Container.Set(10, ItemIds.Wood, 4);
        bag.Container.Set(11, ItemIds.Wood, 5);
        bag.ClickSlot(10);
        bag.ClickSlot(11);
        Check(bag.CountAt(11) == 9 && bag.CursorCount == 0, "plain stacks merge", ref failed);

        bag.Container.Set(12, ItemIds.Wood, 8);
        Check(bag.SplitSlot(12) == SlotMove.Split, "a stack splits", ref failed);
        Check(bag.CountAt(12) == 4 && bag.CursorCount == 4, "a split keeps both halves", ref failed);
        bag.ClickSlot(12);
        Check(bag.CountAt(12) == 8 && bag.CursorCount == 0, "the halves merge back", ref failed);

        bag.Container.SetInstance(1, worn);
        bag.Container.SetInstance(2, fresh);
        bag.ClickSlot(1);
        bag.ClickSlot(2);
        Check(bag.InstanceAt(2) == worn && worn.Durability == 4, "worn sword keeps its wear", ref failed);
        Check(bag.CursorCount == 1 && fresh.Durability == 9, "the other sword keeps its wear", ref failed);
        bag.ClickSlot(1);
        Check(bag.CursorCount == 0, "the spare sword returns to a slot", ref failed);

        bag.Container.Set(0, ItemIds.Sword, 1);
        Check(bag.SplitSlot(0) == SlotMove.RefusedUnique, "a sword does not split", ref failed);

        for (int i = 0; i < PlayerInventory.SlotCount; i++)
            bag.Container.Set(i, ItemIds.Sword, 1);
        bag.ClickSlot(3);
        bag.Container.Set(3, ItemIds.Sword, 1);
        Check(bag.ClickOutside() == SlotMove.RefusedSpace, "a full bag refuses the cursor", ref failed);
        Check(bag.CursorCount == 1, "the refused sword stays on the cursor", ref failed);
        Check(bag.TryAdd(ItemIds.Wood, 2) == 0, "a full bag accepts nothing", ref failed);

        bag.SelectSlot(4);
        Check(bag.Selected == 4, "hotbar slot can be chosen", ref failed);
        bag.Cycle(1);
        Check(bag.Selected == 5, "hotbar cycles forward", ref failed);
        bag.Container.Set(5, ItemIds.Bow, 1);
        Check(bag.SlotAt(bag.Selected) == ItemIds.Bow, "the chosen slot is the held item", ref failed);
        bag.Container.Clear(6);
        bag.SelectSlot(6);
        Check(bag.Selected == 6 && bag.SlotAt(6) == null, "an empty slot can be selected", ref failed);
    }

    static void CheckNotices(ref int failed)
    {
        NoticeBoard board = Notices.Board;
        board.Clear();
        board.Duration = 3f;
        board.HighDuration = 5f;
        for (int i = 0; i < 20; i++)
            board.Post(NoticeKind.Item, "wood", 1, 0);

        Check(board.VisibleCount == 1 && board.VisibleAt(0).Amount == 20, "identical notices merge", ref failed);

        board.Clear();
        for (int i = 0; i < 4; i++)
            board.Post(NoticeKind.Item, "stack" + i, 1, 0);
        board.Post(NoticeKind.Rare, "relic", 1, 1);
        bool sawRare = false;
        for (int i = 0; i < board.VisibleCount; i++)
        {
            if (board.VisibleAt(i).Kind == NoticeKind.Rare)
                sawRare = true;
        }

        Check(sawRare && board.VisibleCount == 4 && board.Queued == 1, "a rare notice takes a visible place", ref failed);
        board.Tick(3.1f);
        Check(board.VisibleCount == 2, "expired notices let the queue through", ref failed);

        var full = new NoticeEntry { Kind = NoticeKind.Impossible, Subject = "space", Amount = 1 };
        string text = NoticeHud.Format(full);
        Check(text.Contains("мест") || text.Contains("full") || text.Contains("Inventory"), "a full bag has a sentence", ref failed);
    }

    static void CheckReadout(ref int failed)
    {
        var lines = new List<ReadoutLine>();
        ItemDefinition staff = ItemCatalog.Find(ItemIds.Staff);
        ItemReadout.Write(staff, ItemInstance.Create(ItemIds.Staff, 1), lines);
        Check(Has(lines, "18"), "staff tooltip shows mana", ref failed);
        Check(Has(lines, "огонь") || Has(lines, "fire"), "staff tooltip shows fire", ref failed);
        Check(!Has(lines, "Перезарядка") && !Has(lines, "Reload"), "a zero reload stays hidden", ref failed);
        Check(!Has(lines, "Выносливость") && !Has(lines, "Stamina"), "a zero stamina cost stays hidden", ref failed);

        ItemDefinition wood = ItemCatalog.Find(ItemIds.Wood);
        ItemReadout.Write(wood, ItemInstance.Create(ItemIds.Wood, 6), lines);
        Check(Has(lines, "6"), "a stack shows its count", ref failed);
        Check(!Has(lines, "Мана") && !Has(lines, "Mana"), "wood has no mana line", ref failed);

        ItemDefinition crystal = ItemCatalog.Find(ItemIds.Crystal);
        ItemReadout.Write(crystal, null, lines);
        Check(Has(lines, "Редкий") || Has(lines, "Rare"), "crystal shows its rarity", ref failed);
    }

    static void CheckCraft(PlayerInventory bag, CraftingSystem crafting, ref int failed)
    {
        for (int i = 0; i < PlayerInventory.SlotCount; i++)
            bag.Container.Clear(i);

        RecipeDefinition bench = ItemCatalog.Active.FindRecipe("recipe.workbench");
        RecipeDefinition chair = ItemCatalog.Active.FindRecipe("recipe.chair");
        var known = new List<RecipeDefinition>();
        crafting.CollectKnown(known);
        Check(known.Exists(recipe => recipe != null && recipe.Id == "recipe.chair"), "a station recipe stays listed", ref failed);

        var shown = new List<RecipeDefinition>();
        crafting.CollectVisible(shown);
        Check(!shown.Exists(recipe => recipe != null && recipe.Id == "recipe.chair"), "the old list still hides a far station", ref failed);

        Check(crafting.DenyReason(bench) == CraftDeny.Ingredients, "the bench is short on wood", ref failed);
        var gaps = new List<CraftGap>();
        crafting.CopyCosts(bench, gaps);
        Check(gaps.Count > 0 && gaps[0].Short && gaps[0].Have < gaps[0].Need, "the shortage names the missing count", ref failed);
        Check(crafting.DenyReason(chair) == CraftDeny.Station, "the chair explains the missing station", ref failed);

        string station = NoticeHud.Format(new NoticeEntry { Kind = NoticeKind.Impossible, Subject = "station:" + chair.StationId, Amount = 1 });
        Check(station.IndexOf("стан", System.StringComparison.OrdinalIgnoreCase) >= 0
            || station.IndexOf("station", System.StringComparison.OrdinalIgnoreCase) >= 0
            || station.IndexOf("bench", System.StringComparison.OrdinalIgnoreCase) >= 0
            || station.IndexOf("верст", System.StringComparison.OrdinalIgnoreCase) >= 0, "the station refusal names the bench", ref failed);

        bag.TryAdd(ItemIds.Wood, 10);
        Check(crafting.DenyReason(bench) == CraftDeny.None, "enough wood makes the bench", ref failed);
        Check(crafting.TryCraft(bench), "the bench can be crafted", ref failed);
        crafting.CopyCosts(bench, gaps);
        Check(gaps.Count > 0 && gaps[0].Have == 0, "costs follow the bag after crafting", ref failed);

        var access = new CraftAccess();
        string opened = null;
        crafting.UseAccess(access);
        crafting.RecipeOpened += id => opened = id;
        access.Unlock("gate.demo");
        access.Unlock("gate.demo");
        Check(opened == "gate.demo", "a new recipe opens once", ref failed);
    }

    static void CheckMap(ref int failed)
    {
        var map = new MapSketch();
        map.AddCell(0, 0, false, false, false, true);
        map.AddCell(1, 0, true, true, true, true);
        map.AddPin(0, 0, 0.5f, 0.5f, MapPinKind.Chest, false);
        map.AddPin(1, 0, 0.8f, 0.2f, MapPinKind.Exit, true);
        Check(!map.Cells[0].Shown, "fog hides an unvisited room", ref failed);
        Check(map.Cells[1].Shown && map.Cells[1].Exit, "the visited exit stays visible", ref failed);
        Check(map.PlayerColumn == 1 && map.PlayerRow == 0, "the player sits in the current room", ref failed);
        Check(!map.Pins[0].Shown && map.Pins[1].Shown, "a chest appears only in a seen room", ref failed);

        map.Clear();
        map.AddCell(0, 0, true, true, false, true);
        Check(map.PlayerColumn == 0, "the first room holds the player", ref failed);
        map.Clear();
        map.AddCell(0, 0, true, false, false, true);
        map.AddCell(1, 0, true, true, true, true);
        Check(map.PlayerColumn == 1 && map.Cells[1].Exit, "entering the next room moves the marker", ref failed);

        Check(map.CycleMode() == 1 && !map.ShowsMap && map.ShowsCaption, "the map can collapse", ref failed);
        Check(map.CycleMode() == 2 && !map.ShowsCaption, "the map can hide", ref failed);
        map.Clear();
        map.AddCell(1, 0, true, true, true, true);
        Check(!map.Cells[0].Shown, "a hidden map draws no room", ref failed);
        Check(map.CycleMode() == 0 && map.ShowsMap, "the map can be shown again", ref failed);
    }

    static void CheckBreak(ref int failed, ref int brokeCount)
    {
        brokeCount = 0;
        var blade = ItemInstance.Create(ItemIds.Sword, 1, 2, null);
        blade.SetDurability(1);
        Check(brokeCount == 0, "wear above zero is quiet", ref failed);
        blade.SetDurability(0);
        blade.SetDurability(0);
        Check(brokeCount == 1, "breaking posts once", ref failed);
    }

    static void CheckLocation(ref int failed, ref int locationLevel)
    {
        var progress = new GameProgress();
        ProgressionService.Use(progress, false);
        ProgressionService.AdvanceLocation("forest");
        Check(locationLevel == 2, "location level raises a fact", ref failed);
    }

    static void CheckPreferences(ref int failed)
    {
        ProgressionService.RememberHotbar(4);
        ProgressionService.RememberMap(1);
        Check(ProgressionService.HotbarSlot == 4, "the hotbar slot is remembered", ref failed);
        Check(ProgressionService.MapMode == 1, "the map mode is remembered", ref failed);
        ProgressionService.RememberHotbar(0);
        ProgressionService.RememberMap(0);
    }

    static void CheckLayout(ref int failed)
    {
        Vector2 menu = InventoryHud.OpenedFootprint();
        Vector2 bar = InventoryHud.HotbarFootprint();
        Check(menu.x > 400f && menu.y > 200f, "the inventory names its own size", ref failed);
        Check(UiScreen.FitsAll(menu, 48f), "the inventory fits every preview resolution", ref failed);
        Check(UiScreen.FitsAll(bar, 24f), "the hotbar fits every preview resolution", ref failed);
        Check(UiScreen.FitsAll(new Vector2(360f, 460f), 24f), "the minimap cap fits every preview resolution", ref failed);

        for (int i = 0; i < UiScreen.PreviewResolutions.Length; i++)
        {
            Vector2Int size = UiScreen.PreviewResolutions[i];
            Check(UiScreen.CoversReference(size.x, size.y), "canvas units cover 1920x1080 at " + size.x + "x" + size.y, ref failed);
            float scale = UiScreen.ExpandScale(size.x, size.y);
            Vector2 corner = UiScreen.TooltipTopLeft(new Vector2(size.x - 2f, 6f), 320f * scale, 180f * scale, size.x, size.y, 8f, 18f);
            float right = corner.x + 320f * scale;
            float bottom = corner.y - 180f * scale;
            Check(corner.x >= 8f && right <= size.x - 8f && bottom >= 8f && corner.y <= size.y - 8f, "a tooltip stays on screen at " + size.x + "x" + size.y, ref failed);
        }
    }

    static bool Has(List<ReadoutLine> lines, string piece)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Text != null && lines[i].Text.IndexOf(piece, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
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
