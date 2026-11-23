using System.Collections.Generic;
using UnityEngine;

public enum FxEvent
{
    Hit,
    Critical,
    Hurt,
    Bleed,
    Death,
    Harvest,
    Chop,
    Mine,
    Break,
    Pickup,
    Chest,
    Rare,
    Magic,
    Bow,
    Crossbow,
    Craft,
    Step,
    Ore,
    Water,
    AmbientSting
}

/// <summary>
/// Short-lived visuals for hits, harvest and world events. One pool, no per-effect class.
/// A profile can replace the placeholder. A missing prefab never throws.
/// </summary>
public static class Fx
{
    const int Cap = 48;
    const int PrefabCap = 16;

    static readonly List<FxBit> Pool = new List<FxBit>();
    static readonly List<FxProp> Props = new List<FxProp>();
    static FxProfile _bound;
    static FxProfile _builtin;
    static Transform _root;
    static int _cursor;
    static int _propCursor;

    public static bool Enabled = true;
    public static bool AllowInEditMode;
    public static int ActiveCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        if (_root != null)
            Object.DestroyImmediate(_root.gameObject);

        Pool.Clear();
        Props.Clear();
        _bound = null;
        _builtin = null;
        _root = null;
        _cursor = 0;
        _propCursor = 0;
        ActiveCount = 0;
        Enabled = true;
        AllowInEditMode = false;
    }

    public static void Warm()
    {
        FxProfile[] loaded = Resources.LoadAll<FxProfile>("VFX");
        if (loaded == null)
            return;

        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null)
            {
                _bound = loaded[i];
                return;
            }
        }
    }

    public static void Bind(FxProfile profile)
    {
        _bound = profile;
    }

    public static FxRecipe RecipeFor(FxEvent ev, DamageType type)
    {
        FxProfile profile = ActiveProfile();
        FxRecipe found = profile != null ? profile.Find(ev, type) : null;
        if (found != null)
            return found;

        return new FxRecipe
        {
            Event = ev,
            Type = type,
            Color = ColorFor(ev, type),
            Count = ev == FxEvent.Death || ev == FxEvent.Break ? 6 : 4,
            Life = ev == FxEvent.Death ? 0.55f : 0.32f,
            Scale = ev == FxEvent.Death || ev == FxEvent.Break ? 0.42f : 0.24f
        };
    }

    public static void FillDefaults(FxProfile profile)
    {
        if (profile == null)
            return;

        profile.Recipes = Builtins();
    }

    public static void Play(FxEvent ev, Vector3 point, DamageType type = DamageType.None)
    {
        SoundBus.Play(ev, point);
        if (!Enabled || !Live())
            return;

        FxRecipe recipe = RecipeFor(ev, type);
        if (recipe.Prefab != null)
        {
            RentProp().Begin(recipe, point);
            return;
        }

        int count = Mathf.Clamp(recipe.Count, 1, 6);
        for (int i = 0; i < count; i++)
            Rent().Begin(recipe, point, i);
    }

    public static void ReleaseAll()
    {
        for (int i = 0; i < Pool.Count; i++)
        {
            if (Pool[i] != null)
                Pool[i].Hide();
        }

        for (int i = 0; i < Props.Count; i++)
        {
            if (Props[i] != null)
                Props[i].Hide();
        }

        ActiveCount = 0;
    }

    public static Color ColorFor(FxEvent ev, DamageType type)
    {
        switch (type)
        {
            case DamageType.Fire: return new Color(1f, 0.42f, 0.12f, 1f);
            case DamageType.Cold: return new Color(0.55f, 0.86f, 1f, 1f);
            case DamageType.Lightning: return new Color(1f, 0.92f, 0.35f, 1f);
            case DamageType.Holy: return new Color(1f, 0.9f, 0.55f, 1f);
            case DamageType.Dark: return new Color(0.42f, 0.22f, 0.62f, 1f);
            case DamageType.Piercing: return new Color(0.82f, 0.88f, 0.94f, 1f);
            case DamageType.Blunt: return new Color(0.72f, 0.58f, 0.38f, 1f);
            case DamageType.Slashing: return new Color(0.86f, 0.9f, 0.94f, 1f);
        }

        switch (ev)
        {
            case FxEvent.Critical: return new Color(1f, 0.82f, 0.25f, 1f);
            case FxEvent.Bleed: return new Color(0.7f, 0.08f, 0.08f, 1f);
            case FxEvent.Death: return new Color(0.35f, 0.35f, 0.38f, 1f);
            case FxEvent.Chop: return new Color(0.55f, 0.36f, 0.16f, 1f);
            case FxEvent.Mine: return new Color(0.62f, 0.64f, 0.66f, 1f);
            case FxEvent.Ore: return new Color(0.72f, 0.46f, 0.22f, 1f);
            case FxEvent.Magic: return new Color(1f, 0.35f, 0.12f, 1f);
            case FxEvent.Rare: return new Color(0.72f, 0.45f, 1f, 1f);
            case FxEvent.Chest: return new Color(0.9f, 0.72f, 0.28f, 1f);
            case FxEvent.Water: return new Color(0.45f, 0.7f, 0.85f, 1f);
            default: return new Color(1f, 0.95f, 0.85f, 1f);
        }
    }

    static bool Live()
    {
        return Application.isPlaying || AllowInEditMode;
    }

    static FxProfile ActiveProfile()
    {
        if (_bound != null)
            return _bound;

        if (_builtin != null)
            return _builtin;

        _builtin = ScriptableObject.CreateInstance<FxProfile>();
        _builtin.hideFlags = HideFlags.HideAndDontSave;
        _builtin.Recipes = Builtins();
        return _builtin;
    }

    static FxRecipe[] Builtins()
    {
        return new[]
        {
            Row(FxEvent.Hit, DamageType.None, ColorFor(FxEvent.Hit, DamageType.None), 4, 0.28f, 0.22f),
            Row(FxEvent.Hit, DamageType.Fire, ColorFor(FxEvent.Hit, DamageType.Fire), 5, 0.36f, 0.26f),
            Row(FxEvent.Hit, DamageType.Cold, ColorFor(FxEvent.Hit, DamageType.Cold), 4, 0.4f, 0.22f),
            Row(FxEvent.Hit, DamageType.Lightning, ColorFor(FxEvent.Hit, DamageType.Lightning), 5, 0.22f, 0.2f),
            Row(FxEvent.Hit, DamageType.Holy, ColorFor(FxEvent.Hit, DamageType.Holy), 4, 0.4f, 0.28f),
            Row(FxEvent.Hit, DamageType.Dark, ColorFor(FxEvent.Hit, DamageType.Dark), 4, 0.4f, 0.26f),
            Row(FxEvent.Hurt, DamageType.None, ColorFor(FxEvent.Hurt, DamageType.None), 3, 0.26f, 0.2f),
            Row(FxEvent.Hurt, DamageType.Fire, ColorFor(FxEvent.Hurt, DamageType.Fire), 4, 0.34f, 0.24f),
            Row(FxEvent.Hurt, DamageType.Cold, ColorFor(FxEvent.Hurt, DamageType.Cold), 3, 0.36f, 0.2f),
            Row(FxEvent.Hurt, DamageType.Lightning, ColorFor(FxEvent.Hurt, DamageType.Lightning), 4, 0.2f, 0.18f),
            Row(FxEvent.Critical, DamageType.None, ColorFor(FxEvent.Critical, DamageType.None), 6, 0.4f, 0.32f),
            Row(FxEvent.Bleed, DamageType.None, ColorFor(FxEvent.Bleed, DamageType.None), 3, 0.45f, 0.16f),
            Row(FxEvent.Death, DamageType.None, ColorFor(FxEvent.Death, DamageType.None), 6, 0.55f, 0.42f),
            Row(FxEvent.Chop, DamageType.None, ColorFor(FxEvent.Chop, DamageType.None), 5, 0.34f, 0.22f),
            Row(FxEvent.Mine, DamageType.None, ColorFor(FxEvent.Mine, DamageType.None), 5, 0.3f, 0.18f),
            Row(FxEvent.Ore, DamageType.None, ColorFor(FxEvent.Ore, DamageType.None), 5, 0.34f, 0.18f),
            Row(FxEvent.Break, DamageType.None, ColorFor(FxEvent.Break, DamageType.None), 6, 0.4f, 0.36f),
            Row(FxEvent.Pickup, DamageType.None, ColorFor(FxEvent.Pickup, DamageType.None), 3, 0.28f, 0.16f),
            Row(FxEvent.Chest, DamageType.None, ColorFor(FxEvent.Chest, DamageType.None), 5, 0.45f, 0.24f),
            Row(FxEvent.Rare, DamageType.None, ColorFor(FxEvent.Rare, DamageType.None), 6, 0.6f, 0.28f),
            Row(FxEvent.Magic, DamageType.None, ColorFor(FxEvent.Magic, DamageType.None), 5, 0.4f, 0.3f),
            Row(FxEvent.Magic, DamageType.Fire, ColorFor(FxEvent.Magic, DamageType.Fire), 5, 0.4f, 0.3f),
            Row(FxEvent.Magic, DamageType.Cold, ColorFor(FxEvent.Magic, DamageType.Cold), 5, 0.45f, 0.28f),
            Row(FxEvent.Water, DamageType.None, ColorFor(FxEvent.Water, DamageType.None), 3, 0.5f, 0.16f)
        };
    }

    static FxRecipe Row(FxEvent ev, DamageType type, Color color, int count, float life, float scale)
    {
        return new FxRecipe
        {
            Event = ev,
            Type = type,
            Color = color,
            Count = count,
            Life = life,
            Scale = scale,
            Speed = 1.4f
        };
    }

    static FxBit Rent()
    {
        EnsureRoot();
        if (Pool.Count < Cap)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(_root, false);
            FxBit created = go.AddComponent<FxBit>();
            Pool.Add(created);
            return created;
        }

        FxBit bit = Pool[_cursor];
        _cursor = (_cursor + 1) % Pool.Count;
        return bit;
    }

    static FxProp RentProp()
    {
        EnsureRoot();
        for (int i = 0; i < Props.Count; i++)
        {
            if (Props[i] != null && !Props[i].Live)
                return Props[i];
        }

        if (Props.Count < PrefabCap)
        {
            var go = new GameObject("Fx Prop");
            go.transform.SetParent(_root, false);
            FxProp created = go.AddComponent<FxProp>();
            Props.Add(created);
            return created;
        }

        FxProp prop = Props[_propCursor];
        _propCursor = (_propCursor + 1) % Props.Count;
        return prop;
    }

    static void EnsureRoot()
    {
        if (_root != null)
            return;

        var go = new GameObject("Fx Pool");
        go.hideFlags = HideFlags.HideAndDontSave;
        _root = go.transform;
    }

    public static void Note(bool live)
    {
        ActiveCount += live ? 1 : -1;
        if (ActiveCount < 0)
            ActiveCount = 0;
    }
}

