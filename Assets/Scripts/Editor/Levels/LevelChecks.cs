using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode checks for tiles, movement rules, pools, rooms, and harvest integration.
/// Run from the menu or with Unity -executeMethod LevelChecks.Run
/// </summary>
public static class LevelChecks
{
    [MenuItem("Game/Уровни/Проверить системы")]
    public static void Run()
    {
        int failed = 0;
        var junk = new List<Object>();
        try
        {
            TerrainNavigation.Install();
            CheckCoordinates(ref failed);
            CheckGrid(ref failed, junk);
            CheckSteps(ref failed);
            CheckResolver(ref failed);
            CheckProgress(ref failed);
            CheckMemoryLayout(ref failed);
            LevelContentBuilder.CreateDefaults(false);
            CheckDemoLevel(ref failed);
            CheckDemoPools(ref failed);
            CheckHarvest(ref failed, junk);
            CheckEmptySpawn(ref failed, junk);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("FAIL exception " + error);
        }
        finally
        {
            for (int i = 0; i < junk.Count; i++)
            {
                if (junk[i] != null)
                    Object.DestroyImmediate(junk[i]);
            }

            ProgressionService.EnableDisk();
            ReceiverRegistry.ResetStatics();
        }

        if (failed > 0)
            Debug.LogError("Level checks failed: " + failed);
        else
            Debug.Log("Level checks passed.");

        if (Application.isBatchMode)
            EditorApplication.Exit(failed > 0 ? 1 : 0);
    }

    static void CheckCoordinates(ref int failed)
    {
        for (int x = -3; x <= 5; x++)
        {
            for (int y = -2; y <= 4; y++)
            {
                Vector2 center = TerrainCoordinates.CellCenterWorld(x, y);
                Vector2Int cell = TerrainCoordinates.WorldToCell(center);
                Check(cell == new Vector2Int(x, y), "cell center " + x + "," + y, ref failed);
                Vector2 inside = IsometricCoordinates.IsometricToWorld(new Vector2(x + 0.2f, y + 0.2f));
                Check(TerrainCoordinates.WorldToCell(inside) == new Vector2Int(x, y), "cell inside " + x + "," + y, ref failed);
            }
        }
    }

    static void CheckGrid(ref int failed, List<Object> junk)
    {
        var host = new GameObject("Grid Check");
        junk.Add(host);
        Grid grid = host.AddComponent<Grid>();
        TerrainCoordinates.Apply(grid);
        Vector3 center = grid.GetCellCenterWorld(new Vector3Int(3, 5, 0));
        Vector2 expected = TerrainCoordinates.CellCenterWorld(3, 5);
        Check(Vector2.Distance(center, expected) < 0.001f, "unity grid matches isometric cells (" + center + " vs " + expected + ")", ref failed);
    }

    static void CheckSteps(ref int failed)
    {
        TerrainField.Cell grass = Make(true, false, 0);
        TerrainField.Cell water = Make(false, false, 0);
        TerrainField.Cell bridge = Make(true, false, 0);
        TerrainField.Cell rock = Make(false, false, 2);
        TerrainField.Cell high = Make(true, false, 1);
        TerrainField.Cell slope = Make(true, true, 0);
        TerrainField.Cell roadHigh = Make(true, false, 1);

        Check(TerrainField.CanStep(grass, true, grass, true), "grass to grass", ref failed);
        Check(!TerrainField.CanStep(grass, true, water, true), "grass stops at water", ref failed);
        Check(TerrainField.CanStep(water, true, grass, true), "leave a blocked cell", ref failed);
        Check(TerrainField.CanStep(grass, true, bridge, true), "walk onto a bridge", ref failed);
        Check(TerrainField.CanStep(bridge, true, grass, true), "leave a bridge", ref failed);
        Check(!TerrainField.CanStep(grass, true, rock, true), "rock blocks", ref failed);
        Check(!TerrainField.CanStep(grass, true, high, true), "no step onto a rise", ref failed);
        Check(TerrainField.CanStep(slope, true, high, true), "slope climbs", ref failed);
        Check(TerrainField.CanStep(high, true, slope, true), "slope descends", ref failed);
        Check(TerrainField.CanStep(high, true, roadHigh, true), "road across the rise", ref failed);
        Check(!TerrainField.CanStep(grass, true, rock, true), "cliff face blocks", ref failed);
        TerrainField.Cell ramp = Make(true, true, 0);
        TerrainField.Cell far = Make(true, false, 2);
        Check(!TerrainField.CanStep(ramp, true, far, true), "ramp cannot skip two heights", ref failed);
    }

