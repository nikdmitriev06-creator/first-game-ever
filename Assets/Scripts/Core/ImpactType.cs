using UnityEngine;

/// <summary>
/// A kind of hit: chop, mine, smash, slash. New kinds are assets, identified by id.
/// Two assets with the same id are the same kind. The asset does not store per-object health.
/// </summary>
[CreateAssetMenu(fileName = "ImpactType", menuName = "Game/Impact Type")]
public class ImpactType : ScriptableObject
{
    [SerializeField] string id;

    public string Id => id;

    public void Configure(string typeId)
    {
        id = typeId;
    }

    public bool Same(ImpactType other)
    {
        if (other == null || string.IsNullOrEmpty(id))
            return false;

        return this == other || id == other.id;
    }
}
