namespace OBJImporter;

using BepInEx;
using HarmonyLib;

/// <summary> General plugin handler. </summary>
[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Load the mod. </summary>
    public void Awake()
    {
        // loading code
        new Harmony(PluginInfo.GUID).PatchAll();
    }
}