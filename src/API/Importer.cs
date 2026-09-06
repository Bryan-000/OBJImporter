namespace OBJImporter.API;

using OBJImporter.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary> Helper for parsing .obj files into meshes/GameObjects. </summary>
public static class Importer
{
    /// <summary> Creates a GameObject from an .obj file with optional transform parameters. </summary>
    public static GameObject CreateGameObject(string path, Vector3? position = null, Quaternion? rotation = null, Transform parent = null, bool lighting = false) =>
        CreateGameObject(path, false, position, rotation, parent, lighting);

    /// <summary> Creates a GameObject from an .obj file with optional collision and transform parameters. </summary>
    public static GameObject CreateGameObject(string path, bool hasCollision = false, Vector3? position = null, Quaternion? rotation = null, Transform parent = null, bool lighting = false) =>
        CreateGameObject(path, hasCollision, position ?? Vector3.zero, rotation ?? Quaternion.identity, parent, lighting);

    /// <summary> Creates a GameObject from an .obj file and sets its collision and transform. </summary>
    public static GameObject CreateGameObject(string path, bool hasCollision, Vector3 position, Quaternion rotation, Transform parent, bool lighting = false)
    {
        GameObject obj = CreateGameObject(path, out Mesh mesh, out _, lighting);
        if (hasCollision)
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;

        Transform trans = obj.transform;
        trans.position = position;
        trans.rotation = rotation;
        trans.SetParent(parent);

        return obj;
    }

    /// <summary> Creates a GameObject from an .obj file. </summary>
    public static GameObject CreateGameObject(string path, bool lighting = false) => CreateGameObject(path, out _, out _, lighting);

    /// <summary> Creates a GameObject from an .obj file and outputs its mesh and materials. </summary>
    public static GameObject CreateGameObject(string path, out Mesh mesh, out Material[] mats, bool lighting = false)
    {
        (mesh, mats) = CreateMesh(path, lighting);

        GameObject obj = new()
        {
            name = mesh.name,
            hideFlags = HideFlags.HideAndDontSave
        };

        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterials = mats;

        return obj;
    }

    /// <summary> Creates a mesh and materials from an .obj file. </summary>
    public static (Mesh, Material[]) CreateMesh(string path, bool lighting = false)
    {
        // clean the path for this specific OS
        path = path.Replace(['\\', '/'], Path.DirectorySeparatorChar);

        if (!path.EndsWith(".obj") || !File.Exists(path))
            throw new FileNotFoundException($"File at '{path}' doesn't exist or isn't an obj file.");


        _logDebug($"Creating mesh from obj file at '{path}'");
        Stopwatch stopwatch = Stopwatch.StartNew();

        _createMesh(path, out Mesh result, out List<Material> materials, lighting);

        stopwatch.Stop();
        _logDebug($"Mesh creation took a total of {stopwatch.Elapsed.TotalMilliseconds}ms");

        return (result, [.. materials]);
    }

    // these methods start with underscores so even when some1 runs this through a publicizer they dont accidentally call these instead of the main funcs
    #region Internal bullshit please dont read this code its ass i hate it

    /// <summary> Static PLogger for the importer class so we can send logs directly to the f8 console. </summary>
    private static readonly plog.Logger _log = new("Importer");

    [Conditional("Debug")]
    private static void _logDebug(string message) =>
        _log.Info(message);

