using System;
using UnityEngine;

/// <summary>
/// Integrity of one world body. The material is shared. The remaining integrity belongs to this instance.
/// </summary>
public class Destructible : MonoBehaviour, IImpactReceiver
{
    [SerializeField] MaterialDefinition material;
    [SerializeField] ImpactReaction[] extra;
    [SerializeField] float maxIntegrity = 3f;
    [SerializeField] float height = 1f;
    [SerializeField] float slop = 0.95f;
    [SerializeField] string worldId;

    float _integrity;
    bool _broken;
    bool _configured;

    public event Action IntegrityChanged;

    public bool CanReceive => _configured && !_broken && _integrity > 0f && isActiveAndEnabled;
    public Vector2 Origin => transform.position;
    public float Height => height;
    public float Slop => slop;
    public float Integrity => _integrity;
    public float MaxIntegrity => maxIntegrity;
    public string WorldId => worldId;
    public MaterialDefinition Material => material;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
    }

    public static Destructible Create(GameObject host, MaterialDefinition bodyMaterial, float hits, float bodyHeight, float bodySlop)
    {
        if (host == null)
            return null;

        Destructible body = host.GetComponent<Destructible>();
        if (body == null)
            body = host.AddComponent<Destructible>();

        body.Configure(bodyMaterial, hits, bodyHeight, bodySlop);
        ImpactFeedback feedback = host.GetComponent<ImpactFeedback>();
        if (feedback == null)
            feedback = host.AddComponent<ImpactFeedback>();
        feedback.Use(bodyMaterial);
        return body;
    }

    public void Configure(MaterialDefinition bodyMaterial, float hits, float bodyHeight, float bodySlop)
    {
        material = bodyMaterial;
        maxIntegrity = Mathf.Max(1f, hits);
        _integrity = maxIntegrity;
        height = Mathf.Max(0.2f, bodyHeight);
        slop = Mathf.Max(0.2f, bodySlop);
        _broken = false;
        _configured = material != null;
        EnsureId();
    }

    public void ApplySavedIntegrity(float current)
    {
        if (current > 0f)
            _integrity = current;
    }

    public void AssignId(string id)
    {
        if (!string.IsNullOrEmpty(id))
            worldId = id;
    }

    public ImpactResult Receive(in Impact impact)
    {
        if (!CanReceive || impact.Power <= 0f)
            return ImpactResult.Ignore();

        if (!TryMultiplier(impact.Type, out float multiplier) || multiplier <= 0f)
            return ImpactResult.Ignore();

        float loss = Mathf.Max(1f, Mathf.Ceil(impact.Power * multiplier));
        _integrity -= loss;
        ImpactFeedback feedback = GetComponent<ImpactFeedback>();
        if (_integrity > 0f)
        {
            if (feedback != null)
                feedback.PlayHit(impact);
            IntegrityChanged?.Invoke();
            return ImpactResult.Damage(loss);
        }

        _integrity = 0f;
        _broken = true;
        IntegrityChanged?.Invoke();
        if (feedback != null)
            feedback.PlayHit(impact);
        Break(impact);
        return ImpactResult.Break(loss);
    }

    void Break(in Impact impact)
    {
        if (!string.IsNullOrEmpty(worldId) && LocationState.Active != null)
            LocationState.Active.MarkRemoved(worldId);

        IBreakReaction[] reactions = GetComponents<IBreakReaction>();
        for (int i = 0; i < reactions.Length; i++)
        {
            if (reactions[i] != null)
                reactions[i].OnBroken(impact);
        }
    }

    bool TryMultiplier(ImpactType type, out float multiplier)
    {
        if (extra != null)
        {
            for (int i = 0; i < extra.Length; i++)
            {
                if (extra[i].Type != null && extra[i].Type.Same(type))
                {
                    multiplier = extra[i].Multiplier;
                    return true;
                }
            }
        }

        if (material != null)
            return material.TryMultiplier(type, out multiplier);

        multiplier = 0f;
        return false;
    }

    void EnsureId()
    {
        if (string.IsNullOrEmpty(worldId))
            worldId = Guid.NewGuid().ToString("N");
    }

    void OnEnable()
    {
        ReceiverRegistry.Register(this);
    }

    void OnDisable()
    {
        ReceiverRegistry.Unregister(this);
    }
}
