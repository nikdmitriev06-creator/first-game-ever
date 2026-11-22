using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns item stacks when the body breaks. A loot table can list several items.
/// The single item fields remain for bodies that drop one stack, such as a tree.
/// </summary>
public class LootDropper : MonoBehaviour, IBreakReaction
{
    [SerializeField] LootTable table;
    [SerializeField] string itemId;
    [SerializeField] int minCount = 1;
    [SerializeField] int maxCount = 1;
    [SerializeField] float scatter = 0.35f;

    readonly List<LootGrant> _grants = new List<LootGrant>();
    ILootRandom _random;
    bool _dropped;

    public void UseTable(LootTable drops)
    {
        if (drops == null)
            return;

        table = drops;
        _dropped = false;
    }

    public void UseRandom(ILootRandom random)
    {
        _random = random;
    }

    public void Configure(string id, int min, int max, float radius)
    {
        itemId = id;
        minCount = Mathf.Max(1, min);
        maxCount = Mathf.Max(minCount, max);
        scatter = Mathf.Max(0f, radius);
        _dropped = false;
    }

    public void OnBroken(in Impact impact)
    {
        if (_dropped)
            return;

        if (table != null)
        {
            _dropped = true;
            DropTable();
            return;
        }

        if (string.IsNullOrEmpty(itemId))
            return;

        _dropped = true;
        int count = Random.Range(minCount, maxCount + 1);
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
        for (int i = 0; i < count; i++)
        {
            float turn = count <= 1 ? 0f : (i / (float)count) * Mathf.PI * 2f;
            float distance = count <= 1 ? Random.Range(0.05f, scatter) : Random.Range(scatter * 0.45f, scatter);
            Vector2 land = (Vector2)transform.position + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn) * 0.62f) * distance;
            WorldDropper.Spawn(itemId, 1, origin, land, 0.12f);
        }
    }

    void DropTable()
    {
        table.Roll(_grants, _random);
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
        int total = _grants.Count;
        for (int i = 0; i < total; i++)
        {
            LootGrant grant = _grants[i];
            float turn = total <= 1 ? 0.4f : (i / (float)total) * Mathf.PI * 2f;
            float distance = scatter * (0.65f + 0.25f * i);
            Vector2 land = (Vector2)transform.position + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn) * 0.62f) * distance;
            WorldDropper.Spawn(grant.ItemId, grant.Count, origin, land, 0.12f);
        }
    }
}
