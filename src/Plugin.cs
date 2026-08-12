namespace OBJImporter;

using BepInEx;
using GameConsole;
using HarmonyLib;

[BepInPlugin(Information.GUID, Information.Name, Information.Version)]
public class Plugin : BaseUnityPlugin
{
    public static class Information
    {
        public const string GUID = "Bryan_-000-.OBJImporter";
        public const string Name = "OBJImporter";
        public const string Version = "1.0.0";
    }

    /// <summary> :33333 </summary>
    public void Awake() =>
        new Harmony(Information.GUID).PatchAll(GetType());

    /// <summary> Adds our command to the F8 console after it's done initalizing. </summary>
    [HarmonyPostfix] [HarmonyPatch(typeof(Console), "Awake")]
    public static void AddCmdOnConsoleLoad(Console __instance) =>
        __instance.RegisterCommand(new ImportCommand());
}