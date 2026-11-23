using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the terrain palette, demo rooms, pools, and the workshop scene.
/// Re-running refreshes terrain art and pool numbers. Painted rooms are left alone unless forced.
/// </summary>
public static class LevelContentBuilder
{
    const string ArtTiles = "Assets/Art/World/Tiles";
    const string ArtProps = "Assets/Art/World/Props";
    const string Data = "Assets/Data/Levels";
    const string CatalogPath = "Assets/Resources/GameContent/LevelCatalog.asset";
    const string Workshop = "Assets/Scenes/LevelWorkshop.unity";

    struct Spec
    {
        public string Id;
        public string Name;
        public PassageKind Passage;
        public int Height;
        public string Group;
        public Color Color;
        public int Style;
    }

    [MenuItem("Game/Уровни/Создать демо-контент")]
    public static void CreateDefaults()
    {
        CreateDefaults(false);
    }

    [MenuItem("Game/Уровни/Пересоздать демо-комнаты")]
    public static void RecreateRooms()
    {
        CreateDefaults(true);
    }

    public static void CreateDefaults(bool forceRooms)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameContentBuilder.EnsureAssets();
        ItemCatalog.Reload();
        EnsureFolders();

        Dictionary<string, TerrainDefinition> terrains = BuildTerrains();
        TerrainLibrary library = BuildLibrary(terrains);
        GameObject slime = BuildSlime("Slime", 3f, 100f, 10f);
        GameObject tough = BuildSlime("SlimeTough", 4f, 180f, 16f);
        GameObject rock = BuildRock("Rock", "loot.rock", false);
        GameObject copper = BuildRock("CopperVein", "loot.copper_vein", true);
        GameObject tree = BuildTree();
        GameObject bush = BuildBush();

        SpawnPoolDefinition enemies = BuildPool(
            "ForestEnemies",
            "Лесные противники",
            SpawnCategory.Monster,
            new[]
            {
                Entry("slime", slime, 10, 1, 0),
                Entry("slime.tough", tough, 5, 3, 0)
            });
        SpawnPoolDefinition resources = BuildPool(
            "ForestResources",
            "Лесные ресурсы",
            SpawnCategory.Resource,
            new[]
            {
                Entry("rock", rock, 8, 1, 0),
                Entry("tree", tree, 8, 1, 0),
                Entry("copper", copper, 4, 4, 0)
            });

