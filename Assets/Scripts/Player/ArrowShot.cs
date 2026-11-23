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

    CombatStrike _strike;

    public void Launch(Vector2 direction, float speed, float range, float damage)
    {
        Launch(direction, speed, range, damage, null);
    }

    public void Launch(Vector2 direction, float speed, float range, float damage, CombatStrike strike)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        _direction = direction.normalized;
        _speed = Mathf.Max(0.01f, speed);
        _remaining = Mathf.Max(0.01f, range);
        Damage = Mathf.Max(0f, damage);
        _strike = strike;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        Vector2 from = transform.position;
        float step = _speed * Time.deltaTime;
        Vector2 to = from + _direction * step;
        transform.position = to;
        _remaining -= step;

        ImpactLibrary.Ensure();
        var impact = new Impact(ImpactLibrary.Slash, Damage, to, _direction, this, _strike);
        var hits = new System.Collections.Generic.List<IImpactReceiver>();
        ReceiverRegistry.CollectOnSegment(from, to, hits);
        while (hits.Count > 0)
        {
            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] == null)
                    continue;

                float distance = Vector2.Distance(from, hits[i].Origin);
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = i;
            }

            IImpactReceiver target = hits[nearest];
            hits.RemoveAt(nearest);
            if (target == null)
                continue;

            ImpactResult result = target.Receive(impact);
            if (result.Outcome == ImpactOutcome.Ignored)
                continue;

            Destroy(gameObject);
            return;
        }

        if (_remaining <= 0f)
            Destroy(gameObject);
    }
}