public class FxBit : MonoBehaviour
{
    SpriteRenderer _renderer;
    float _life;
    float _full;
    Vector2 _velocity;
    bool _live;

    void Awake()
    {
        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = FxQuad.Get();
        _renderer.sortingOrder = IsoSort.ActorMax - 8;
        gameObject.SetActive(false);
    }

    public void Begin(FxRecipe recipe, Vector3 point, int index)
    {
        if (_live)
            Fx.Note(false);

        if (recipe == null)
            recipe = new FxRecipe();

        transform.position = point;
        transform.localScale = Vector3.one * Mathf.Max(0.08f, recipe.Scale);
        if (_renderer != null)
            _renderer.color = recipe.Color.a <= 0f ? Color.white : recipe.Color;

        float speed = recipe.Speed > 0f ? recipe.Speed : 1.4f;
        float spin = (index - 2) * 0.7f;
        _velocity = new Vector2(spin, 0.8f) + Random.insideUnitCircle * (0.45f * speed);
        _full = recipe.Life > 0.05f ? recipe.Life : 0.32f;
        _life = _full;
        _live = true;
        Fx.Note(true);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_live)
            Fx.Note(false);
        _live = false;
        _life = 0f;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_live)
            return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Hide();
            return;
        }

        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _velocity *= 0.92f;
        if (_renderer == null)
            return;

        Color color = _renderer.color;
        color.a = Mathf.Clamp01(_life / Mathf.Max(0.05f, _full));
        _renderer.color = color;
    }
}

