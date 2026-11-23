using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only difficulty arm. A player build does not contain this script, so it cannot change a run.
/// </summary>
public static class LevelDebugSettings
{
    const string ArmedKey = "Game.LevelDebug.Armed";
    const string DifficultyKey = "Game.LevelDebug.Difficulty";
    const string SeedKey = "Game.LevelDebug.Seed";

    public static bool Armed => EditorPrefs.GetBool(ArmedKey, false);
    public static int Difficulty => Mathf.Max(1, EditorPrefs.GetInt(DifficultyKey, 1));
    public static int Seed => EditorPrefs.GetInt(SeedKey, 1);

    public static void Arm(int difficulty, int seed)
    {
        EditorPrefs.SetBool(ArmedKey, true);
        EditorPrefs.SetInt(DifficultyKey, Mathf.Max(1, difficulty));
        EditorPrefs.SetInt(SeedKey, seed);
    }

    public static void Disarm()
    {
        EditorPrefs.SetBool(ArmedKey, false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        if (!Armed)
        {
            ProgressionService.ClearOverride();
            return;
        }

        ProgressionService.SetRunDifficulty(Difficulty);
        ProgressionService.Run.Seed = Seed;
    }
}
