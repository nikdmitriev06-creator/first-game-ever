using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpawnEntry
{
    public string id;
    public bool enabled = true;
    [Min(0)] public int weight = 1;
    public GameObject prefab;
    public int minDifficulty = 1;
    [Tooltip("0 means no upper limit.")]
    public int maxDifficulty;
    public string conditionId;
}

/// <summary>
/// Weighted variants for one kind of spawn. Difficulty decides which entries are eligible.
/// </summary>
[CreateAssetMenu(fileName = "SpawnPool", menuName = "Game/Levels/Spawn Pool")]
public class SpawnPoolDefinition : ScriptableObject
{
    [SerializeField] string id = "pool";
    [SerializeField] string displayName = "Пул";
    [SerializeField] SpawnCategory category = SpawnCategory.Monster;
    [SerializeField] SpawnEntry[] entries = Array.Empty<SpawnEntry>();

    public string Id => id;
    public string DisplayName => displayName;
    public SpawnCategory Category => category;
    public IReadOnlyList<SpawnEntry> Entries => entries ?? Array.Empty<SpawnEntry>();

    public void Configure(string poolId, string name, SpawnCategory poolCategory, SpawnEntry[] next)
    {
        id = poolId;
        displayName = name;
        category = poolCategory;
        entries = next ?? Array.Empty<SpawnEntry>();
    }
}
