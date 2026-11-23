using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One rising total per hit. Ten slashing and ten fire on the same blow are a single 20.
/// Several numbers that start together step aside so they do not cover each other.
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    const int Pool = 24;
    const float Hold = 0.5f;
    const float Fade = 0.4f;
    const float RiseSpeed = 1.35f;
    const float CrowdDistance = 0.55f;

    struct Bit
    {
        public RectTransform Rect;
        public TMP_Text Label;
        public Vector3 World;
        public float Age;
        public bool Live;
    }

    static DamageNumbers _active;
    Bit[] _bits;
    Camera _camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _active = null;
    }

    public static void Show(Vector3 world, float amount)
    {
        if (!Application.isPlaying || amount <= 0f || float.IsNaN(amount))
            return;

        int shown = Mathf.RoundToInt(amount);
        if (shown <= 0)
            return;

        Ensure();
        if (_active != null)
            _active.Spawn(world, shown);
    }

    public static void Clear()
    {
        if (_active == null)
            return;

        _active.HideAll();
    }

    static void Ensure()
    {
        if (_active != null)
            return;

        var go = new GameObject("Damage Numbers");
        _active = go.AddComponent<DamageNumbers>();
    }

    void Awake()
    {
        if (_active != null && _active != this)
        {
            Destroy(gameObject);
            return;
        }

        _active = this;
        Build();
    }

    void OnDestroy()
    {
        if (_active == this)
            _active = null;
    }

    void LateUpdate()
    {
        if (_bits == null)
            return;

        if (_camera == null)
            _camera = Camera.main;

        float step = Time.deltaTime;
        for (int i = 0; i < _bits.Length; i++)
        {
            if (!_bits[i].Live)
                continue;

            Bit bit = _bits[i];
            bit.Age += step;
            bit.World.y += RiseSpeed * step;
            float life = Hold + Fade;
            if (bit.Age >= life)
            {
                bit.Live = false;
                bit.Rect.gameObject.SetActive(false);
                _bits[i] = bit;
                continue;
            }

            float alpha = bit.Age <= Hold ? 1f : 1f - (bit.Age - Hold) / Fade;
            Color color = bit.Label.color;
            color.a = alpha;
            bit.Label.color = color;
            Place(bit);
            _bits[i] = bit;
        }
    }

    void Spawn(Vector3 world, int amount)
    {
        int slot = -1;
        int crowd = 0;
        float oldest = -1f;
        int oldestIndex = 0;
        for (int i = 0; i < _bits.Length; i++)
        {
            if (!_bits[i].Live)
            {
                if (slot < 0)
                    slot = i;
                continue;
            }

            if (_bits[i].Age > oldest)
            {
                oldest = _bits[i].Age;
                oldestIndex = i;
            }

            Vector2 delta = (Vector2)(_bits[i].World - world);
            if (delta.sqrMagnitude <= CrowdDistance * CrowdDistance)
                crowd++;
        }

        if (slot < 0)
            slot = oldestIndex;

        float column = (crowd % 5) - 2;
        float row = crowd / 5;
        Vector3 start = world + new Vector3(column * 0.42f, 0.2f + row * 0.32f, 0f);

        Bit bit = _bits[slot];
        bit.World = start;
        bit.Age = 0f;
        bit.Live = true;
        bit.Label.text = amount.ToString();
        bit.Label.color = new Color(0.98f, 0.95f, 0.86f, 1f);
        UiGlyphs.Prepare(bit.Label, bit.Label.text);
        bit.Rect.gameObject.SetActive(true);
        _bits[slot] = bit;
        Place(bit);
    }

    void Place(Bit bit)
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null || bit.Rect == null)
            return;

        Vector3 screen = _camera.WorldToScreenPoint(bit.World);
        bool visible = screen.z > 0f;
        if (bit.Rect.gameObject.activeSelf != visible)
            bit.Rect.gameObject.SetActive(visible);
        if (!visible)
            return;

        bit.Rect.position = new Vector3(screen.x, screen.y, 0f);
    }

    void HideAll()
    {
        if (_bits == null)
            return;

        for (int i = 0; i < _bits.Length; i++)
        {
            Bit bit = _bits[i];
            bit.Live = false;
            bit.Age = 0f;
            if (bit.Rect != null)
                bit.Rect.gameObject.SetActive(false);
            _bits[i] = bit;
        }
    }

    void Build()
    {
        UiScreen.Mount(gameObject, 125);

        _bits = new Bit[Pool];
        for (int i = 0; i < _bits.Length; i++)
        {
            var rect = new GameObject("Number", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(72f, 36f);

            TMP_Text text = UiGlyphs.Place(rect.gameObject, string.Empty, 28f, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.86f, 1f), UiGlyphs.Wrap.Single, true);
            rect.gameObject.SetActive(false);

            _bits[i] = new Bit { Rect = rect, Label = text };
        }
    }
}
