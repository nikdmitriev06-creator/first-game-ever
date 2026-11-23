using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode checks for loot rarity, chests, switches and creature orders.
/// Run with Unity -executeMethod WorldChecks.Run
/// </summary>
public static class WorldChecks
{
    [MenuItem("Game/Проверить мир")]
    public static void Run()
    {
        int failed = 0;
        var created = new List<Object>();
        try
        {
            GameContentBuilder.EnsureAssets();
            ItemCatalog.Reload();
            Interactables.ResetStatics();
            ImpactLibrary.Ensure();
            MaterialLibrary.Ensure();
            CheckMind(ref failed);
            CheckRarity(created, ref failed);
            CheckChest(created, ref failed);
            CheckSwitch(created, ref failed);
            CheckNearest(created, ref failed);
            CheckBreak(created, ref failed);
            CheckMemory(ref failed);
        }
        catch (System.Exception error)
        {
            failed++;
            Debug.LogError("World check crashed: " + error);
        }
        finally
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
        }

        if (failed == 0)
            Debug.Log("World checks passed.");
        else
            Debug.LogError("World checks failed: " + failed);

        if (Application.isBatchMode)
            EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    static void CheckMind(ref int failed)
    {
        Check(CreatureMind.Choose(8f, 1f, 6f, 10f, 1.2f, 7f, false) == MindOrder.Detect, "a new target is noticed", ref failed);
        Check(CreatureMind.Choose(4f, 1f, 6f, 10f, 1.2f, 7f, true) == MindOrder.Chase, "a seen target is chased", ref failed);
        Check(CreatureMind.Choose(0.4f, 1f, 6f, 10f, 1.2f, 7f, true) == MindOrder.Attack, "attack range stops the chase hop", ref failed);
        Check(CreatureMind.Choose(12f, 1f, 6f, 10f, 1.2f, 7f, true) == MindOrder.Return, "a lost target sends the creature home", ref failed);
        Check(CreatureMind.Choose(2f, 9f, 6f, 10f, 1.2f, 7f, true) == MindOrder.Return, "the leash ends an endless chase", ref failed);
        Check(CreatureMind.Choose(20f, 0.2f, 6f, 10f, 1.2f, 7f, false) == MindOrder.Idle, "an unseen far player leaves the creature idle", ref failed);
    }

    static void CheckRarity(List<Object> created, ref int failed)
    {
        ItemDefinition crystal = ItemCatalog.Find(ItemIds.Crystal);
        ItemDefinition relic = ItemCatalog.Find(ItemIds.Relic);
        ItemDefinition ore = ItemCatalog.Find(ItemIds.CopperOre);
        Check(crystal != null && crystal.Rarity == ItemRarity.Rare, "crystal shard is rare", ref failed);
        Check(relic != null && relic.Rarity == ItemRarity.VeryRare, "relic is very rare", ref failed);
        Check(ore != null && ore.Rarity == ItemRarity.Uncommon, "copper ore is uncommon", ref failed);

        LootTable table = ScriptableObject.CreateInstance<LootTable>();
        created.Add(table);
        table.Configure("test.rarity", new[]
        {
            new LootDrop { Item = crystal, Min = 1, Max = 1, Chance = 1f },
            new LootDrop { Item = relic, Min = 1, Max = 1, Chance = 1f, MinDifficulty = 3 }
        });
        var grants = new List<LootGrant>();
        table.Roll(grants, new SeededLootRandom(1), 1, ItemRarity.Rare);
        Check(grants.Count == 1 && grants[0].ItemId == ItemIds.Crystal, "a common cap and low difficulty hide the relic", ref failed);
        table.Roll(grants, new SeededLootRandom(1), 3, ItemRarity.VeryRare);
        Check(grants.Count == 2, "the relic appears when difficulty and rarity allow it", ref failed);
    }

    static void CheckChest(List<Object> created, ref int failed)
    {
        LocationState.Reset();
        var go = new GameObject("Chest");
        created.Add(go);
        go.AddComponent<SpriteRenderer>();
        LootChest chest = go.AddComponent<LootChest>();
        chest.Configure("test.chest", "Сундук", null, ItemRarity.Common);
        chest.Interact(new InteractionContext());
        Check(chest.IsOpen && !chest.CanInteract, "a chest opens once", ref failed);
        chest.Interact(new InteractionContext());
        Check(chest.GrantCount == 0, "a second open does not roll again", ref failed);

        var again = new GameObject("Chest Again");
        created.Add(again);
        again.AddComponent<SpriteRenderer>();
        LootChest saved = again.AddComponent<LootChest>();
        saved.Configure("test.chest", "Сундук", null, ItemRarity.Common);
        Check(saved.IsOpen, "the same chest id stays open", ref failed);
    }

