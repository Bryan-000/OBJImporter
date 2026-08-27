namespace OBJImporter;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary> Helper for parsing .obj files into meshes/GameObjects. </summary>
public static class Importer
{
    /// <summary> Static PLogger for the importer class so we can send logs directly to the f8 console. </summary>
    private static readonly plog.Logger Log = new("Importer");

    /// <summary> Creates a GameObject from an .obj file with optional transform parameters. </summary>
    public static GameObject CreateGameObject(string path, Vector3? position = null, Quaternion? rotation = null, Transform parent = null) =>
        CreateGameObject(path, false, position, rotation, parent);

    /// <summary> Creates a GameObject from an .obj file with optional collision and transform parameters. </summary>
    public static GameObject CreateGameObject(string path, bool hasCollision = false, Vector3? position = null, Quaternion? rotation = null, Transform parent = null) =>
        CreateGameObject(path, hasCollision, position ?? Vector3.zero, rotation ?? Quaternion.identity, parent);

    /// <summary> Creates a GameObject from an .obj file and sets its collision and transform. </summary>
    public static GameObject CreateGameObject(string path, bool hasCollision, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject obj = CreateGameObject(path, out Mesh mesh, out _);
        if (hasCollision)
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;

        Transform trans = obj.transform;
        trans.position = position;
        trans.rotation = rotation;
        trans.SetParent(parent);

        return obj;
    }

    /// <summary> Creates a GameObject from an .obj file. </summary>
    public static GameObject CreateGameObject(string path) => CreateGameObject(path, out _, out _);

    /// <summary> Creates a GameObject from an .obj file and outputs its mesh and materials. </summary>
    public static GameObject CreateGameObject(string path, out Mesh mesh, out Material[] mats)
    {
        (mesh, mats) = CreateMesh(path);

        GameObject obj = new(mesh.name);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterials = mats;

        return obj;
    }

    /// <summary> Creates a mesh and materials from an .obj file. </summary>
    public static (Mesh, Material[]) CreateMesh(string path)
    {
        // clean the path for this specific OS
        path = path.Replace(['\\', '/'], Path.DirectorySeparatorChar);

        if (!path.EndsWith(".obj") || !File.Exists(path))
            throw new FileNotFoundException($"File at '{path}' doesn't exist or isn't an obj file.");


        Debug($"Creating mesh from obj file at '{path}'");
        Stopwatch stopwatch = Stopwatch.StartNew();

        _createMesh(path, out Mesh result, out List<Material> materials);

        stopwatch.Stop();
        Debug($"Mesh creation took a total of {stopwatch.Elapsed.TotalSeconds} seconds.");

        return (result, [.. materials]);
    }

    #region Internal bullshit please dont read this code its ass i hate it

    [Conditional("Debug")]
    private static void Debug(string message) => Log.Info(message);

    internal static void _createMesh(string path, out Mesh mesh, out List<Material> materials)
    {
        // go through each line and read the obj's data, variables starting with 'obj_' get modified b4 being fed into the unity mesh
        _extractOBJData(path,
            out List<Vector3> vertices, out List<Vector3> obj_normals, out List<Vector2> obj_UVs,
            out List<List<int>> subMeshIndices, out List<int> obj_normalIndices, out List<int> obj_uvIndices,
            out materials
        );


        List<int> totalVertexIndices = [];
        foreach (List<int> indices in subMeshIndices)
            totalVertexIndices.AddRange(indices);


        // sort UV's and normals list for unity, since unity uses the same indices for vertices as for everything else]
        Vector2[] UVs = new Vector2[vertices.Count];
        Vector3[] normals = new Vector3[vertices.Count];
        if (obj_UVs.Count != 0 || obj_normals.Count != 0)
        {
            for (int i = 0; i < totalVertexIndices.Count; i++)
            {
                // take the uv at obj_uvIndice in obj_uv's and set the uv at vertexIndice in uv's to that obj_uv
                // so that when unity takes the vertexIndice and looks in the uv's for the uv at that vertexIndice, it gets the right one
                if (obj_UVs.Count != 0) UVs[totalVertexIndices[i]] = obj_UVs[obj_uvIndices[i]];
                if (obj_normals.Count != 0) normals[totalVertexIndices[i]] = obj_normals[obj_normalIndices[i]];
            }
        }


        // turn modified obj data into a mesh :3
        mesh = new()
        {
            name = Path.GetFileNameWithoutExtension(path)
        };

        // set vertices miaaaow
        mesh.SetVertices(vertices);
        mesh.subMeshCount = subMeshIndices.Count;
        for (int i = 0; i < subMeshIndices.Count; i++)
            mesh.SetTriangles(subMeshIndices[i], i);

        // some meshs dont have uv's so check
        if (obj_UVs.Count != 0)
            mesh.SetUVs(0, UVs);

        // same for normals, but calculate them if not
        if (obj_normals.Count != 0)
            mesh.SetNormals(normals);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }

