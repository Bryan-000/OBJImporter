namespace ObjImporter;

using BepInEx;
using GameConsole;
using HarmonyLib;

[BepInPlugin(Info.GUID, Info.Name, Info.Version)]
public partial class ObjPlugin : BaseUnityPlugin
{
    internal static ObjPlugin Instance;

    /// <summary> :33333 </summary>
    private void Awake()
    {
        Instance = this;

        if (Info.Debug)
            Harmony.CreateAndPatchAll(GetType(), Info.GUID);
    }

#if DEBUG

    /// <summary> Adds debugging commands for importing to the F8 console when it's created. </summary>
    [HarmonyPostfix] [HarmonyPatch(typeof(Console), "Awake")]
    private static void AddCmdOnConsoleLoad(Console __instance)
    {
        __instance.RegisterCommand(new ImportCommand());
        __instance.RegisterCommand(new ImportBenchmarkCommand());
    }

#endif
}