    static void CheckSwitch(List<Object> created, ref int failed)
    {
        LocationState.Reset();
        var leverObject = new GameObject("Lever");
        var doorObject = new GameObject("Door");
        created.Add(leverObject);
        created.Add(doorObject);
        leverObject.AddComponent<SpriteRenderer>();
        doorObject.AddComponent<SpriteRenderer>();
        WorldSwitch lever = leverObject.AddComponent<WorldSwitch>();
        WorldSwitch door = doorObject.AddComponent<WorldSwitch>();
        lever.Configure(WorldSwitch.Kind.Lever, "test.lever", "test.link", "Рычаг");
        door.Configure(WorldSwitch.Kind.Door, "test.door", "test.link", "Дверь");
        Check(!door.CanInteract, "a door is not pressed by itself", ref failed);
        lever.Interact(new InteractionContext());
        Check(lever.IsOn && door.IsOn, "a lever opens the linked door", ref failed);
        lever.Interact(new InteractionContext());
        Check(!lever.IsOn && !door.IsOn, "a second press closes the door", ref failed);
    }

    static void CheckNearest(List<Object> created, ref int failed)
    {
        Interactables.ResetStatics();
        var near = new GameObject("Near");
        var far = new GameObject("Far");
        created.Add(near);
        created.Add(far);
        near.transform.position = new Vector3(1f, 0f, 0f);
        far.transform.position = new Vector3(4f, 0f, 0f);
        near.AddComponent<SpriteRenderer>();
        far.AddComponent<SpriteRenderer>();
        WorldSwitch close = near.AddComponent<WorldSwitch>();
        WorldSwitch distant = far.AddComponent<WorldSwitch>();
        close.Configure(WorldSwitch.Kind.Campfire, "near.fire", "near", "Ближний");
        distant.Configure(WorldSwitch.Kind.Campfire, "far.fire", "far", "Дальний");
        IInteractable found = Interactables.Nearest(Vector2.zero, 3f);
        Check((Object)found == close, "the nearer object is chosen", ref failed);
        Check(Interactables.Nearest(Vector2.zero, 0.2f) == null, "nothing is chosen out of reach", ref failed);
    }

    static void CheckBreak(List<Object> created, ref int failed)
    {
        var go = new GameObject("Crate");
        created.Add(go);
        go.AddComponent<SpriteRenderer>();
        Destructible body = Destructible.Create(go, MaterialLibrary.Furniture, 2f, 0.7f, 0.5f);
        body.Allow(ImpactLibrary.Slash, 1f);
        body.AssignId("test.crate");
        go.AddComponent<BreakWear>();
        ImpactResult slash = body.Receive(new Impact(ImpactLibrary.Slash, 1f, Vector2.zero, Vector2.right, null));
        Check(slash.Outcome == ImpactOutcome.Damaged && body.Integrity < body.MaxIntegrity, "a weapon chips a crate", ref failed);
        var wall = new GameObject("Seal");
        created.Add(wall);
        Destructible seal = Destructible.Create(wall, MaterialLibrary.Stone, 4f, 1f, 0.5f);
        seal.Seal();
        Check(seal.Receive(new Impact(ImpactLibrary.Mine, 5f, Vector2.zero, Vector2.up, null)).Outcome == ImpactOutcome.Ignored, "a sealed slab ignores the pick", ref failed);
    }

    static void CheckMemory(ref int failed)
    {
        var state = new LocationState();
        state.ApplyFlags(new[] { "rock.1" }, new[] { "chest.1" }, new[] { "door.1" });
        Check(state.WasRemoved("rock.1") && state.WasOpened("chest.1") && state.WasEnabled("door.1"), "world flags restore", ref failed);
        var removed = new List<string>();
        var opened = new List<string>();
        var enabled = new List<string>();
        state.CopyFlags(removed, opened, enabled);
        var copy = new LocationState();
        copy.ApplyFlags(removed, opened, enabled);
        Check(copy.WasOpened("chest.1") && !copy.WasOpened("other"), "copied flags keep an open chest", ref failed);
    }

    static void Check(bool condition, string label, ref int failed)
    {
        if (condition)
            return;

        failed++;
        Debug.LogError("World check failed: " + label);
    }
}
