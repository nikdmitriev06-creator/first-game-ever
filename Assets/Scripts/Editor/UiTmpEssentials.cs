using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TextMesh Pro needs its distance-field shader before UI labels can draw.
/// The shader ships inside the uGUI package already used by this project.
/// </summary>
public static class UiTmpEssentials
{
    const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Tools/UI/Import TMP Essentials")]
    public static void Import()
    {
        if (File.Exists(SettingsPath))
        {
            Debug.Log("TMP essentials are already in the project.");
            return;
        }

        string package = PackageFile();
        if (string.IsNullOrEmpty(package))
        {
            Debug.LogError("TMP essentials package was not found in com.unity.ugui.");
            return;
        }

#pragma warning disable CS0618
        AssetDatabase.ImportPackage(package, false);
#pragma warning restore CS0618
    }

    [InitializeOnLoadMethod]
    static void ImportWhenMissing()
    {
        EditorApplication.delayCall += TryImport;
    }

    static void TryImport()
    {
        if (File.Exists(SettingsPath))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            int tries = SessionState.GetInt("UiTmpTries", 0);
            if (tries > 40)
                return;

            SessionState.SetInt("UiTmpTries", tries + 1);
            EditorApplication.delayCall += TryImport;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (SessionState.GetBool("UiTmpEssentials", false))
            return;

        SessionState.SetBool("UiTmpEssentials", true);
        Import();
    }

    static string PackageFile()
    {
        UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        for (int i = 0; i < packages.Length; i++)
        {
            if (packages[i].name != "com.unity.ugui")
                continue;

            string file = Path.Combine(packages[i].resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (File.Exists(file))
                return file;
        }

        return null;
    }
}