        TerrainDefinition grass = terrains["terrain.grass"];
        RoomDefinition glade = BuildGlade(terrains, enemies, resources, bush, forceRooms);
        RoomDefinition path = BuildPath(terrains, forceRooms);
        RoomDefinition end = BuildEnd(terrains, enemies, forceRooms);
        LevelDefinition level = BuildLevel(glade, path, end, enemies, resources);
        LevelLayoutDefinition layout = BuildLayout(level, forceRooms);
        BuildCatalog(library, level, layout);
        AssetDatabase.SaveAssets();
        try
        {
            EnsureWorkshop(glade, level);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Мастерская сцена не записана: " + error.Message);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Демо-контент уровней записан.");
        AssetDatabase.Refresh();
    }

    static Dictionary<string, TerrainDefinition> BuildTerrains()
    {
        Spec[] specs =
        {
            S("terrain.grass", "Трава", PassageKind.Ground, 0, "grass", new Color(0.34f, 0.58f, 0.28f), 0),
            S("terrain.dirt", "Земля", PassageKind.Ground, 0, "dirt", new Color(0.48f, 0.34f, 0.2f), 1),
            S("terrain.sand", "Песок", PassageKind.Ground, 0, "sand", new Color(0.76f, 0.68f, 0.4f), 2),
            S("terrain.stone", "Камень", PassageKind.Ground, 0, "stone", new Color(0.55f, 0.56f, 0.58f), 3),
            S("terrain.mud", "Грязь", PassageKind.Ground, 0, "mud", new Color(0.36f, 0.28f, 0.18f), 4),
            S("terrain.forest", "Лесная почва", PassageKind.Ground, 0, "forest", new Color(0.24f, 0.4f, 0.22f), 5),
            S("terrain.road", "Грунтовая дорога", PassageKind.Ground, 0, "road", new Color(0.62f, 0.46f, 0.28f), 6),
            S("terrain.road.stone", "Каменная дорога", PassageKind.Ground, 0, "road", new Color(0.66f, 0.66f, 0.64f), 7),
            S("terrain.road.forest", "Лесная тропа", PassageKind.Ground, 0, "road", new Color(0.4f, 0.46f, 0.26f), 8),
            S("terrain.road.high", "Дорога на высоте", PassageKind.Ground, 1, "road", new Color(0.7f, 0.54f, 0.32f), 9),
            S("terrain.water", "Река", PassageKind.Blocked, 0, "water", new Color(0.22f, 0.42f, 0.72f), 10),
            S("terrain.water.shallow", "Мелководье", PassageKind.Blocked, 0, "water", new Color(0.38f, 0.62f, 0.74f), 11),
            S("terrain.water.lake", "Озеро", PassageKind.Blocked, 0, "water", new Color(0.14f, 0.28f, 0.58f), 12),
            S("terrain.shore", "Берег", PassageKind.Ground, 0, "shore", new Color(0.62f, 0.58f, 0.36f), 13),
            S("terrain.rock", "Скала", PassageKind.Blocked, 2, "rock", new Color(0.4f, 0.4f, 0.42f), 14),
            S("terrain.wall", "Каменная стена", PassageKind.Blocked, 2, "wall", new Color(0.32f, 0.32f, 0.34f), 15),
            S("terrain.elevation", "Возвышенность", PassageKind.Ground, 1, "elevation", new Color(0.42f, 0.56f, 0.34f), 16),
            S("terrain.cliff", "Обрыв", PassageKind.Blocked, 1, "cliff", new Color(0.28f, 0.32f, 0.24f), 17),
            S("terrain.slope", "Склон", PassageKind.Ramp, 0, "slope", new Color(0.5f, 0.62f, 0.32f), 18),
            S("terrain.bridge", "Мост", PassageKind.Bridge, 0, "bridge", new Color(0.58f, 0.4f, 0.22f), 19),
            S("terrain.grass.tuft", "Пучок травы", PassageKind.Ground, 0, "tuft", new Color(0.3f, 0.62f, 0.24f), 20)
        };

        var map = new Dictionary<string, TerrainDefinition>();
        for (int i = 0; i < specs.Length; i++)
        {
            Spec spec = specs[i];
            string sheet = ArtTiles + "/" + spec.Id.Replace(".", "_") + ".png";
            Sprite[] sprites = EnsureSheet(sheet, spec.Color, spec.Style);
            TerrainDefinition terrain = Asset(Data + "/Terrains/" + spec.Id.Replace(".", "_") + ".asset", ScriptableObject.CreateInstance<TerrainDefinition>);
            terrain.Configure(spec.Id, spec.Name, spec.Passage, spec.Height, spec.Group, spec.Color);
            TerrainTile tile = Asset(Data + "/Tiles/Tile_" + spec.Id.Replace(".", "_") + ".asset", ScriptableObject.CreateInstance<TerrainTile>);
            tile.Bind(terrain);
            var edges = new Sprite[16];
            var centers = new Sprite[4];
            for (int s = 0; s < sprites.Length && s < 16; s++)
                edges[s] = sprites[s];
            for (int s = 0; s < 4; s++)
                centers[s] = sprites.Length > 16 + s ? sprites[16 + s] : (sprites.Length > 0 ? sprites[0] : null);
            terrain.BindArt(centers, edges, tile);
            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(tile);
            map[spec.Id] = terrain;
        }

        return map;
    }

    static TerrainLibrary BuildLibrary(Dictionary<string, TerrainDefinition> terrains)
    {
        var list = new List<TerrainDefinition>();
        foreach (KeyValuePair<string, TerrainDefinition> pair in terrains)
            list.Add(pair.Value);

        TerrainLibrary library = Asset(Data + "/TerrainLibrary.asset", ScriptableObject.CreateInstance<TerrainLibrary>);
        library.SetTerrains(list.ToArray());
        EditorUtility.SetDirty(library);
        return library;
    }

    static RoomDefinition BuildGlade(
        Dictionary<string, TerrainDefinition> terrains,
        SpawnPoolDefinition enemies,
        SpawnPoolDefinition resources,
        GameObject bush,
        bool force)
    {
        RoomDefinition room = Room("Room_ForestGlade", "room.forest.glade", "Лесная поляна", 14, 10, RoomCategory.River, "forest", terrains["terrain.grass"], force);
        if (!force && room.Markers.Count > 0)
            return room;

        room.ClearAuthored();
        Fill(room, 9, 0, 10, 9, RoomLayer.Ground, terrains["terrain.water"]);
        Fill(room, 8, 0, 8, 9, RoomLayer.Ground, terrains["terrain.shore"]);
        Fill(room, 11, 0, 11, 9, RoomLayer.Ground, terrains["terrain.shore"]);
        Fill(room, 0, 4, 13, 4, RoomLayer.Ground, terrains["terrain.road"]);
        room.SetTile(9, 4, RoomLayer.Ground, terrains["terrain.bridge"]);
        room.SetTile(10, 4, RoomLayer.Ground, terrains["terrain.bridge"]);
        Fill(room, 1, 6, 4, 8, RoomLayer.Ground, terrains["terrain.elevation"]);
        Fill(room, 1, 5, 4, 5, RoomLayer.Ground, terrains["terrain.slope"]);
        room.SetTile(2, 2, RoomLayer.Terrain, terrains["terrain.rock"]);
        room.SetTile(5, 7, RoomLayer.Details, terrains["terrain.grass.tuft"]);
        room.AddMarker(Socket("glade.sw", 0, 4, IsoDirection.SouthWest, false));
        room.AddMarker(Socket("glade.ne", 13, 4, IsoDirection.NorthEast, true));
        room.AddMarker(Spawn("monsters", 3, 4, enemies, SpawnCategory.Monster, 1, 2, 2f, null));
        room.AddMarker(Spawn("resources", 6, 2, resources, SpawnCategory.Resource, 1, 2, 2f, new[] { "terrain.grass", "terrain.forest", "terrain.dirt" }));
        room.AddMarker(Prop("bush", 1, 1, bush, MarkerKind.Decor));
        EditorUtility.SetDirty(room);
        return room;
    }

    static RoomDefinition BuildPath(Dictionary<string, TerrainDefinition> terrains, bool force)
    {
        RoomDefinition room = Room("Room_ForestPath", "room.forest.path", "Лесной коридор", 8, 5, RoomCategory.Corridor, "forest", terrains["terrain.grass"], force);
        if (!force && room.Markers.Count > 0)
            return room;

        room.ClearAuthored();
        Fill(room, 0, 2, 7, 2, RoomLayer.Ground, terrains["terrain.road"]);
        room.AddMarker(Socket("path.sw", 0, 2, IsoDirection.SouthWest, true));
        room.AddMarker(Socket("path.ne", 7, 2, IsoDirection.NorthEast, true));
        EditorUtility.SetDirty(room);
        return room;
    }

    static RoomDefinition BuildEnd(Dictionary<string, TerrainDefinition> terrains, SpawnPoolDefinition enemies, bool force)
    {
        RoomDefinition room = Room("Room_ForestEnd", "room.forest.end", "Каменная комната", 8, 8, RoomCategory.Boss, "forest", terrains["terrain.stone"], force);
        if (!force && room.Markers.Count > 0)
            return room;

        room.ClearAuthored();
        room.AddMarker(Socket("end.sw", 0, 3, IsoDirection.SouthWest, true));
        room.AddMarker(Spawn("end.monsters", 4, 4, enemies, SpawnCategory.Elite, 1, 1, 1.5f, null));
        EditorUtility.SetDirty(room);
        return room;
    }

    static LevelDefinition BuildLevel(
        RoomDefinition glade,
        RoomDefinition path,
        RoomDefinition end,
        SpawnPoolDefinition enemies,
        SpawnPoolDefinition resources)
    {
        LevelDefinition level = Asset(Data + "/Level_Forest.asset", ScriptableObject.CreateInstance<LevelDefinition>);
        level.Configure(
            "forest",
            "Лес",
            "forest",
            glade,
            end,
            new[] { glade, path, end },
            new[] { path },
            3,
            3,
            false,
            enemies,
            resources);
        EditorUtility.SetDirty(level);
        return level;
    }

    static LevelLayoutDefinition BuildLayout(LevelDefinition level, bool force)
    {
        LevelLayoutDefinition layout = Asset(Data + "/Layouts/Layout_Forest.asset", ScriptableObject.CreateInstance<LevelLayoutDefinition>);
        layout.Configure("layout.forest", level, 1);
        if (!force && layout.Placements.Count > 0)
        {
            EditorUtility.SetDirty(layout);
            return layout;
        }

        LayoutResult built = RoomLayoutBuilder.Build(level, 1);
        var placements = new List<LayoutPlacement>();
        for (int i = 0; i < built.Nodes.Count; i++)
        {
            placements.Add(new LayoutPlacement
            {
                room = built.Nodes[i].Room,
                origin = built.Nodes[i].Origin
            });
        }

        layout.SetPlacements(placements, 1);
        EditorUtility.SetDirty(layout);
        if (!built.Ok)
            Debug.LogWarning(string.Join("\n", built.Problems));
        return layout;
    }

    static void BuildCatalog(TerrainLibrary library, LevelDefinition level, LevelLayoutDefinition layout)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/GameContent");
        LevelCatalog catalog = Asset(CatalogPath, ScriptableObject.CreateInstance<LevelCatalog>);
        catalog.Set(new[] { level }, library, layout);
        EditorUtility.SetDirty(catalog);
    }

    static void EnsureWorkshop(RoomDefinition glade, LevelDefinition level)
    {
        if (File.Exists(Workshop))
            return;

        Scene active = SceneManager.GetActiveScene();
        bool replace = Application.isBatchMode || string.IsNullOrEmpty(active.path);
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            replace ? NewSceneMode.Single : NewSceneMode.Additive);
        var roomObject = new GameObject("Forest Glade");
        SceneManager.MoveGameObjectToScene(roomObject, scene);
        RoomDocument document = roomObject.AddComponent<RoomDocument>();
        document.Assign(glade, level);
        document.SetSuppressProceduralWorld(true);
        document.SetSeed(1);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab");
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = TerrainCoordinates.CellCenterWorld(3, 2);
            var cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.4375f;
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.08f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene, Workshop);
    }

    static RoomDefinition Room(
        string file,
        string id,
        string name,
        int width,
        int height,
        RoomCategory category,
        string biome,
        TerrainDefinition ground,
        bool force)
    {
        string path = Data + "/Rooms/" + file + ".asset";
        bool created = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path) == null;
        RoomDefinition room = Asset(path, ScriptableObject.CreateInstance<RoomDefinition>);
        if (created || force || string.IsNullOrEmpty(room.Id))
            room.Configure(id, name, width, height, category, biome, ground);
        room.SetDefaultGround(ground);
        EditorUtility.SetDirty(room);
        return room;
    }

    static void Fill(RoomDefinition room, int x0, int y0, int x1, int y1, RoomLayer layer, TerrainDefinition terrain)
    {
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
                room.SetTile(x, y, layer, terrain);
        }
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

    static RoomMarkerData Spawn(
        string id,
        int x,
        int y,
        SpawnPoolDefinition pool,
        SpawnCategory category,
        int min,
        int max,
        float radius,
        string[] surfaces)
    {
        return new RoomMarkerData
        {
            id = id,
            kind = MarkerKind.Spawn,
            x = x,
            y = y,
            pool = pool,
            category = category,
            minCount = min,
            maxCount = max,
            radius = radius,
            minDifficulty = 1,
            enabled = true,
            allowedSurfaceIds = surfaces ?? System.Array.Empty<string>()
        };
    }

    static RoomMarkerData Prop(string id, int x, int y, GameObject prefab, MarkerKind kind)
    {
        return new RoomMarkerData
        {
            id = id,
            kind = kind,
            x = x,
            y = y,
            prefab = prefab,
            enabled = true
        };
    }

    static SpawnEntry Entry(string id, GameObject prefab, int weight, int min, int max)
    {
        return new SpawnEntry
        {
            id = id,
            enabled = true,
            weight = weight,
            prefab = prefab,
            minDifficulty = min,
            maxDifficulty = max
        };
    }

    static SpawnPoolDefinition BuildPool(string file, string name, SpawnCategory category, SpawnEntry[] entries)
    {
        SpawnPoolDefinition pool = Asset(Data + "/Pools/" + file + ".asset", ScriptableObject.CreateInstance<SpawnPoolDefinition>);
        pool.Configure(file, name, category, entries);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    static GameObject BuildSlime(string name, float scale, float health, float damage)
    {
        string path = "Assets/Prefabs/World/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        go.AddComponent<SpriteRenderer>();
        Slime slime = go.AddComponent<Slime>();
        var so = new SerializedObject(slime);
        so.FindProperty("artScale").floatValue = scale;
        SerializedProperty stats = so.FindProperty("stats");
        if (stats != null)
        {
            stats.FindPropertyRelative("maxHealth").floatValue = health;
            stats.FindPropertyRelative("damage").floatValue = damage;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildRock(string name, string lootId, bool copper)
    {
        string path = "Assets/Prefabs/World/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        LootTable loot = copper ? EnsureCopperLoot() : AssetDatabase.LoadAssetAtPath<LootTable>("Assets/Resources/GameContent/Loot_Rock.asset");
        Sprite sprite = EnsureProp(copper ? "Copper" : "Rock", copper ? 2 : 1, 28, 20);
        var go = new GameObject(name);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        go.AddComponent<BillboardSort>();
        HarvestNode node = go.AddComponent<HarvestNode>();
        var so = new SerializedObject(node);
        so.FindProperty("materialId").stringValue = "stone";
        so.FindProperty("integrity").floatValue = HarvestNode.DefaultIntegrity;
        so.FindProperty("bodyHeight").floatValue = 0.7f;
        so.FindProperty("slop").floatValue = 0.85f;
        so.FindProperty("loot").objectReferenceValue = loot;
        so.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildTree()
    {
        string path = "Assets/Prefabs/World/Tree.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var go = new GameObject("Tree");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = EnsureProp("Tree", 0, 40, 72);
        go.AddComponent<BillboardSort>();
        go.AddComponent<ForestTree>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildBush()
    {
        string path = "Assets/Prefabs/World/Bush.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var go = new GameObject("Bush");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = EnsureProp("Bush", 3, 24, 18);
        go.AddComponent<BillboardSort>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static LootTable EnsureCopperLoot()
    {
        const string path = "Assets/Data/Loot/Loot_CopperVein.asset";
        LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootTable>();
            AssetDatabase.CreateAsset(table, path);
        }

        ItemDefinition ore = ItemCatalog.Find(ItemIds.CopperOre);
        ItemDefinition stone = ItemCatalog.Find(ItemIds.Stone);
        table.Configure("loot.copper_vein", new[]
        {
            new LootDrop { Item = ore, Min = 1, Max = 2, Chance = 1f },
            new LootDrop { Item = stone, Min = 1, Max = 1, Chance = 0.5f }
        });
        EditorUtility.SetDirty(table);
        return table;
    }

    static Sprite[] EnsureSheet(string path, Color color, int style)
    {
        if (!File.Exists(path))
            TerrainPlaceholderArt.WriteSheet(path, color, style);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spriteImportMode = SpriteImportMode.Multiple;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider data = factory.GetSpriteEditorDataProviderFromObject(importer);
        data.InitSpriteEditorDataProvider();

        var names = data.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var known = new Dictionary<string, GUID>();
        if (names != null)
        {
            foreach (SpriteNameFileIdPair pair in names.GetNameFileIdPairs())
            {
                if (pair != null && !string.IsNullOrEmpty(pair.name))
                    known[pair.name] = pair.GetFileGUID();
            }
        }

        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new SpriteRect[TerrainPlaceholderArt.Slices];
        var pairs = new SpriteNameFileIdPair[rects.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            string slice = baseName + "_" + i.ToString("00");
            GUID id;
            if (!known.TryGetValue(slice, out id) || id.Empty())
                id = GUID.Generate();

            rects[i] = new SpriteRect
            {
                name = slice,
                rect = new Rect(i * TerrainPlaceholderArt.TileWidth, 0f, TerrainPlaceholderArt.TileWidth, TerrainPlaceholderArt.TileHeight),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = id
            };
            pairs[i] = new SpriteNameFileIdPair(slice, id);
        }

        data.SetSpriteRects(rects);
        if (names != null)
            names.SetNameFileIdPairs(pairs);
        data.Apply();
        importer.SaveAndReimport();
        return LoadSprites(path);
    }

    static Sprite EnsureProp(string name, int kind, int width, int height)
    {
        string path = ArtProps + "/" + name + ".png";
        if (!File.Exists(path))
            TerrainPlaceholderArt.WriteProp(path, width, height, kind);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0.08f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite[] LoadSprites(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                sprites.Add(sprite);
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    static T Asset<T>(string path, System.Func<T> create) where T : Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
            return existing;

        T created = create();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static Spec S(string id, string name, PassageKind passage, int height, string group, Color color, int style)
    {
        return new Spec
        {
            Id = id,
            Name = name,
            Passage = passage,
            Height = height,
            Group = group,
            Color = color,
            Style = style
        };
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/World");
        EnsureFolder(ArtTiles);
        EnsureFolder(ArtProps);
        EnsureFolder("Assets/Data");
        EnsureFolder(Data);
        EnsureFolder(Data + "/Terrains");
        EnsureFolder(Data + "/Tiles");
        EnsureFolder(Data + "/Rooms");
        EnsureFolder(Data + "/Pools");
        EnsureFolder(Data + "/Layouts");
        EnsureFolder("Assets/Data/Loot");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/World");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
