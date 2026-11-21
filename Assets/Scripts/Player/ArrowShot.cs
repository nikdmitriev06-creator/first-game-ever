using UnityEngine;

/// <summary>
/// Flies in a straight line. The painted tip sits opposite the top of the sprite, so the facing is flipped.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ArrowShot : MonoBehaviour
{
    Vector2 _direction = Vector2.right;
    float _speed = 32f;
    float _remaining = 40f;

    public float Damage { get; private set; }

    public void Launch(Vector2 direction, float speed, float range, float damage)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        _direction = direction.normalized;
        _speed = Mathf.Max(0.01f, speed);
        _remaining = Mathf.Max(0.01f, range);
        Damage = Mathf.Max(0f, damage);

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        Vector2 from = transform.position;
        float step = _speed * Time.deltaTime;
        transform.position += (Vector3)(_direction * step);
        _remaining -= step;

        if (Enemy.TryDamageSegment(from, transform.position, Damage))
        {
            Destroy(gameObject);
            return;
        }

        if (_remaining <= 0f)
            Destroy(gameObject);
    }
}
