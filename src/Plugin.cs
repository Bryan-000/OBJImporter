namespace OBJImporter;

using BepInEx;
using GameConsole;
using HarmonyLib;

/// <summary> General plugin handler. </summary>
[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Patch all of our harmony patches as to load the import command when the console is loaded :33333 </summary>
    public void Awake() =>
        new Harmony(PluginInfo.GUID).PatchAll(GetType());

    [HarmonyPostfix] [HarmonyPatch(typeof(Console), "Awake")]
    public static void AddCmdOnConsoleLoad(Console __instance) =>
        __instance.RegisterCommand(new ImportCommand());
}