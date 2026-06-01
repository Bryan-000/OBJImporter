namespace OBJImporter;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Importer
{
    /// <summary> Creates a mesh from a .obj file at the provided path. </summary>
    public static Mesh CreateMesh(string path)
    {
        if (!path.EndsWith(".obj") || !File.Exists(path))
            throw new FileNotFoundException($"File at path {path} doesn't exist or isn't an obj file.");

        return CreateMesh(File.ReadAllLines(path));
    }

    /// <summary> Creates a mesh from the lines of a .obj file. </summary>
    public static Mesh CreateMesh(IEnumerable<string> lines)
    {
        // go through each line and read the obj's data, variables starting with 'obj_' get modified b4 being fed into the unity mesh
        ExtractData(lines, out List<Vector3> vertices, out List<Vector3> obj_normals, out List<Vector2> obj_UVs,
                           out List<int> vertexIndices, out List<int> obj_normalIndices, out List<int> obj_uvIndices);


        // sort UV's and normals list for unity, since unity uses the same indices for vertices as for everything else
        Vector2[] UVs = new Vector2[vertices.Count];
        if (obj_UVs.Count != 0)                                    // take the uv at obj_uvIndice in obj_uv's
            for (int i = 0; i < vertexIndices.Count; i++)          // and set the uv at vertexIndice in uv's to that obj_uv
                UVs[vertexIndices[i]] = obj_UVs[obj_uvIndices[i]];
                                                                   // so that when unity takes the vertexIndice
        Vector3[] normals = new Vector3[vertices.Count];           // and looks in the uv's for the uv, it gets the right one
        if (obj_normals.Count != 0)
            for (int i = 0; i < vertexIndices.Count; i++)
                normals[vertexIndices[i]] = obj_normals[obj_normalIndices[i]];


        // turn modified obj data into a mesh :3
        Mesh mesh = new();

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

        return mesh;
    }

    /// <summary> Reads an obj line by line and parses the data from it into managed C# objects. </summary>
    public static void ExtractData(IEnumerable<string> lines, out List<Vector3> vertices, out List<Vector3> normals, out List<Vector2> UVs, out List<int> vertexIndices, out List<int> normalIndices, out List<int> uvIndices)
    {
        vertices = [];
        normals = [];
        UVs = [];
        vertexIndices = [];
        normalIndices = [];
        uvIndices = [];

        foreach (string line in lines)
        {
            if (line.StartsWith('#') || line.StartsWith("mtllib"))
                continue;

            // vertice positions :3
            if (line.StartsWith("v "))
                vertices.Add(StringToVector3(line[2..]));

            // normals meow
            else if (line.StartsWith("vn "))
                normals.Add(StringToVector3(line[3..]));

            // uv's rawr >:3
            else if (line.StartsWith("vt "))
                UVs.Add(StringToVector2(line[3..]));

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
                    for (int i = 1; i < oldParts.Length - 1; i++)
                    {
                        parts.Add(oldParts[0]);
                        parts.Add(oldParts[i]);
                        parts.Add(oldParts[i + 1]);
                    }
                }

                // f 1 2 3
                if (!line.Contains('/'))
                {
                    vertexIndices.AddRange(parts.Select(i => int.Parse(i) - 1));
                }
                else
                {
                    // f v1/u1/n1 v2/u2/n2 v3/u3/n3
                    foreach (string part in parts)
                    {
                        string[] segmentsmeow = part.Split('/');

                        vertexIndices.Add(int.Parse(segmentsmeow[0]) - 1); // vertex indicies MUST exist

                        // either UV indices or normal indicies could maybe not exist if this model doesnt have uv's or normals
                        // and in those cases it just does `f v1//n1 v2//n2 v3//n3` or `f v1/u1/ v2/u2/ v3/u3/`
                        if (int.TryParse(segmentsmeow[1], out int uI)) uvIndices.Add(uI - 1);
                        if (int.TryParse(segmentsmeow[2], out int nI)) normalIndices.Add(nI - 1);
                    }
                }
            }
        }
    }

    #region Tools

    /// <summary> Converts a string into a Vector3. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 StringToVector3(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Vector3 pos = new(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));

        return pos;
    }

    /// <summary> Converts a string into a Vector2. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 StringToVector2(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Vector2 pos = new(float.Parse(parts[0]), float.Parse(parts[1]));

        return pos;
    }

    #endregion
}
