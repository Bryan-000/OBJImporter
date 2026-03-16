namespace OBJImporter;

using GameConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using PLogConsole = GameConsole.Console;

public class ImportCommand : ICommand
{
    public string Name => "Import";
    public string Description => "Imports an OBJ file into ultrakill.";
    public string Command => "import";

    public void Execute(PLogConsole con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        if (!path.EndsWith(".obj") || !File.Exists(path))
            Debug.LogError("File at path {path} doesn't exist or isn't an obj file.");

        // get all lines since everything important to us in an obj is split by line
        IEnumerable<string> lines = File.ReadAllLines(path);

        // go through each line and read the obj
        List<Vector3> vertices = [];
        List<Vector3> obj_normals = [];
        List<Vector2> obj_UVs = [];
        List<int> vertexIndices = [];
        List<int> obj_normalIndices = []; // obj stands for "OH!! BLOWJOB!!"
        List<int> obj_uvIndices = [];
        foreach (string line in lines)
        {
            if (line.StartsWith('#') || line.StartsWith("mtllib"))
                continue;

            // vertice positions :3
            if (line.StartsWith("v "))
                vertices.Add(StringToVector3(line[2..]));

            // normals meow
            else if (line.StartsWith("vn "))
                obj_normals.Add(StringToVector3(line[3..]));

            // uv's rawr >:3
            else if (line.StartsWith("vt "))
                obj_UVs.Add(StringToVector2(line[3..]));

            // faces/indicies :p
            else if (line.StartsWith("f "))
            {
                List<string> parts = [.. line[2..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];

                // triangle strip faces AAAAAAAAAAAAAAA
                if (parts.Count > 3)
                {
                    // reorder the parts list to make multiple triangles out of a strip
                    string[] oldParts = [.. parts];

                    parts.Clear();
                    for (int i = 1; i < oldParts.Length-1; i++)
                    {
                        parts.Add(oldParts[0]);
                        parts.Add(oldParts[i]);
                        parts.Add(oldParts[i+1]);
                    }
                }

                // f 1 2 3
                if (!line.Contains('/'))
                    vertexIndices.AddRange(parts.Select(i => int.Parse(i)-1));

                // f v1/u1/n1 v2/u2/n2 v3/u3/n3
                else foreach (string part in parts)
                {
                    string[] segmentsmeow = part.Split('/');
                    
                        vertexIndices.Add(int.Parse(segmentsmeow[0])-1); // vertex indicies MUST exist
                    
                    // either UV indices or normal indicies could maybe not exist if this model doesnt have uv's or normals
                    // and in those cases it just does `f v1//n1 v2//n2 v3//n3` or `f v1/u1/ v2/u2/ v3/u3/`
                        if (int.TryParse(segmentsmeow[1], out int uI)) obj_uvIndices.Add(uI-1);
                        if (int.TryParse(segmentsmeow[2], out int nI)) obj_normalIndices.Add(nI-1);
                }
            }
        }

        // sort UV's and normals list since unity uses the same indices for vertices as for everything else
        Vector2[] UVs = new Vector2[vertices.Count];
        if (obj_UVs.Count != 0)
            for (int i = 0; i < vertexIndices.Count; i++)
                UVs[vertexIndices[i]] = obj_UVs[obj_uvIndices[i]];

        Vector3[] normals = new Vector3[vertices.Count];
        if (obj_normals.Count != 0)
            for (int i = 0; i < vertexIndices.Count; i++)
                normals[vertexIndices[i]] = obj_normals[obj_normalIndices[i]];

        // turn obj data into a mesh :3
        Mesh mesh = new()
        {
            name = Path.GetFileNameWithoutExtension(path)
        };

        // set vertices miaaaow
        mesh.SetVertices(vertices);
        mesh.SetIndices(vertexIndices, MeshTopology.Triangles, 0);

        // some meshs dont have uv's so check
        if (obj_UVs.Count != 0)
            mesh.SetUVs(0, UVs);
        else
            mesh.RecalculateUVDistributionMetric(0);

        // same for normals
        if (obj_normals.Count != 0)
            mesh.SetNormals(normals);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        // create gameobject with mesh
        GameObject obj = new(mesh.name);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = new(DefaultReferenceManager.Instance.masterShader);

        obj.transform.position = NewMovement.Instance?.transform.position ?? Vector3.zero;
    }
}