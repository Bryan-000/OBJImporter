#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

/// <summary> Exports all the assets for the mod into an AssetBundle. </summary>
public class AssetsExporter : EditorWindow
{
    /// <summary> Path to the folder to export to, serialized so it doesn't reset. </summary>
    [SerializeField] private string exportPath; // also i saw some1 put the lil [] thing before the field once and i like it so im using it now :3

    /// <summary> I use this script for multiple mods :P </summary>
    private const string ModName = "OBJImporter";

    /// <summary> Create the Export Assets button and the Assets Exporter window when the buttons clicked. </summary>
    [MenuItem("Assets/Export Assets")]
    public static void OnWindow()
    {
        EditorWindow wnd = GetWindow<AssetsExporter>();
        wnd.titleContent = new GUIContent("Assets Exporter");
    }

    /// <summary> Create the actual UI for the window. </summary>
    public void CreateGUI()
    {
        exportPath = EditorPrefs.GetString($"{ModName}SrcPath");

        rootVisualElement.Add(CreateGap(10f));

        Label title = new($"Select {ModName} source code folder");
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        rootVisualElement.Add(title);

        rootVisualElement.Add(CreateGap(7.5f));

        TextField exportTextField = new("Export Folder Path");
        exportTextField.RegisterValueChangedCallback(Event => EditorPrefs.SetString($"{ModName}SrcPath", exportPath = Event.newValue));
        exportTextField.SetValueWithoutNotify(exportPath);
        rootVisualElement.Add(exportTextField);
        rootVisualElement.Add(CreateGap(5f));

        rootVisualElement.Add(CreateButton("Select Export Folder", 25f, () =>
            exportTextField.value = exportPath =
            EditorUtility.SaveFolderPanel("Select Export Folder", Directory.Exists(exportPath) ? exportPath : Application.dataPath, ModName)));

        rootVisualElement.Add(CreateGap(5f));

        rootVisualElement.Add(CreateButton("Export Assets", 20f, () => Export()));
    }

    /// <summary> Export just the .bundle files to the export folder. </summary>
    public void Export()
    {
        if (!Directory.Exists("Export")) Directory.CreateDirectory("Export");

        BuildPipeline.BuildAssetBundles("Export", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        foreach (string file in Directory.GetFiles("Export", "*.bundle"))
        {
            File.Copy(file, Path.Combine(exportPath, Path.GetFileName(file)), true);
        }
    }

    /// <summary> Creates a silly lil gap in the window thingy. </summary>
    public VisualElement CreateGap(float height)
    {
        VisualElement gap = new();
        gap.style.height = new StyleLength(height);

        return gap;
    }

    /// <summary> Creates a button in the window. </summary>
    public Button CreateButton(string text, float height, Action onClicked)
    {
        Button button = new(onClicked) { text = text };
        button.style.height = new StyleLength(height);

        return button;
    }
}

#endif