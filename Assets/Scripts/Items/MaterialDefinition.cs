using System;
using UnityEngine;

[Serializable]
public struct ImpactReaction
{
    public ImpactType Type;
    public float Multiplier;
}

/// <summary>
/// Shared resistance. Many objects can reference one material. Current integrity is not stored here.
/// </summary>
[CreateAssetMenu(fileName = "Material", menuName = "Game/Material")]
public class MaterialDefinition : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] ImpactReaction[] reactions = Array.Empty<ImpactReaction>();
    [SerializeField] Color debris = new Color(0.93f, 0.78f, 0.46f, 1f);

    public string Id => id;
    public Color Debris => debris;

    public void Configure(string materialId, ImpactReaction[] accepted, Color chip)
    {
        id = materialId;
        reactions = accepted ?? Array.Empty<ImpactReaction>();
        debris = chip;
    }

    public bool TryMultiplier(ImpactType type, out float multiplier)
    {
        multiplier = 0f;
        if (type == null || reactions == null)
            return false;

        for (int i = 0; i < reactions.Length; i++)
        {
            ImpactType reaction = reactions[i].Type;
            if (reaction != null && reaction.Same(type))
            {
                multiplier = reactions[i].Multiplier;
                return true;
            }
        }

        return false;
    }
}
