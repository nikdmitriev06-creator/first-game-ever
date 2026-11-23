using UnityEngine;

/// <summary>
/// Runs statuses on a player or a creature. The host is added when the first status lands.
/// Death and disable clear the active effects. Immunity, if set, stays.
/// </summary>
public class StatusHost : MonoBehaviour
{
    readonly StatusState _state = new StatusState();

    [SerializeField] StatusKind[] immune = System.Array.Empty<StatusKind>();

    Enemy _enemy;
    PlayerVitals _vitals;

    public StatusState State => _state;
    public float OutgoingPhysicalMultiplier => _state.OutgoingPhysicalMultiplier;
    public int ActiveCount => _state.Count;

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _vitals = GetComponent<PlayerVitals>();
        ApplyImmune();
    }

    void OnEnable()
    {
        ApplyImmune();
    }

    void Update()
    {
        Advance(Time.deltaTime);
    }

    public float Advance(float deltaTime)
    {
        if (_enemy == null)
            _enemy = GetComponent<Enemy>();

        if (_enemy != null && _enemy.Dead)
        {
            _state.Clear();
            return 0f;
        }

        float bleed = _state.Tick(deltaTime);
        if (bleed <= 0f)
            return 0f;

        if (_enemy != null)
            _enemy.TakeDamage(bleed);
        else
        {
            if (_vitals == null)
                _vitals = GetComponent<PlayerVitals>();
            if (_vitals != null)
                _vitals.SetHealth(_vitals.Health - bleed);
        }

        Fx.Play(FxEvent.Bleed, transform.position);
        return bleed;
    }

    public bool TryApply(in StatusSpec spec, float roll01)
    {
        return _state.TryApply(spec, roll01);
    }

    public void Clear()
    {
        _state.Clear();
    }

    void ApplyImmune()
    {
        if (immune == null)
            return;

        for (int i = 0; i < immune.Length; i++)
        {
            if (immune[i] != StatusKind.None)
                _state.SetImmune(immune[i], true);
        }
    }
}
