using System.Collections;
using UnityEngine;

/// <summary>
/// Flash, shake, and chips for a body that was hit. Color comes from the material, not from the tool.
/// </summary>
public class ImpactFeedback : MonoBehaviour
{
    MaterialDefinition _material;
    SpriteRenderer _renderer;
    Vector3 _home;
    float _flash;
    float _shake;
    Sprite _chip;
    Material _chipMaterial;
    bool _bursting;

    public void Use(MaterialDefinition material)
    {
        _material = material;
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _home = transform.localPosition;
    }

    public void PlayHit(in Impact impact)
    {
        _flash = 1f;
        _shake = 0.16f;
        _home = transform.localPosition;
        if (Application.isPlaying && isActiveAndEnabled && !_bursting)
            StartCoroutine(Burst(impact.Point, impact.Direction));
    }

    void Update()
    {
        if (_renderer == null || (_flash <= 0f && _shake <= 0f))
            return;

        if (_flash > 0f)
        {
            _flash -= Time.deltaTime * 5f;
            float blend = Mathf.Clamp01(_flash);
            _renderer.color = Color.Lerp(Color.white, new Color(1f, 0.76f, 0.38f), blend);
        }

        if (_shake > 0f)
        {
            _shake -= Time.deltaTime;
            if (_shake < 0f)
                _shake = 0f;
        }

        float kick = _shake > 0f ? Mathf.Sin(Time.time * 48f) * _shake * 0.14f : 0f;
        transform.localPosition = _home + new Vector3(kick, 0f, 0f);
    }

    IEnumerator Burst(Vector2 at, Vector2 dir)
    {
        _bursting = true;
        const int count = 5;
        var bits = new Transform[count];
        var renderers = new SpriteRenderer[count];
        var velocity = new Vector2[count];
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;
        dir.Normalize();

        Color debris = _material != null ? _material.Debris : new Color(0.93f, 0.78f, 0.46f, 1f);
        if (_chipMaterial == null && _renderer != null)
            _chipMaterial = _renderer.sharedMaterial;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Impact Chip");
            if (transform.parent != null)
                go.transform.SetParent(transform.parent, true);
            go.transform.position = at;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ChipSprite();
            renderer.sortingOrder = 18;
            if (_renderer != null)
                renderer.sortingLayerID = _renderer.sortingLayerID;
            if (_chipMaterial != null)
                renderer.sharedMaterial = _chipMaterial;
            renderer.color = i % 2 == 0 ? debris : Color.Lerp(debris, Color.white, 0.45f);
            bits[i] = go.transform;
            renderers[i] = renderer;

            float spread = (i - 2) * 0.85f;
            Vector2 side = new Vector2(-dir.y, dir.x);
            velocity[i] = -dir * 0.6f + side * spread + Vector2.up * Random.Range(0.8f, 1.8f);
        }

        float life = 0.2f;
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(t / life);
            for (int i = 0; i < count; i++)
            {
                if (bits[i] == null)
                    continue;

                velocity[i] += Vector2.down * (16f * Time.deltaTime);
                bits[i].position += (Vector3)(velocity[i] * Time.deltaTime);
                Color color = renderers[i].color;
                color.a = fade;
                renderers[i].color = color;
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (bits[i] != null)
                Destroy(bits[i].gameObject);
        }

        _bursting = false;
    }

    Sprite ChipSprite()
    {
        if (_chip != null)
            return _chip;

        const int size = 3;
        var pixels = new Color32[size * size];
        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = white;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        _chip = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        _chip.hideFlags = HideFlags.HideAndDontSave;
        return _chip;
    }
}