    internal static void _createMesh(string path, out Mesh mesh, out List<Material> materials, bool lighting = false)
    {
        // go through each line and read the obj's data, variables starting with 'obj_' get modified b4 being fed into the unity mesh
        _extractOBJData(path,
            out List<Vector3> vertices, out List<Vector3> obj_normals, out List<Vector2> obj_UVs,
            out List<List<int>> subMeshIndices, out List<int> obj_normalIndices, out List<int> obj_uvIndices,
            out materials
        );


        if (lighting)
            foreach (Material mat in materials)
            {
                mat.EnableKeyword(UKMaster.VERTEX_LIGHTING);
            }


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
                if (obj_UVs.Count != 0)  UVs[totalVertexIndices[i]] = obj_UVs[obj_uvIndices[i]];
                if (obj_normals.Count != 0)  normals[totalVertexIndices[i]] = obj_normals[obj_normalIndices[i]];
            }
        }


        // turn modified obj data into a mesh :3
        mesh = new()
        {
            name = Path.GetFileNameWithoutExtension(path),
            hideFlags = HideFlags.HideAndDontSave
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
        foreach (ReadOnlySpan<char> line in File.ReadLines(objPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            switch (line[0])
            {
                // vertices, normals, uv's
                // uses I_ToVector3 to flip the models on the x-axis cuz unity cords and .obj cords are different
                case 'v':
                    switch (line[1])
                    {
                        // (v) vertice positions :3
                        case ' ':
                            Vector3 vertex = Parser.I_ToVector3(line[2..], out int cursor);
                            if (Parser.TryReadFloat(line[2..], ref cursor, out float scaler))
                                vertex *= scaler;

                            vertices.Add(vertex);
                        break;

                        // (vn) normals meow
                        case 'n':
                            normals.Add(Parser.I_ToVector3(line[3..]));
                        break;

                        // (vt) uv's rawr >:3
                        case 't':
                            UVs.Add(Parser.ToVector2(line[3..]));
                        break;
                    }
                break;

                // (f) faces/indicies :p
                case 'f':
                    List<(int v, int? vt, int? vn)> parts = new(3);
                    bool justVertex = !Parser.Contains(line, '/');

                    // parse the line
                    int position = 2;
                    try
                    {
                        while (Parser.SeekAndSlice(line, ' ', ref position, out var sliceStr))
                        {
                            if (justVertex)
                            {
                                parts.Add((int.Parse(sliceStr), null, null));
                            }
                            else
                            {
                                int slicePos = 0;
                                if (Parser.SeekAndSlice(sliceStr, '/', ref slicePos, out var vStr))
                                {
                                    // unity indices start from 0 while .obj's start from 1, so remove 1
                                    int v = Parser.ParseInt(vStr) - 1;

                                    int? vt = Parser.SeekAndSlice(sliceStr, '/', ref slicePos, out var vtStr)
                                        ? Parser.ParseInt(vtStr) - 1 : null;

                                    int? vn = Parser.SeekAndSlice(sliceStr, '/', ref slicePos, out var vnStr)
                                        ? Parser.ParseInt(vnStr) - 1 : null;

                                    parts.Add((v, vt, vn));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Face read error, failed to parse line '{line.ToString()}', last position: '{position}'", ex);
                    }

                    // add to the lists while reversing the winding order since we flip the models on the x-axis
                    // if theres more than 3 parts in this face, that means its a strip, and we need to parse it as multiple faces
                    if (parts.Count > 3)
                    {
                        // convert strip faces into multiple triangles before adding them to the lists
                        for (int i = 1; i < parts.Count - 1; i++)
                        {
                            _addToIndices(parts[0], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                            _addToIndices(parts[i + 1], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                            _addToIndices(parts[i], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                        }
                    }
                    else
                    {
                        // why cant local functions use out params
                        _addToIndices(parts[0], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                        _addToIndices(parts[2], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                        _addToIndices(parts[1], vertexIndices[currentSubMesh], uvIndices, normalIndices);
                    }
                break;

                // sets up the next indices to be in a diff submesh
                // and sets right material to be used for those
                case 'u' when line.StartsWith("usemtl"):
                    if (outMaterials.Count != 0)
                    {
                        currentSubMesh++;
                        vertexIndices.Add([]);
                    }

                    string mtlName = line[7..].ToString();
                    outMaterials.Add(materials[mtlName]);
                break;

                // loads a mtl, the doohickey which defines multiple materials and their textures + properties
                case 'm' when line.StartsWith("mtllib"):
                    string mtlPath = Path.GetFullPath(line[7..].ToString(), Path.GetDirectoryName(objPath));

                    stopwatch.Stop();
                    if (mtlPath.EndsWith(".mtl") && File.Exists(mtlPath))
                        _extractMTLData(mtlPath, ref materials);
                    stopwatch.Start();
                break;
            }
        }

        // if there arent any materials, make a blank one
        if (outMaterials.Count == 0)
        {
            Material blank = new(UKMaster.shader);
            blank.ChangeBlendModeToTransparent();

            outMaterials.Add(blank);
        }

        stopwatch.Stop();
        _logDebug($".OBJ mesh data extraction took {stopwatch.Elapsed.TotalMilliseconds}ms");
    }

    private static void _addToIndices((int v, int? vt, int? vn) vertex, List<int> subVertexIndices, List<int> uvIndices, List<int> normalIndices)
    {
        subVertexIndices.Add(vertex.v);
        if (vertex.vt.HasValue) uvIndices.Add(vertex.vt.Value);
        if (vertex.vn.HasValue) normalIndices.Add(vertex.vn.Value);
    }

    internal static void _extractMTLData(string mtlPath, ref Dictionary<string, Material> materials)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Material current = null;
        foreach (ReadOnlySpan<char> line in File.ReadLines(mtlPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (current != null)
            {
                switch (line[0])
                {
                    // color
                    case 'K' when line[1] == 'd':
                        current.SetColor(UKMaster._Color, Parser.ToColor(line[3..]));
                    break;

                    // opacity
                    case 'd':
                        current.SetFloat(UKMaster._Opacity, float.Parse(line[2..]));
                    break;

                    // inverse opacity
                    case 'T' when line[1] == 'r':
                        current.SetFloat(UKMaster._Opacity, 1f - float.Parse(line[2..]));
                    break;

                    // texture
                    case 'm' when line.StartsWith("map_Kd"):
                        string texPath = Path.GetFullPath(line[7..].ToString(), Path.GetDirectoryName(mtlPath));

                        OBJPlugin.Instance.StartCoroutine(_assignTextureCorountine(current, texPath));
                    break;
                }
            }

            if (line[0] == 'n' && line.StartsWith("newmtl"))
            {
                current = new(UKMaster.shader)
                {
                    name = line[7..].ToString(),
                    hideFlags = HideFlags.HideAndDontSave
                };

                current.ChangeBlendModeToTransparent();
                materials[current.name] = current;
            }
        }

        stopwatch.Stop();
        _logDebug($".MTL mesh data extraction took {stopwatch.Elapsed.TotalMilliseconds}ms");
    }

    private static IEnumerator _assignTextureCorountine(Material target, string texPath)
    {
        Task<byte[]> readFile = File.ReadAllBytesAsync(texPath);
        while (!readFile.IsCompleted)
            yield return null;

        if (readFile.IsCompletedSuccessfully)
        {
            Texture2D tex = new(0, 0);
            tex.name = Path.GetFileName(texPath);

            if (tex.LoadImage(readFile.Result))
                target.SetTexture(UKMaster._MainTex, tex);
        }
        else
        {
            UnityEngine.Debug.LogException(new(
                $"Failed to load texture at path '{texPath}', status: '{readFile.Status}'.",
                readFile.Exception
            ));
        }
    }

    #endregion
}