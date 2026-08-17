namespace OBJImporter;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public static class Importer
{
    /// <summary> Static PLogger for the importer class so we can send logs directly to the f8 console. </summary>
    public static plog.Logger Log = new("Importer");

    /// <summary> Creates a mesh from a .obj file at the provided path. </summary>
    public static Mesh CreateMesh(string path)
    {
        // clean the path for this specific OS
        path = path.Replace(['\\', '/'], Path.DirectorySeparatorChar);

        if (!path.EndsWith(".obj") || !File.Exists(path))
            throw new FileNotFoundException($"File at path {path} doesn't exist or isn't an obj file.");


        Log.Info($"Creating mesh from obj file at \"{path}\"");
        Stopwatch stopwatch = Stopwatch.StartNew();

        Mesh result = __createMesh(path);

        stopwatch.Stop();
        Log.Info($"Mesh creation took a total of {stopwatch.Elapsed.TotalSeconds} seconds.");

        return result;
    }

    /// <summary> Actual implemention of <see cref="CreateMesh(string)"/>. </summary>
    internal static Mesh __createMesh(string path)
    {
        // go through each line and read the obj's data, variables starting with 'obj_' get modified b4 being fed into the unity mesh
        ExtractOBJData(path,
            out List<Vector3> vertices,  out List<Vector3> obj_normals,   out List<Vector2> obj_UVs,
            out List<int> vertexIndices, out List<int> obj_normalIndices, out List<int> obj_uvIndices
        );


        // sort UV's and normals list for unity, since unity uses the same indices for vertices as for everything else]
        Vector2[] UVs = new Vector2[vertices.Count];
        Vector3[] normals = new Vector3[vertices.Count];
        if (obj_UVs.Count != 0 || obj_normals.Count != 0)
        {
            int i = 0;
            do
            {
                // take the uv at obj_uvIndice in obj_uv's and set the uv at vertexIndice in uv's to that obj_uv
                // so that when unity takes the vertexIndice and looks in the uv's for the uv at that vertexIndice, it gets the right one
                if (obj_UVs.Count != 0) UVs[vertexIndices[i]] = obj_UVs[obj_uvIndices[i]];
                if (obj_normals.Count != 0) normals[vertexIndices[i]] = obj_normals[obj_normalIndices[i]];
            }
            while (++i < vertexIndices.Count);
        }


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

    /// <summary> Reads an obj line by line and parses the data from it into managed C# objects, automatically flipping the model on the X-axis so it works cleanly with Unity. </summary>
    public static void ExtractOBJData(string objPath, out List<Vector3> vertices, out List<Vector3> normals, out List<Vector2> UVs, out List<int> vertexIndices, out List<int> normalIndices, out List<int> uvIndices)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        vertices = [];
        normals = [];
        UVs = [];
        vertexIndices = [];
        normalIndices = [];
        uvIndices = [];

        Dictionary<string, Material> materials = [];
        foreach (string line in File.ReadAllLines(objPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line[0] == 'v')
            {
                // (v) vertice positions :3
                if (line[1] == ' ')
                    vertices.Add(StringToVector3(line[2..]));

                // (vn) normals meow
                else if (line[1] == 'n')
                    normals.Add(StringToVector3(line[3..]));

                // (vt) uv's rawr >:3
                else if (line[1] == 't')
                    UVs.Add(StringToVector2(line[3..]));
            }

            // (f) faces/indicies :p
            else if (line[0] == 'f')
            {
                List<string> parts = [.. line[2..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];

                // triangle strip faces AAAAAAAAAAAAAAA
                if (parts.Count > 3)
                {
                    // reorder the parts list to make multiple triangles out of a strip
                    string[] oldParts = [.. parts];

                    parts.Clear();
                    int i = 1;
                    do
                    {
                        parts.Add(oldParts[0]);
                        parts.Add(oldParts[i]);
                        parts.Add(oldParts[i + 1]);
                    }
                    while (++i < oldParts.Length - 1);
                }

                // reverse winding order
                for (int t = 0; t < parts.Count; t += 3)
                    (parts[t + 1], parts[t + 2]) = (parts[t + 2], parts[t + 1]);

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

            // 
            else if (line.StartsWith("usemtl"))
            {

            }

            // loads a mtl, the doohickey which defines multiple materials and their textures/properties
            else if (line.StartsWith("mtllib"))
            {
                string mtlPath = line[7..]; // line.SubString("mtllib ".Length)

                if (!File.Exists(mtlPath))
                {
                    // try relative path
                    mtlPath = Path.Combine(Path.GetDirectoryName(objPath), mtlPath);

                    // if even the relative path doesnt work, give up lmfao
                    if (!File.Exists(mtlPath))
                        continue;
                }


                ExtractMTLData(mtlPath, ref materials);
            }
        }

        stopwatch.Stop();
        Log.Info($".OBJ mesh data extraction took {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    public static void ExtractMTLData(string mtlPath, ref Dictionary<string, Material> materials)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Material current = null;
        foreach (string line in File.ReadAllLines(mtlPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line.StartsWith("newmtl"))
            {
                current = new(DefaultReferenceManager.Instance.masterShader);
                materials[line[7..]] = current;
            }
            else if (current != null)
            {
                if (line[0] == 'K')
                {
                    if (line[1] == 'a')
                    {
                        
                    }
                    else if (line[1] == 'd')
                    {
                        
                    }
                    else if (line[1] == 's')
                    {
                        
                    }
                }
                else if (line[0] == 'N') // Ns
                {
                    
                }
                else if (line[0] == 'd')
                {
                    
                }
                else if (line[0] == 'm') // map_Kd
                {
                    
                }
            }
        }

        stopwatch.Stop();
        Log.Info($".MTL mesh data extraction took {stopwatch.Elapsed.TotalSeconds} seconds.");
    }
}