namespace OBJImporter;

using BepInEx;
using GameConsole;
using HarmonyLib;

/// <summary> General plugin handler. </summary>
[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    public class PluginInfo
    {
        public const string GUID = "Bryan_-000-.OBJImporter";
        public const string Name = "OBJImporter";
        public const string Version = "1.0.0";
    }

    /// <summary> Patch all of our harmony patches as to load the import command when the console is loaded :33333 </summary>
    public void Awake() =>
        new Harmony(PluginInfo.GUID).PatchAll(GetType());


    [HarmonyPostfix] [HarmonyPatch(typeof(Console), "Awake")]
    public static void AddCmdOnConsoleLoad(Console __instance) =>
        __instance.RegisterCommand(new ImportCommand());
}