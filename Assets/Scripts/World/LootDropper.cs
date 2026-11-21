using UnityEngine;

/// <summary>
/// Spawns item stacks when the body breaks. It does not know which tool landed the hit.
/// </summary>
public class LootDropper : MonoBehaviour, IBreakReaction
{
    [SerializeField] string itemId;
    [SerializeField] int minCount = 1;
    [SerializeField] int maxCount = 1;
    [SerializeField] float scatter = 0.35f;

    bool _dropped;

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
        if (_dropped || string.IsNullOrEmpty(itemId))
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
}
