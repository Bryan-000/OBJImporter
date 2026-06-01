namespace OBJImporter;

using GameConsole;
using System.IO;
using UnityEngine;

public class ImportCommand : ICommand
{
    public string Name => "Import";
    public string Description => "Imports an OBJ file into ultrakill.";
    public string Command => "import";

    public void Execute(Console con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        Mesh mesh = Importer.CreateMesh(path);
        mesh.name = Path.GetFileNameWithoutExtension(path);

        // create a gameobject with a meshrenderer+filter with the newly created mesh
        GameObject obj = new(mesh.name);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = new(DefaultReferenceManager.Instance.masterShader);

        obj.transform.position = (NewMovement.Instance?.transform ?? Camera.main?.transform ?? Camera.current?.transform)?.position ?? Vector3.zero;
    }
}