public class FxProp : MonoBehaviour
{
    GameObject _instance;
    float _life;
    bool _live;

    public bool Live => _live;

    public void Begin(FxRecipe recipe, Vector3 point)
    {
        Hide();
        if (recipe == null || recipe.Prefab == null)
            return;

        _instance = Object.Instantiate(recipe.Prefab, point, Quaternion.identity, transform);
        _life = recipe.Life > 0.05f ? recipe.Life : 0.6f;
        _live = true;
        Fx.Note(true);
        gameObject.SetActive(true);
        ParticleSystem particles = _instance.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
            particles.Play(true);
    }

    public void Hide()
    {
        if (_live)
            Fx.Note(false);
        _live = false;
        _life = 0f;
        if (_instance != null)
        {
            ParticleSystem particles = _instance.GetComponentInChildren<ParticleSystem>();
            if (particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (Application.isPlaying)
                Object.Destroy(_instance);
            else
                Object.DestroyImmediate(_instance);
            _instance = null;
        }

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_live)
            return;

        _life -= Time.deltaTime;
        if (_life <= 0f)
            Hide();
    }
}

static class FxQuad
{
    static Sprite _sprite;

    public static Sprite Get()
    {
        if (_sprite != null)
            return _sprite;

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        _sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _sprite.hideFlags = HideFlags.HideAndDontSave;
        return _sprite;
    }
}
