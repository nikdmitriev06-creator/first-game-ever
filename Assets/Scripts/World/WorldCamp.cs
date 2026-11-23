using UnityEngine;

/// <summary>
/// A small camp beside the forest entrance: a crate, chests, a levered door, a fire, rubble and a crystal.
/// Ids come from the camp position so a second visit keeps the same save keys.
/// </summary>
public static class WorldCamp
{
    public static void Place(Transform parent, Vector2 origin)
    {
        if (parent == null)
            return;

        MaterialLibrary.Ensure();
        ImpactLibrary.Ensure();
        LootLibrary.Ensure();
        ItemCatalog.Ensure();
        string key = Mathf.RoundToInt(origin.x) + "." + Mathf.RoundToInt(origin.y);
        string link = "camp.gate." + key;

        Crate(parent, origin + new Vector2(3.2f, 0.4f), "camp.crate." + key);
        Chest(parent, origin + new Vector2(4.6f, 0.2f), "camp.chest." + key, "Сундук", ItemRarity.Uncommon);
        Chest(parent, origin + new Vector2(5.8f, 1.1f), "camp.rare." + key, "Редкий сундук", ItemRarity.VeryRare);
        Switch(parent, origin + new Vector2(2.2f, 1.6f), "camp.lever." + key, link, WorldSwitch.Kind.Lever, "Рычаг", new Color(0.45f, 0.42f, 0.32f, 1f));
        Switch(parent, origin + new Vector2(2.2f, 3.1f), "camp.door." + key, link, WorldSwitch.Kind.Door, "Дверь", new Color(0.42f, 0.3f, 0.18f, 1f));
        Switch(parent, origin + new Vector2(6.4f, -0.4f), "camp.fire." + key, "camp.fire." + key, WorldSwitch.Kind.Campfire, "Костёр", new Color(0.35f, 0.22f, 0.16f, 1f));
        Node(parent, origin + new Vector2(-2.4f, 1.2f), "camp.rubble." + key, "Завал", MaterialLibrary.Stone, false, false);
        Node(parent, origin + new Vector2(-3.4f, 2.2f), "camp.crystal." + key, "Кристалл", MaterialLibrary.Stone, true, false);
        Node(parent, origin + new Vector2(1.2f, 3.4f), "camp.seal." + key, "Плита", MaterialLibrary.Stone, false, true);
    }

    static void Crate(Transform parent, Vector2 point, string id)
    {
        if (Gone(id))
            return;

        GameObject go = Body(parent, point, "Crate", new Color(0.62f, 0.42f, 0.22f, 1f), new Vector2(0.8f, 0.7f));
        HarvestNode node = go.AddComponent<HarvestNode>();
        node.Configure(MaterialLibrary.Furniture, 2f, 0.7f, 0.6f, null);
        Destructible body = node.Body;
        if (body != null)
        {
            body.AssignId(id);
            body.Allow(ImpactLibrary.Slash, 1f);
        }

        LootDropper dropper = go.GetComponent<LootDropper>();
        if (dropper != null)
            dropper.Configure(ItemIds.Wood, 1, 3, 0.4f);
    }

    static void Chest(Transform parent, Vector2 point, string id, string caption, ItemRarity cap)
    {
        GameObject go = Body(parent, point, caption, new Color(0.55f, 0.36f, 0.16f, 1f), new Vector2(0.9f, 0.6f));
        LootChest chest = go.AddComponent<LootChest>();
        chest.Configure(id, caption, CapTable(cap), cap);
    }

    static void Switch(Transform parent, Vector2 point, string id, string link, WorldSwitch.Kind kind, string caption, Color color)
    {
        GameObject go = Body(parent, point, caption, color, kind == WorldSwitch.Kind.Door ? new Vector2(0.35f, 1.3f) : new Vector2(0.55f, 0.55f));
        WorldSwitch linkBody = go.AddComponent<WorldSwitch>();
        linkBody.Configure(kind, id, link, caption);
    }

    static void Node(Transform parent, Vector2 point, string id, string name, MaterialDefinition material, bool crystal, bool blocked)
    {
        if (Gone(id))
            return;

        Color color = crystal ? new Color(0.45f, 0.75f, 0.95f, 1f) : new Color(0.45f, 0.46f, 0.48f, 1f);
        GameObject go = Body(parent, point, name, color, new Vector2(0.9f, 0.7f));
        HarvestNode node = go.AddComponent<HarvestNode>();
        node.Configure(material, crystal ? 3f : 4f, 0.7f, 0.7f, crystal ? CrystalTable() : LootLibrary.Rock);
        Destructible body = node.Body;
        if (body == null)
            return;

        body.AssignId(id);
        if (crystal && Application.isPlaying)
        {
            LocalLight glow = go.AddComponent<LocalLight>();
            glow.Configure(LocalLight.Role.Magic, new Color(0.55f, 0.82f, 1f, 1f), 2.4f, 0.55f, false);
        }
        if (blocked)
            body.Seal();
    }

    static bool Gone(string id)
    {
        return LocationState.Active != null && LocationState.Active.WasRemoved(id);
    }

    static GameObject Body(Transform parent, Vector2 point, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(point.x, point.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Pixel();
        renderer.color = color;
        renderer.sortingOrder = IsoSort.Order(point.y);
        BillboardSort sort = go.AddComponent<BillboardSort>();
        sort.Follow = true;
        return go;
    }

    static LootTable _common;
    static LootTable _rare;
    static LootTable _crystal;

    static LootTable CapTable(ItemRarity cap)
    {
        if (cap >= ItemRarity.Rare)
            return RareTable();

        if (_common != null)
            return _common;

        _common = ScriptableObject.CreateInstance<LootTable>();
        _common.hideFlags = HideFlags.HideAndDontSave;
        _common.Configure("loot.camp.common", new[]
        {
            new LootDrop { Item = ItemCatalog.Find(ItemIds.Wood), Min = 2, Max = 4, Chance = 1f },
            new LootDrop { Item = ItemCatalog.Find(ItemIds.CopperOre), Min = 1, Max = 1, Chance = 0.4f }
        });
        return _common;
    }

    static LootTable RareTable()
    {
        if (_rare != null)
            return _rare;

        _rare = ScriptableObject.CreateInstance<LootTable>();
        _rare.hideFlags = HideFlags.HideAndDontSave;
        _rare.Configure("loot.camp.rare", new[]
        {
            new LootDrop { Item = ItemCatalog.Find(ItemIds.Crystal), Min = 1, Max = 2, Chance = 1f },
            new LootDrop { Item = ItemCatalog.Find(ItemIds.Relic), Min = 1, Max = 1, Chance = 0.08f }
        });
        return _rare;
    }

    static LootTable CrystalTable()
    {
        if (_crystal != null)
            return _crystal;

        _crystal = ScriptableObject.CreateInstance<LootTable>();
        _crystal.hideFlags = HideFlags.HideAndDontSave;
        _crystal.Configure("loot.crystal", new[]
        {
            new LootDrop { Item = ItemCatalog.Find(ItemIds.Crystal), Min = 1, Max = 2, Chance = 1f },
            new LootDrop { Item = ItemCatalog.Find(ItemIds.Relic), Min = 1, Max = 1, Chance = 0.05f, MinDifficulty = 3 }
        });
        return _crystal;
    }

    static Sprite _pixel;

    static Sprite Pixel()
    {
        if (_pixel != null)
            return _pixel;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        _pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.2f), 1f);
        _pixel.hideFlags = HideFlags.HideAndDontSave;
        return _pixel;
    }
}