    static TerrainField.Cell Make(bool walkable, bool ramp, int height)
    {
        return new TerrainField.Cell
        {
            Has = true,
            Walkable = walkable,
            Ramp = ramp,
            Height = height,
            Speed = 1f,
            Passage = walkable ? PassageKind.Ground : PassageKind.Blocked
        };
    }

    static void CheckResolver(ref int failed)
    {
        var pool = ScriptableObject.CreateInstance<SpawnPoolDefinition>();
        var first = new SpawnEntry { id = "a", enabled = true, weight = 1, minDifficulty = 1, maxDifficulty = 0 };
        var second = new SpawnEntry { id = "b", enabled = true, weight = 3, minDifficulty = 3, maxDifficulty = 0 };
        var off = new SpawnEntry { id = "c", enabled = false, weight = 10, minDifficulty = 1 };
        pool.Configure("test", "Test", SpawnCategory.Monster, new[] { first, second, off });

        List<SpawnEntry> low = SpawnResolver.Available(pool, 1);
        Check(low.Count == 1 && low[0].id == "a", "low difficulty keeps the first variant", ref failed);
        List<SpawnEntry> high = SpawnResolver.Available(pool, 4);
        Check(high.Count == 2, "high difficulty keeps both variants", ref failed);
        Check(SpawnResolver.Pick(high, 0f).id == "a", "roll 0 picks weight 1", ref failed);
        Check(SpawnResolver.Pick(high, 0.2f).id == "a", "roll 0.2 stays on the first", ref failed);
        Check(SpawnResolver.Pick(high, 0.25f).id == "b", "roll 0.25 crosses to weight 3", ref failed);
        Check(SpawnResolver.Pick(high, 0.9f).id == "b", "roll 0.9 stays on the heavier variant", ref failed);
        Check(SpawnResolver.Pick(new List<SpawnEntry>(), 0.5f) == null, "empty list picks nothing", ref failed);
        Check(SpawnResolver.Available(pool, 0).Count == 0, "below every minimum is empty", ref failed);
        Check(SpawnResolver.PickCount(1, 2, 0f) == 1, "count low", ref failed);
        Check(SpawnResolver.PickCount(1, 2, 0.99f) == 2, "count high", ref failed);

        var capped = new SpawnEntry { id = "old", enabled = true, weight = 1, minDifficulty = 1, maxDifficulty = 2 };
        pool.Configure("test", "Test", SpawnCategory.Monster, new[] { capped });
        Check(SpawnResolver.Available(pool, 3).Count == 0, "max difficulty excludes the entry", ref failed);
        Object.DestroyImmediate(pool);
    }

    static void CheckProgress(ref int failed)
    {
        var progress = new GameProgress();
        ProgressionService.Use(progress, false);
        Check(ProgressionService.LocationLevel("forest") == 1, "new location starts at 1", ref failed);
        ProgressionService.SetRunDifficulty(5);
        Check(ProgressionService.EffectiveDifficulty("forest") == 5, "run override is the effective difficulty", ref failed);
        Check(ProgressionService.LocationLevel("forest") == 1, "override does not write progress", ref failed);
        ProgressionService.AdvanceLocation("forest");
        Check(ProgressionService.LocationLevel("forest") == 2, "advance stores the next level", ref failed);
        Check(ProgressionService.EffectiveDifficulty("forest") == 5, "override still hides the stored level", ref failed);
        ProgressionService.ClearOverride();
        Check(ProgressionService.EffectiveDifficulty("forest") == 2, "clearing the override reads progress", ref failed);
        string path = Path.Combine(Application.persistentDataPath, "location-progress.json");
        long before = File.Exists(path) ? new FileInfo(path).Length : -1;
        ProgressionService.AdvanceLocation("forest");
        long after = File.Exists(path) ? new FileInfo(path).Length : -1;
        Check(before == after, "a memory session does not touch the progress file", ref failed);
    }

