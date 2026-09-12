#if DEBUG

namespace ObjImporter;

using GameConsole;
using ObjImporter.API;
using ObjImporter.Tools;
using System.Diagnostics;
using UnityEngine;

internal class ImportCommand : ICommand
{
    public string Name => "Import";
    public string Description => "Imports a .obj file into ultrakill.";
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
            UnityDebug.LogException(ex);
        }
    }
}

internal class ImportBenchmarkCommand : ICommand
{
    public string Name => "ImportBenchmark";
    public string Description => "Benchmarks .obj importing.";
    public string Command => "import_benchmark";
    readonly plog.Logger Log = new("Benchmark");

    public void Execute(Console con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        double start = Time.realtimeSinceStartupAsDouble;
        Stopwatch watch = Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
            Parser.CreateMesh(path, out _, out _);

        watch.Stop();
        double timeTaken = Time.realtimeSinceStartupAsDouble - start;

        Log.Info($"Benchmark results: each import on average took [ {watch.ElapsedMilliseconds / 1000d}ms ] or [ {timeTaken}ms ] to complete.");
    }
}

#endif