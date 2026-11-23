using UnityEngine;

/// <summary>
/// One breakable world body described by a material, an integrity, and a loot table.
/// A new rock type is another material or another table on this same component.
/// </summary>
public class HarvestNode : MonoBehaviour
{
    public const float DefaultIntegrity = 4f;

    [SerializeField] MaterialDefinition material;
    [SerializeField] string materialId;
    [SerializeField] float integrity = DefaultIntegrity;
    [SerializeField] float bodyHeight = 0.7f;
    [SerializeField] float slop = 0.85f;
    [SerializeField] LootTable loot;

    public Destructible Body => GetComponent<Destructible>();
    public LootTable Loot => loot;

    public void UseMaterialId(string id)
    {
        materialId = id;
        if (material == null)
            Apply();
    }

    public void Configure(MaterialDefinition bodyMaterial, float hits, float height, float aimSlop, LootTable drops)
    {
        material = bodyMaterial;
        integrity = hits;
        bodyHeight = height;
        slop = aimSlop;
        loot = drops;
        Apply();
    }

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        if (material == null && !string.IsNullOrEmpty(materialId))
            material = MaterialLibrary.Named(materialId);

        if (material == null)
            return;

        Destructible.Create(gameObject, material, integrity, bodyHeight, slop);

        LootDropper dropper = GetComponent<LootDropper>();
        if (dropper == null)
            dropper = gameObject.AddComponent<LootDropper>();
        if (loot != null)
            dropper.UseTable(loot);

        // Loot is added first, so the drop runs before the body is removed.
        if (GetComponent<VanishOnBreak>() == null)
            gameObject.AddComponent<VanishOnBreak>();
        if (GetComponent<BreakWear>() == null)
            gameObject.AddComponent<BreakWear>();
    }
}
