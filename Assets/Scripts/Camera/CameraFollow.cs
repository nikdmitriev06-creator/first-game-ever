using UnityEngine;

/// <summary>
/// Keeps the orthographic camera on the target. Replace this component later
/// if a Cinemachine virtual camera should follow the same transform.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target = null;
    [SerializeField] float smoothTime = 0f;

    Vector3 _velocity;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        if (smoothTime <= 0f)
        {
            transform.position = desired;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }
}
