namespace OBJImporter;

using GameConsole;
using plog;
using System.Diagnostics;

public class ImportCommand : ICommand
{
    public string Name => "Import";
    public string Description => "Imports an OBJ file into ultrakill.";
    public string Command => "import";

    public void Execute(Console con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        try
        {
            Importer.CreateGameObject(path, NewMovement.Instance?.transform.position);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
        }
    }
}

public class ImportBenchmarkCommand : ICommand
{
    public string Name => "ImportBenchmark";
    public string Description => "Benchmarks .obj importing.";
    public string Command => "import_benchmark";
    readonly Logger Log = new("Benchmark");

    public void Execute(Console con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        Stopwatch watch = Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
            Importer._createMesh(path, out _, out _);

        watch.Stop();

        Log.Info($"Benchmark took {watch.Elapsed.TotalSeconds} seconds to complete, each import on average took {watch.Elapsed.TotalMilliseconds / 1000d} miliseconds to complete.");
    }
}