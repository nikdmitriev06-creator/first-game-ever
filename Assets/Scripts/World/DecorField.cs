using UnityEngine;

/// <summary>
/// A small drift of grass, leaves, fireflies or cave dust around the player.
/// The biome decides which motes appear. One component moves the whole field.
/// </summary>
public class DecorField : MonoBehaviour
{
    public static DecorField Active { get; private set; }

    public static bool Enabled = true;
    public static float Intensity = 1f;

    const int Count = 16;
    const float Radius = 9f;

    Mote[] _motes;
    BiomeProfile _biome;
    string _biomeId = "forest";
    bool _hidden;
    Transform _anchor;
    float _scan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        if (Active != null)
            DestroyImmediate(Active.gameObject);

        Active = null;
        Enabled = true;
        Intensity = 1f;
    }

    public static void Ensure()
    {
        if (Active != null)
            return;

        var go = new GameObject("Decor Field");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<DecorField>();
    }

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;
        BiomeLibrary.Ensure();
        _biome = BiomeLibrary.Get("forest");
        Sprite sprite = FxQuad.Get();
        _motes = new Mote[Count];
        for (int i = 0; i < Count; i++)
        {
            var bit = new GameObject("Mote");
            bit.transform.SetParent(transform, false);
            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = IsoSort.ActorMax - 40;
            _motes[i] = new Mote
            {
                Transform = bit.transform,
                Renderer = renderer,
                Offset = Random.insideUnitCircle * Radius,
                Spin = Random.Range(0.4f, 1.4f),
                Phase = Random.Range(0f, 6.28f),
                Speed = Random.Range(0.15f, 0.55f)
            };
        }

        Paint();
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
            Refresh();
        }

        if (_anchor == null || _hidden || _biome == null || !Enabled || Intensity <= 0.001f)
            return;

        Vector3 origin = _anchor.position;
        float time = Time.time;
        float strength = Mathf.Clamp01(Intensity) * Mathf.Clamp01(_biome.Motion);
        for (int i = 0; i < _motes.Length; i++)
        {
            Mote mote = _motes[i];
            if (!mote.Renderer.enabled)
                continue;

            float sway = Mathf.Sin(time * mote.Spin + mote.Phase);
            if (mote.Grass)
            {
                mote.Offset.x += sway * 0.15f * Time.deltaTime;
            }
            else
            {
                float fall = mote.Glow ? 0.15f : 1f;
                mote.Offset.y -= mote.Speed * Time.deltaTime * fall * strength;
            }

            if (mote.Offset.sqrMagnitude > Radius * Radius)
                mote.Offset = Random.insideUnitCircle * Radius;

            Vector3 pos = origin + (Vector3)mote.Offset;
            pos.x += sway * (mote.Grass ? 0.35f : mote.Leaf ? 0.8f : 0.35f) * strength;
            if (!mote.Grass)
                pos.y += Mathf.Cos(time * mote.Speed + mote.Phase) * 0.35f;
            mote.Transform.position = pos;
            mote.Transform.localScale = Vector3.one * (mote.Leaf ? 0.18f : 0.12f);
            if (!mote.Leaf || mote.Glow)
            {
                Color color = mote.Renderer.color;
                color.a = (0.25f + 0.7f * Mathf.Abs(sway)) * strength;
                mote.Renderer.color = color;
            }
        }
    }

    void Refresh()
    {
        if (_anchor == null && GameRoot.Active != null && GameRoot.Active.Player != null)
            _anchor = GameRoot.Active.Player.transform;

        string id = BiomeLibrary.Current(out bool hidden);
        if (id == _biomeId && hidden == _hidden && _biome != null)
            return;

        _biomeId = id;
        _hidden = hidden;
        _biome = string.IsNullOrEmpty(id) ? null : BiomeLibrary.Get(id);
        Paint();
    }

    void Paint()
    {
        bool leaves = _biome != null && _biome.Leaves;
        bool grass = _biome != null && _biome.Grass;
        bool flies = _biome != null && _biome.Fireflies;
        bool dust = _biome != null && _biome.CaveDust;
        bool water = _biome != null && _biome.Water;
        bool show = Enabled && !_hidden && (leaves || grass || flies || dust || water);
        for (int i = 0; i < _motes.Length; i++)
        {
            Mote mote = _motes[i];
            mote.Glow = flies && i % 4 == 0;
            mote.Grass = grass && !mote.Glow && i % 2 == 0;
            mote.Leaf = leaves && !mote.Grass && !mote.Glow;
            if (!leaves && !grass && (dust || water))
                mote.Leaf = false;
            mote.Renderer.enabled = show;
            mote.Renderer.color = ColorFor(mote, dust, flies, water);
        }
    }

    static Color ColorFor(Mote mote, bool dust, bool flies, bool water)
    {
        if (mote.Grass)
            return new Color(0.32f, 0.55f, 0.24f, 0.7f);
        if (mote.Leaf)
            return new Color(0.45f, 0.62f, 0.28f, 0.75f);
        if (water && !flies)
            return new Color(0.45f, 0.68f, 0.72f, 0.45f);
        if (dust && !flies)
            return new Color(0.72f, 0.74f, 0.78f, 0.45f);
        return new Color(0.85f, 1f, 0.45f, 0.9f);
    }

    sealed class Mote
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Offset;
        public float Spin;
        public float Phase;
        public float Speed;
        public bool Leaf = true;
        public bool Grass;
        public bool Glow;
    }
}
