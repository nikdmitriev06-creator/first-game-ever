using UnityEngine;

/// <summary>
/// Scene handle for one authored marker. Gameplay spawning is done by <see cref="RoomDocument"/>.
/// </summary>
public class RoomMarker : MonoBehaviour
{
    [SerializeField] string markerId;
    [SerializeField] RoomDocument document;

    public string MarkerId => markerId;
    public RoomDocument Document => document;

    public void Bind(RoomDocument owner, string id)
    {
        document = owner;
        markerId = id;
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (Application.isPlaying || document == null || document.IsRebuilding)
            return;

        if (document.HasOther(this))
            document.AdoptCopy(this);
    }
#endif
}
