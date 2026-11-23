using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// The world flags LocationState already keeps, written next to the location progress file.
/// Editor checks do not write it: a save happens only while GameRoot is running.
/// </summary>
public static class WorldMemory
{
    [Serializable]
    class File
    {
        public string[] removed;
        public string[] opened;
        public string[] enabled;
    }

    public static void Load(LocationState state)
    {
        if (state == null || !System.IO.File.Exists(Path()))
            return;

        try
        {
            File file = JsonUtility.FromJson<File>(System.IO.File.ReadAllText(Path()));
            if (file == null)
                return;

            state.ApplyFlags(file.removed, file.opened, file.enabled);
        }
        catch (Exception error)
        {
            Debug.LogWarning("World state was not read: " + error.Message);
        }
    }

    public static void Store(LocationState state)
    {
        if (GameRoot.Active == null || state == null || state != LocationState.Active)
            return;

        var removed = new List<string>();
        var opened = new List<string>();
        var enabled = new List<string>();
        state.CopyFlags(removed, opened, enabled);
        var file = new File
        {
            removed = removed.ToArray(),
            opened = opened.ToArray(),
            enabled = enabled.ToArray()
        };

        try
        {
            System.IO.File.WriteAllText(Path(), JsonUtility.ToJson(file));
        }
        catch (Exception error)
        {
            Debug.LogWarning("World state was not written: " + error.Message);
        }
    }

    public static string Path()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "world-state.json");
    }
}