    static void CheckMemoryLayout(ref int failed)
    {
        RoomDefinition start = RoomAsset("start", 4, 4);
        RoomDefinition path = RoomAsset("path", 4, 4);
        RoomDefinition end = RoomAsset("end", 4, 4);
        start.AddMarker(Socket("start.ne", 3, 1, IsoDirection.NorthEast, true));
        path.AddMarker(Socket("path.sw", 0, 1, IsoDirection.SouthWest, true));
        path.AddMarker(Socket("path.ne", 3, 1, IsoDirection.NorthEast, false));
        end.AddMarker(Socket("end.sw", 0, 1, IsoDirection.SouthWest, true));

        var level = ScriptableObject.CreateInstance<LevelDefinition>();
        level.Configure("memory", "Memory", "forest", start, end, new[] { start, path, end }, new[] { path }, 3, 3, false, null, null);
        LayoutResult first = RoomLayoutBuilder.Build(level, 7);
        LayoutResult second = RoomLayoutBuilder.Build(level, 7);
        Check(first.Ok, "three rooms connect: " + string.Join("; ", first.Problems), ref failed);
        Check(first.Nodes.Count == 3, "three rooms were placed", ref failed);
        Check(Same(first, second), "the same seed repeats the layout", ref failed);

        var overlap = new List<LayoutPlacement>
        {
            new LayoutPlacement { room = start, origin = Vector2Int.zero },
            new LayoutPlacement { room = path, origin = Vector2Int.zero }
        };
        LayoutResult crossed = RoomLayoutBuilder.FromPlacements(overlap, 1);
        Check(!crossed.Ok, "overlapping rooms are rejected", ref failed);

        RoomDefinition cave = RoomAsset("cave", 4, 4);
        cave.AddMarker(Socket("cave.sw", 0, 1, IsoDirection.SouthWest, true));
        cave.FindMarker("cave.sw").socketTag = "cave";
        var blocked = ScriptableObject.CreateInstance<LevelDefinition>();
        blocked.Configure("blocked", "Blocked", "cave", start, cave, new[] { cave }, new[] { cave }, 2, 2, false, null, null);
        LayoutResult mismatch = RoomLayoutBuilder.Build(blocked, 1);
        Check(!mismatch.Ok, "a different socket tag does not connect", ref failed);

        Object.DestroyImmediate(level);
        Object.DestroyImmediate(blocked);
        Object.DestroyImmediate(start);
        Object.DestroyImmediate(path);
        Object.DestroyImmediate(end);
        Object.DestroyImmediate(cave);
    }

