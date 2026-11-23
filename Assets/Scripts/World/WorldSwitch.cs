using UnityEngine;

/// <summary>
/// A lever, a door, or a campfire. Levers push every switch that shares their link id.
/// </summary>
public class WorldSwitch : MonoBehaviour, IInteractable
{
    public enum Kind
    {
        Lever,
        Door,
        Campfire
    }

    [SerializeField] Kind kind;
    [SerializeField] string worldId = "switch";
    [SerializeField] string linkId = "link";
    [SerializeField] string label = "Рычаг";
    [SerializeField] float radius = 1.5f;

    bool _on;
    SpriteRenderer _renderer;
    LocalLight _glow;

    public bool CanInteract => isActiveAndEnabled && kind != Kind.Door;
    public Vector2 Origin => transform.position;
    public float Radius => radius;
    public string Label => label;
    public string Verb => kind == Kind.Campfire ? "Зажечь" : "Нажать";
    public string Refusal => kind == Kind.Door ? (_on ? "Открыто" : "Закрыто") : string.Empty;
    public bool IsOn => _on;
    public string LinkId => linkId;

    public void Configure(Kind switchKind, string id, string link, string caption)
    {
        kind = switchKind;
        worldId = id;
        linkId = link;
        label = caption;
        ApplySaved();
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        ApplySaved();
    }

    void Start()
    {
        if (LevelWorld.Instance != null)
            LevelWorld.Instance.NoteInterest(transform.position, MapPinKind.Mechanism);
    }

    void OnEnable()
    {
        Interactables.Register(this);
        WorldLinks.Register(this);
        ApplySaved();
    }

    void OnDisable()
    {
        Interactables.Unregister(this);
        WorldLinks.Unregister(this);
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract)
            return;

        SetOn(!_on, false);
    }

    public void SetOn(bool value, bool fromLink)
    {
        _on = value;
        Paint();
        if (LocationState.Active != null)
        {
            LocationState.Active.MarkEnabled(worldId, _on);
            WorldMemory.Store(LocationState.Active);
        }

        if (!fromLink && kind == Kind.Lever)
            WorldLinks.Broadcast(linkId, _on, this);

        if (!fromLink && _on)
            Notices.Post(NoticeKind.Mechanism, label, 1, 0);
    }

    void ApplySaved()
    {
        if (LocationState.Active != null && LocationState.Active.WasEnabled(worldId))
            _on = true;
        Paint();
    }

    void Paint()
    {
        if (kind == Kind.Campfire)
            EnsureGlow();

        if (_renderer == null)
            return;

        if (kind == Kind.Campfire)
        {
            _renderer.color = _on ? new Color(1f, 0.45f, 0.12f, 1f) : new Color(0.35f, 0.22f, 0.16f, 1f);
        }
        else if (kind == Kind.Door)
            _renderer.color = _on ? new Color(0.55f, 0.38f, 0.2f, 0.35f) : new Color(0.42f, 0.3f, 0.18f, 1f);
        else
            _renderer.color = _on ? new Color(0.85f, 0.75f, 0.3f, 1f) : new Color(0.45f, 0.42f, 0.32f, 1f);
    }

    void EnsureGlow()
    {
        if (!Application.isPlaying || kind != Kind.Campfire)
            return;

        if (_glow == null)
        {
            _glow = GetComponent<LocalLight>();
            if (_glow == null)
                _glow = gameObject.AddComponent<LocalLight>();
            _glow.Configure(LocalLight.Role.Fire, new Color(1f, 0.55f, 0.22f, 1f), 3.4f, 0.9f, true);
        }

        _glow.SetLive(_on);
    }
}

public static class WorldLinks
{
    static readonly System.Collections.Generic.List<WorldSwitch> All = new System.Collections.Generic.List<WorldSwitch>();

    public static void Register(WorldSwitch link)
    {
        if (link != null && !All.Contains(link))
            All.Add(link);
    }

    public static void Unregister(WorldSwitch link)
    {
        All.Remove(link);
    }

    public static void Broadcast(string linkId, bool on, WorldSwitch source)
    {
        for (int i = 0; i < All.Count; i++)
        {
            WorldSwitch link = All[i];
            if (link == null || link == source || link.LinkId != linkId)
                continue;

            link.SetOn(on, true);
        }
    }
}
