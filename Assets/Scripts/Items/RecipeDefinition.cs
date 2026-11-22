using System;
using UnityEngine;

[Serializable]
public struct Ingredient
{
    public string ItemId;
    public int Count;
}

/// <summary>
/// One craft. The station id is empty when the recipe can be made by hand.
/// </summary>
[CreateAssetMenu(fileName = "Recipe", menuName = "Game/Recipe")]
public class RecipeDefinition : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] string displayName;
    [SerializeField] string displayNameEnglish;
    [SerializeField] Ingredient[] ingredients = Array.Empty<Ingredient>();
    [SerializeField] string resultId;
    [SerializeField] int resultCount = 1;
    [SerializeField] string stationId;
    [SerializeField] string unlockId;

    public string Id => id;
    public string DisplayName => displayName;
    public string DisplayNameEnglish => displayNameEnglish;
    public Ingredient[] Ingredients => ingredients;
    public string ResultId => resultId;
    public int ResultCount => resultCount;
    public string StationId => stationId;
    public bool NeedsStation => !string.IsNullOrEmpty(stationId);
    public string UnlockId => unlockId;
    public bool NeedsUnlock => !string.IsNullOrEmpty(unlockId);

    public void Configure(
        string recipeId,
        string russianName,
        string englishName,
        string result,
        int count,
        string station,
        Ingredient[] cost)
    {
        id = recipeId;
        displayName = russianName;
        displayNameEnglish = englishName;
        resultId = result;
        resultCount = Mathf.Max(1, count);
        stationId = station;
        ingredients = cost ?? Array.Empty<Ingredient>();
    }

    public void AssignUnlock(string gateId)
    {
        unlockId = gateId;
    }
}