    static void CheckDemoLevel(ref int failed)
    {
        LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/Data/Levels/Level_Forest.asset");
        Check(level != null, "forest level asset", ref failed);
        if (level == null)
            return;

        LayoutResult first = RoomLayoutBuilder.Build(level, 1);
        LayoutResult second = RoomLayoutBuilder.Build(level, 1);
        Check(first.Ok, "demo level connects: " + string.Join("; ", first.Problems), ref failed);
        Check(first.Nodes.Count >= 3, "demo level places the rooms", ref failed);
        Check(Same(first, second), "demo seed is stable", ref failed);

        RoomDefinition glade = AssetDatabase.LoadAssetAtPath<RoomDefinition>("Assets/Data/Levels/Rooms/Room_ForestGlade.asset");
        Check(glade != null, "glade asset", ref failed);
        if (glade == null)
            return;

        Check(glade.SurfaceAt(0, 0) != null && glade.SurfaceAt(0, 0).Walkable, "glade grass is walkable", ref failed);
        Check(glade.SurfaceAt(9, 0) != null && !glade.SurfaceAt(9, 0).Walkable, "river is blocked", ref failed);
        Check(glade.SurfaceAt(9, 4) != null && glade.SurfaceAt(9, 4).Passage == PassageKind.Bridge, "bridge crosses the river", ref failed);
        Check(glade.SurfaceAt(2, 7) != null && glade.SurfaceAt(2, 7).Height == 1, "plateau is raised", ref failed);
        Check(glade.SurfaceAt(2, 5) != null && glade.SurfaceAt(2, 5).Ramp, "slope is a ramp", ref failed);
        var issues = new List<AuditIssue>();
        RoomAudit.Evaluate(glade, 1, issues);
        int errors = 0;
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i].Error)
                errors++;
        }

        Check(errors == 0, "glade audit has no errors (" + errors + ")", ref failed);
    }

    static void CheckDemoPools(ref int failed)
    {
        SpawnPoolDefinition enemies = AssetDatabase.LoadAssetAtPath<SpawnPoolDefinition>("Assets/Data/Levels/Pools/ForestEnemies.asset");
        SpawnPoolDefinition resources = AssetDatabase.LoadAssetAtPath<SpawnPoolDefinition>("Assets/Data/Levels/Pools/ForestResources.asset");
        Check(enemies != null && resources != null, "demo pools exist", ref failed);
        if (enemies == null || resources == null)
            return;

        Check(SpawnResolver.Available(enemies, 1).Count == 1, "forest enemies at level 1", ref failed);
        Check(SpawnResolver.Available(enemies, 4).Count == 2, "forest enemies at level 4", ref failed);
        Check(SpawnResolver.Available(resources, 1).Count == 2, "resources at level 1", ref failed);
        Check(SpawnResolver.Available(resources, 4).Count == 3, "resources at level 4", ref failed);
        Check(enemies.Entries[0].prefab != null && enemies.Entries[1].prefab != null, "enemy entries have prefabs", ref failed);
    }

    static void CheckHarvest(ref int failed, List<Object> junk)
    {
        ItemCatalog.Reload();
        MaterialLibrary.Ensure();
        LootLibrary.Ensure();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Rock.prefab");
        Check(prefab != null && prefab.GetComponent<HarvestNode>() != null, "rock prefab uses HarvestNode", ref failed);
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Tree.prefab") != null, "tree prefab exists", ref failed);
        if (prefab == null)
            return;

        LocationContext.ResetStatics();
        GameObject rock = Object.Instantiate(prefab);
        junk.Add(rock);
        HarvestNode node = rock.GetComponent<HarvestNode>();
        node.UseMaterialId("stone");
        Destructible body = node.Body;
        Check(body != null && body.Material != null && body.Material.Id == "stone", "rock resolves the stone material", ref failed);
        if (body == null)
            return;

        Impact mine = new Impact(ImpactLibrary.Mine, 1f, body.Origin, Vector2.right, null);
        ImpactResult last = default;
        for (int i = 0; i < 6 && body.Integrity > 0f; i++)
            last = body.Receive(mine);

        Check(last.Outcome == ImpactOutcome.Broken, "pick breaks the spawned rock", ref failed);
        WorldItem[] drops = UnityEngine.Object.FindObjectsByType<WorldItem>();
        WorldItem stone = null;
        for (int i = 0; i < drops.Length; i++)
        {
            if (drops[i] != null && drops[i].StackId == ItemIds.Stone)
                stone = drops[i];
            else if (drops[i] != null)
                junk.Add(drops[i].gameObject);
        }

        Check(stone != null, "rock drops a stone world item", ref failed);
        if (stone == null)
            return;

        junk.Add(stone.gameObject);
        var player = new GameObject("Level Check Player");
        junk.Add(player);
        PlayerInventory inventory = player.AddComponent<PlayerInventory>();
        if (player.GetComponent<CraftingSystem>() == null)
            player.AddComponent<CraftingSystem>();
        stone.CollectFrom(inventory);
        Check(inventory.CountOf(ItemIds.Stone) > 0, "stone enters the inventory", ref failed);
    }

    static void CheckEmptySpawn(ref int failed, List<Object> junk)
    {
        var pool = ScriptableObject.CreateInstance<SpawnPoolDefinition>();
        junk.Add(pool);
        pool.Configure("empty", "Empty", SpawnCategory.Monster, System.Array.Empty<SpawnEntry>());
        var marker = new RoomMarkerData
        {
            id = "empty",
            kind = MarkerKind.Spawn,
            enabled = true,
            pool = pool,
            minCount = 1,
            maxCount = 2,
            minDifficulty = 1
        };
        SpawnOutcome outcome = SpawnDirector.Spawn(marker, 1, 1, null, null, null, true);
        Check(outcome.Spawned == 0 && !string.IsNullOrEmpty(outcome.Note), "empty pool does not throw", ref failed);
    }

    static RoomDefinition RoomAsset(string id, int width, int height)
    {
        var room = ScriptableObject.CreateInstance<RoomDefinition>();
        room.Configure(id, id, width, height, RoomCategory.Clearing, "forest", null);
        return room;
    }

    static RoomMarkerData Socket(string id, int x, int y, IsoDirection direction, bool required)
    {
        return new RoomMarkerData
        {
            id = id,
            kind = MarkerKind.Socket,
            x = x,
            y = y,
            direction = direction,
            required = required,
            socketTag = "path",
            enabled = true
        };
    }

    static bool Same(LayoutResult a, LayoutResult b)
    {
        if (a.Nodes.Count != b.Nodes.Count || a.Ok != b.Ok)
            return false;

        for (int i = 0; i < a.Nodes.Count; i++)
        {
            if (a.Nodes[i].Room != b.Nodes[i].Room || a.Nodes[i].Origin != b.Nodes[i].Origin)
                return false;
        }

        return true;
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
