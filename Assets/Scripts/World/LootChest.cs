using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A container opened by the shared interact button. The reward is rolled once and then remembered.
/// </summary>
public class LootChest : MonoBehaviour, IInteractable
{
    [SerializeField] string worldId = "chest";
    [SerializeField] string label = "Сундук";
    [SerializeField] LootTable loot;
    [SerializeField] ItemRarity maxRarity = ItemRarity.VeryRare;
    [SerializeField] int difficulty;
    [SerializeField] float radius = 1.6f;

    bool _open;
    int _grants;
    SpriteRenderer _renderer;
    Color _closed = new Color(0.62f, 0.42f, 0.18f, 1f);

    public bool CanInteract => !_open && isActiveAndEnabled;
    public Vector2 Origin => transform.position;
    public float Radius => radius;
    public string Label => label;
    public string Verb => "Открыть";
    public string Refusal => _open ? "Уже открыт" : string.Empty;
    public bool IsOpen => _open;
    public int GrantCount => _grants;

    public void Configure(string id, string caption, LootTable table, ItemRarity cap)
    {
        worldId = id;
        label = caption;
        loot = table;
        maxRarity = cap;
        ApplySaved();
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _closed = _renderer.color;
        ApplySaved();
    }

    void Start()
    {
        if (LevelWorld.Instance != null)
            LevelWorld.Instance.NoteInterest(transform.position, MapPinKind.Chest);
    }

    void OnEnable()
    {
        Interactables.Register(this);
        ApplySaved();
    }

    void OnDisable()
    {
        Interactables.Unregister(this);
    }

    public void Interact(InteractionContext context)
    {
        if (_open)
            return;

        _open = true;
        if (LocationState.Active != null)
        {
            LocationState.Active.MarkOpened(worldId);
            WorldMemory.Store(LocationState.Active);
        }

        if (loot != null)
        {
            var grants = new List<LootGrant>();
            int seed = StableSeed(worldId);
            loot.Roll(grants, new SeededLootRandom(seed), difficulty, maxRarity);
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.4f;
            for (int i = 0; i < grants.Count; i++)
            {
                Vector2 land = (Vector2)transform.position + new Vector2(0.3f * i, 0.15f);
                WorldDropper.Spawn(grants[i].ItemId, grants[i].Count, origin, land, 0.1f);
            }

            _grants = grants.Count;
            Fx.Play(FxEvent.Chest, transform.position);
            for (int i = 0; i < grants.Count; i++)
            {
                ItemDefinition granted = ItemCatalog.Find(grants[i].ItemId);
                if (granted != null && granted.Rarity >= ItemRarity.Rare)
                {
                    Fx.Play(FxEvent.Rare, transform.position);
                    break;
                }
            }
        }
        else
        {
            Fx.Play(FxEvent.Chest, transform.position);
        }

        Notices.Post(NoticeKind.Chest, worldId, _grants, 1);
        Paint();
    }

    void ApplySaved()
    {
        if (LocationState.Active != null && LocationState.Active.WasOpened(worldId))
            _open = true;
        Paint();
    }

    void Paint()
    {
        if (_renderer == null)
            return;

        _renderer.color = _open ? new Color(0.35f, 0.24f, 0.12f, 1f) : _closed;
    }

    public static int StableSeed(string id)
    {
        if (string.IsNullOrEmpty(id))
            return 1;

        int hash = 17;
        for (int i = 0; i < id.Length; i++)
            hash = hash * 31 + id[i];
        return hash;
    }
}
