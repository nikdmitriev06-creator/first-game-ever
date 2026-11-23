using System;
using UnityEngine;

/// <summary>
/// One visual for an event, optionally limited to a damage type.
/// An empty prefab means the shared placeholder burst is used.
/// </summary>
[Serializable]
public class FxRecipe
{
    public FxEvent Event = FxEvent.Hit;
    public DamageType Type = DamageType.None;
    public Color Color = Color.white;
    public int Count = 4;
    public float Life = 0.32f;
    public float Scale = 0.24f;
    public float Speed = 1.4f;
    public GameObject Prefab;
}

/// <summary>
/// Assigns visuals to events. A new effect is another row, not another class.
/// </summary>
[CreateAssetMenu(fileName = "FxProfile", menuName = "Game/VFX Profile")]
public class FxProfile : ScriptableObject
{
    public FxRecipe[] Recipes = Array.Empty<FxRecipe>();

    public FxRecipe Find(FxEvent ev, DamageType type)
    {
        FxRecipe any = null;
        if (Recipes == null)
            return null;

        for (int i = 0; i < Recipes.Length; i++)
        {
            FxRecipe recipe = Recipes[i];
            if (recipe == null || recipe.Event != ev)
                continue;

            if (recipe.Type == type)
                return recipe;

            if (recipe.Type == DamageType.None && any == null)
                any = recipe;
        }

        return any;
    }
}