    internal static void _extractOBJData(string objPath,
            out List<Vector3> vertices, out List<Vector3> normals, out List<Vector2> UVs,
            out List<List<int>> vertexIndices, out List<int> normalIndices, out List<int> uvIndices,
            out List<Material> outMaterials
        )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        vertices = []; vertexIndices = [[]];
        normals = [];  normalIndices = [];
        UVs = [];      uvIndices = [];
        outMaterials = [];

        int currentSubMesh = 0;
        Dictionary<string, Material> materials = [];
        foreach (string line in File.ReadLines(objPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line[0] == 'v')
            {
                // (v) vertice positions :3
                if (line[1] == ' ')
                    vertices.Add(IStringToVector3(line[2..]));

                // (vn) normals meow
                else if (line[1] == 'n')
                    normals.Add(IStringToVector3(line[3..]));

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
                    for (int i = 1; i < oldParts.Length - 1; i++)
                    {
                        parts.Add(oldParts[0]);
                        parts.Add(oldParts[i]);
                        parts.Add(oldParts[i + 1]);
                    }
                }

                // reverse winding order
                for (int t = 0; t < parts.Count; t += 3)
                    (parts[t + 1], parts[t + 2]) = (parts[t + 2], parts[t + 1]);

                // f 1 2 3
                if (!line.Contains('/'))
                {
                    vertexIndices[currentSubMesh].AddRange(parts.Select(i => int.Parse(i) - 1));
                }
                else
                {
                    // f v1/u1/n1 v2/u2/n2 v3/u3/n3
                    foreach (string part in parts)
                    {
                        string[] segmentsmeow = part.Split('/');

                        vertexIndices[currentSubMesh].Add(int.Parse(segmentsmeow[0]) - 1); // vertex indicies MUST exist

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
                if (outMaterials.Count != 0)
                {
                    currentSubMesh++;
                    vertexIndices.Add([]);
                }

                string mtlName = line[7..];
                outMaterials.Add(materials[mtlName]);
            }

            // loads a mtl, the doohickey which defines multiple materials and their textures + properties
            else if (line.StartsWith("mtllib"))
            {
                string mtlPath = Path.GetFullPath(line[7..], Path.GetDirectoryName(objPath));

                stopwatch.Stop();
                _extractMTLData(mtlPath, ref materials);
                stopwatch.Start();
            }
        }

        stopwatch.Stop();
        Debug($".OBJ mesh data extraction took {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    internal static void _extractMTLData(string mtlPath, ref Dictionary<string, Material> materials)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Material current = null;
        foreach (string line in File.ReadLines(mtlPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (current != null)
            {
                if (line[0] == 'K' && line[1] == 'd') // color
                {
                    (float r, float g, float b) = StringToFloat3(line[3..]);

                    current.SetColor(UKMaster._Color, new(r, g, b));
                }
                else if (line[0] == 'd') // opacity
                {
                    float opacity = float.Parse(line[2..]);

                    current.SetFloat(UKMaster._Opacity, opacity);
                }
                else if (line[0] == 'm' && line.StartsWith("map_Kd")) // texture
                {
                    string texPath = Path.GetFullPath(line[7..], Path.GetDirectoryName(mtlPath));

                    Texture2D tex = new(0, 0);
                    tex.LoadImage(File.ReadAllBytes(texPath));

                        current.SetTexture(UKMaster._MainTex, tex);
                }
            }

            if (line[0] == 'n' && line.StartsWith("newmtl"))
            {
                current = new(DefaultReferenceManager.Instance.masterShader);
                current.ChangeBlendModeToTransparent();
                current.name = line[7..];

                materials[current.name] = current;
            }
        }

        stopwatch.Stop();
        Debug($".MTL mesh data extraction took {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    #endregion
}