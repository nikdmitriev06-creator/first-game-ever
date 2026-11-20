using UnityEngine;

/// <summary>
/// Rectangular walkable area taken from this object's sprite bounds.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayArea : MonoBehaviour
{
    [SerializeField] SpriteRenderer areaRenderer = null;

    void Awake()
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();
    }

    public Vector2 Clamp(Vector2 worldPosition, float inset)
    {
        if (areaRenderer == null)
            areaRenderer = GetComponent<SpriteRenderer>();

        Bounds bounds = areaRenderer.bounds;
        float minX = bounds.min.x + inset;
        float maxX = bounds.max.x - inset;
        float minY = bounds.min.y + inset;
        float maxY = bounds.max.y - inset;

        worldPosition.x = minX > maxX ? bounds.center.x : Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.y = minY > maxY ? bounds.center.y : Mathf.Clamp(worldPosition.y, minY, maxY);
        return worldPosition;
    }
}
