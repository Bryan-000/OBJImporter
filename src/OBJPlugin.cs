namespace OBJImporter;

using BepInEx;
using GameConsole;
using HarmonyLib;

[BepInPlugin(Information.GUID, Information.Name, Information.Version)]
public class OBJPlugin : BaseUnityPlugin
{
    public static class Information
    {
        public const string GUID = "Bryan_-000-.OBJImporter";
        public const string Name = "OBJImporter";
        public const string Version = "1.0.0";

#if Debug
        public const bool Debug = true;
#else
        public const bool Debug = false;
#endif
    }

    internal static OBJPlugin Instance;

    /// <summary> :33333 </summary>
    private void Awake()
    {
        Instance = this;

        Harmony.CreateAndPatchAll(GetType(), Information.GUID);
    }

    /// <summary> Adds our command to the F8 console when it's created. </summary>
    [HarmonyPostfix] [HarmonyPatch(typeof(Console), "Awake")]
    private static void AddCmdOnConsoleLoad(Console __instance)
    {
        __instance.RegisterCommand(new ImportCommand());
        __instance.RegisterCommand(new ImportBenchmarkCommand());
    }
}