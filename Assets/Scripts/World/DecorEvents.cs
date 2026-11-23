using UnityEngine;

public class DecorEvents : MonoBehaviour
{
    public static DecorEvents Active { get; private set; }

    public static bool Enabled = true;
    public static int MaxAlive = 3;
    public static int Alive { get; private set; }

    const int Pool = 6;

    Actor[] _actors;
    float _wait = 6f;
    float _scan;
    BiomeProfile _biome;
    bool _hidden;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        if (Active != null)
            DestroyImmediate(Active.gameObject);

        Active = null;
        Alive = 0;
        Enabled = true;
        MaxAlive = 3;
    }

    public static void Ensure()
    {
        if (Active != null)
            return;

        var go = new GameObject("Decor Events");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<DecorEvents>();
    }

    public static bool Admit()
    {
        if (!Enabled || Alive >= MaxAlive)
            return false;

        Alive++;
        return true;
    }

    public static void ReleaseOne()
    {
        if (Alive > 0)
            Alive--;
    }

    public static void ClearActive()
    {
        Alive = 0;
        if (Active != null)
            Active.HideAll();
    }

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;
        _actors = new Actor[Pool];
        Sprite sprite = FxQuad.Get();
        for (int i = 0; i < Pool; i++)
        {
            var bit = new GameObject("Decor Event");
            bit.transform.SetParent(transform, false);
            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = IsoSort.ActorMax - 20;
            renderer.enabled = false;
            _actors[i] = new Actor { Transform = bit.transform, Renderer = renderer };
        }
    }

    void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    void LateUpdate()
    {
        _scan -= Time.deltaTime;
        if (_scan <= 0f)
        {
            _scan = 0.75f;
            _hidden = false;
            string id = BiomeLibrary.Current(out _hidden);
            _biome = _hidden || string.IsNullOrEmpty(id) ? null : BiomeLibrary.Get(id);
        }

        float time = Time.time;
        int live = 0;
        for (int i = 0; i < _actors.Length; i++)
        {
            Actor actor = _actors[i];
            if (!actor.On)
                continue;

            actor.Age += Time.deltaTime;
            if (actor.Age >= actor.Life)
            {
                Stop(actor);
                continue;
            }

            live++;
            Vector3 pos = actor.Origin;
            float along = actor.Age / Mathf.Max(0.1f, actor.Life);
            if (actor.Bird)
            {
                pos.x += actor.Speed * actor.Age;
                pos.y += Mathf.Sin(time * 2f + actor.Phase) * 0.25f;
            }
            else if (actor.Critter)
            {
                pos.x += actor.Speed * actor.Age;
                pos.y += Mathf.Abs(Mathf.Sin(time * 8f + actor.Phase)) * 0.18f;
            }
            else
            {
                pos.y -= actor.Speed * actor.Age;
                pos.x += Mathf.Sin(time + actor.Phase) * 0.4f;
            }

            actor.Transform.position = pos;
            Color color = actor.Renderer.color;
            color.a = along > 0.8f ? (1f - along) * 5f : 0.9f;
            actor.Renderer.color = color;
        }

        if (Alive != live)
            Alive = live;

        if (!Enabled || _hidden || _biome == null || !_biome.Events)
            return;

        _wait -= Time.deltaTime;
        if (_wait > 0f || Alive >= MaxAlive)
            return;

        _wait = Random.Range(8f, 18f);
        if (_biome.Water && Random.value < 0.5f)
            SoundBus.Play(FxEvent.Water, transform.position);
        else if (Random.value < 0.35f)
            SoundBus.Play(FxEvent.AmbientSting, transform.position);

        TrySpawn();
    }

    void TrySpawn()
    {
        if (!Admit())
            return;

        Actor actor = Free();
        if (actor == null)
        {
            ReleaseOne();
            return;
        }

        Vector3 origin = Vector3.zero;
        if (GameRoot.Active != null && GameRoot.Active.Player != null)
            origin = GameRoot.Active.Player.transform.position;

        int roll = Random.Range(0, 4);
        if (roll == 0 && _biome.Birds)
            Begin(actor, origin + new Vector3(-8f, 4.5f, 0f), true, false, new Color(0.25f, 0.27f, 0.3f, 0.9f), 2.4f, 3.2f);
        else if (roll == 1 && _biome.Leaves)
            Begin(actor, origin + new Vector3(Random.Range(-3f, 3f), 3.5f, 0f), false, false, new Color(0.45f, 0.55f, 0.24f, 0.85f), 0.7f, 2.4f);
        else if (roll == 2 && _biome.Critters)
            Begin(actor, origin + new Vector3(-3f, -0.4f, 0f), false, true, new Color(0.42f, 0.32f, 0.22f, 0.9f), 1.1f, 2.2f);
        else if (_biome.CaveDust || _biome.Fireflies)
            Begin(actor, origin + (Vector3)Random.insideUnitCircle * 2f, false, false, _biome.Fireflies ? new Color(0.85f, 1f, 0.45f, 0.9f) : new Color(0.75f, 0.76f, 0.8f, 0.5f), 0.4f, 2f);
        else
        {
            ReleaseOne();
        }
    }

    static void Begin(Actor actor, Vector3 origin, bool bird, bool critter, Color color, float speed, float life)
    {
        actor.On = true;
        actor.Bird = bird;
        actor.Critter = critter;
        actor.Origin = origin;
        actor.Age = 0f;
        actor.Life = life;
        actor.Speed = speed;
        actor.Phase = Random.Range(0f, 6f);
        actor.Transform.position = origin;
        actor.Transform.localScale = Vector3.one * (bird ? 0.22f : 0.14f);
        actor.Renderer.enabled = true;
        actor.Renderer.color = color;
    }

    void Stop(Actor actor)
    {
        if (!actor.On)
            return;

        actor.On = false;
        actor.Renderer.enabled = false;
        ReleaseOne();
    }

    void HideAll()
    {
        if (_actors == null)
            return;

        for (int i = 0; i < _actors.Length; i++)
        {
            Actor actor = _actors[i];
            if (actor == null)
                continue;

            actor.On = false;
            if (actor.Renderer != null)
                actor.Renderer.enabled = false;
        }
    }

    Actor Free()
    {
        for (int i = 0; i < _actors.Length; i++)
        {
            if (!_actors[i].On)
                return _actors[i];
        }

        return null;
    }

    sealed class Actor
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Origin;
        public float Age;
        public float Life;
        public float Speed;
        public float Phase;
        public bool On;
        public bool Bird;
        public bool Critter;
    }
}
