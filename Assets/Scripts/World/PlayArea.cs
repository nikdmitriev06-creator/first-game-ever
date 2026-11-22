using UnityEngine;

/// <summary>
/// Rectangular walkable area taken from this object's sprite bounds.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayArea : MonoBehaviour
{
    [SerializeField] SpriteRenderer areaRenderer = null;

    // World bounds captured while the sprite is active. Inactive rooms keep this
    // rectangle so the player can still be placed on them after a transition.
    [System.NonSerialized] bool _useCachedBounds;
    [System.NonSerialized] Bounds _cachedBounds;

    void Awake()
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();
    }

    public Bounds CachedBounds
    {
        get
        {
            if (_useCachedBounds)
                return _cachedBounds;

            if (areaRenderer == null)
                areaRenderer = GetComponent<SpriteRenderer>();

            return areaRenderer != null ? areaRenderer.bounds : new Bounds(transform.position, Vector3.one);
        }
    }

    public void CacheBounds()
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();

        _cachedBounds = areaRenderer != null ? areaRenderer.bounds : new Bounds(transform.position, Vector3.one);
        if (_cachedBounds.size.sqrMagnitude <= 0.01f)
            _cachedBounds = BoundsFromSprite();

        _useCachedBounds = _cachedBounds.size.sqrMagnitude > 0.01f;
    }

    Bounds BoundsFromSprite()
    {
        if (areaRenderer == null || areaRenderer.sprite == null)
            return new Bounds(transform.position, Vector3.one);

        Sprite sprite = areaRenderer.sprite;
        Vector3 scale = transform.lossyScale;
        float width = sprite.rect.width / sprite.pixelsPerUnit * Mathf.Abs(scale.x);
        float height = sprite.rect.height / sprite.pixelsPerUnit * Mathf.Abs(scale.y);
        return new Bounds(transform.position, new Vector3(Mathf.Max(width, 0.01f), Mathf.Max(height, 0.01f), 0.2f));
    }

    public Vector2 Clamp(Vector2 worldPosition, float inset)
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();

        Bounds bounds = _useCachedBounds ? _cachedBounds : areaRenderer.bounds;
        float minX = bounds.min.x + inset;
        float maxX = bounds.max.x - inset;
        float minY = bounds.min.y + inset;
        float maxY = bounds.max.y - inset;

        worldPosition.x = minX > maxX ? bounds.center.x : Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.y = minY > maxY ? bounds.center.y : Mathf.Clamp(worldPosition.y, minY, maxY);
        return worldPosition;
    }
}
