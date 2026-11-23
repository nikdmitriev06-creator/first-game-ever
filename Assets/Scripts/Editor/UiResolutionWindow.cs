using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Switches the Game view to the resolutions the HUD is built for.
/// The canvas scaler reads that view, so this is the check for anchors and type.
/// A standalone player still follows the same scaler when the window is resized.
/// </summary>
public class UiResolutionWindow : EditorWindow
{
    static readonly Vector2Int[] Sizes =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3440, 1440)
    };

    string _note = "Pick a size. The Game view switches to a fixed resolution.";

    [MenuItem("Tools/UI/Resolutions")]
    static void Open()
    {
        GetWindow<UiResolutionWindow>("UI Resolutions");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Game view", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Each button adds that fixed size, if it is missing, and selects it. Enter Play to see the HUD. Resize the standalone window to check live scaling.", MessageType.Info);
        for (int i = 0; i < Sizes.Length; i++)
        {
            Vector2Int size = Sizes[i];
            if (GUILayout.Button(size.x + " × " + size.y))
                _note = Apply(size.x, size.y);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_note, MessageType.None);
    }

    public static string Apply(int width, int height)
    {
        try
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editor.GetType("UnityEditor.GameViewSize");
            Type kindType = editor.GetType("UnityEditor.GameViewSizeType");
            Type viewType = editor.GetType("UnityEditor.GameView");
            if (sizesType == null || sizeType == null || kindType == null || viewType == null)
                return "This Editor build does not expose the Game view size list.";

            object sizes = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object group = sizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizes);
            Type groupType = group.GetType();
            int builtin = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
            int custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
            string label = width + "x" + height + " UI";
            int index = -1;
            for (int i = 0; i < custom; i++)
            {
                object existing = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { builtin + i });
                int existingWidth = (int)sizeType.GetProperty("width").GetValue(existing);
                int existingHeight = (int)sizeType.GetProperty("height").GetValue(existing);
                if (existingWidth == width && existingHeight == height)
                {
                    index = builtin + i;
                    break;
                }
            }

            if (index < 0)
            {
                object kind = Enum.Parse(kindType, "FixedResolution");
                ConstructorInfo ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                object created = ctor.Invoke(new[] { kind, width, height, label });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
                custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
                index = builtin + custom - 1;
            }

            EditorWindow view = EditorWindow.GetWindow(viewType);
            PropertyInfo selected = viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selected.SetValue(view, index);
            view.Repaint();
            return "Game view is " + width + "×" + height + ".";
        }
        catch (Exception error)
        {
            return "Could not switch the Game view (" + error.GetType().Name + "). Choose " + width + "×" + height + " from the Game view size menu.";
        }
    }
}
