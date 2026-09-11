namespace OBJImporter.Tools;

using OBJImporter.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using static OBJPlugin;

/// <summary>
/// This is an internal logic class, <para/>
/// please use <see cref="Importer"/> unless you *have to* use this and you know what you're doing.
/// </summary>
public static class Parser
{
    private static readonly plog.Logger Log = new("Parser");

    [Conditional("DEBUG")]
    private static void LogDebug(string message) =>
        Log.Info(message);

    public static void CreateMesh(string path, out Mesh mesh, out List<Material> materials, bool lighting = false)
    {
        // go through each line and read the obj's data, variables starting with 'obj_' get modified b4 being fed into the unity mesh
        ExtractOBJData(path,
            out List<Vector3> rawVectices, out List<Vector3> rawNormals, out List<Vector2> rawUVs,
            out List<Vector3> vertices,    out List<Vector3> normals,    out List<Vector2> UVs,
            out List<List<int>> subMeshIndices, out materials
        );


        if (lighting)
            foreach (Material mat in materials)
            {
                mat.EnableKeyword(UKMaster.VERTEX_LIGHTING);
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
        if (rawUVs.Count != 0)
            mesh.SetUVs(0, UVs);

        // same for normals, but calculate them if not
        if (rawNormals.Count != 0)
            mesh.SetNormals(normals);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }

    public static void ExtractOBJData(string objPath,
            out List<Vector3> rawVertices, out List<Vector3> rawNormals, out List<Vector2> rawUVs,
            out List<Vector3> vertices,    out List<Vector3> normals,    out List<Vector2> UVs,
            out List<List<int>> indices,   out List<Material> outMaterials
        )
    {
        Stopwatch stopwatch;
        if (Info.Debug)
            stopwatch = Stopwatch.StartNew();

        rawVertices = []; vertices = [];
        rawNormals = [];  normals = [];
        rawUVs = [];      UVs = [];

        indices = [[]];
        outMaterials = [];

        int currentSubMesh = 0;
        Dictionary<string, Material> materials = [];
        Dictionary<(int v, int? vt, int? vn), int> vertexCache = [];
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
                            Vector3 vertex = ParseHelper.I_ToVector3(line[2..], out int cursor);
                            if (ParseHelper.TryReadFloat(line[2..], ref cursor, out float scaler))
                                vertex *= scaler;

                            rawVertices.Add(vertex);
                            break;

                        // (vn) normals meow
                        case 'n':
                            rawNormals.Add(ParseHelper.I_ToVector3(line[3..]));
                            break;

                        // (vt) uv's rawr >:3
                        case 't':
                            rawUVs.Add(ParseHelper.ToVector2(line[3..]));
                            break;
                    }
                    break;

                // (f) faces/indicies :p
                case 'f':
                    List<(int v, int? vt, int? vn)> parts = new(3);
                    bool justVertex = !ParseHelper.Contains(line, '/');

                    // parse the line
                    int position = 2;
                    try
                    {
                        while (ParseHelper.SeekAndSlice(line, ' ', ref position, out var sliceStr))
                        {
                            if (justVertex)
                            {
                                parts.Add((int.Parse(sliceStr), null, null));
                            }
                            else
                            {
                                int slicePos = 0;
                                if (ParseHelper.SeekAndSlice(sliceStr, '/', ref slicePos, out var vStr))
                                {
                                    // unity indices start from 0 while .obj's start from 1, so remove 1
                                    int v = ParseHelper.ParseInt(vStr) - 1;

                                    int? vt = ParseHelper.SeekAndSlice(sliceStr, '/', ref slicePos, out var vtStr)
                                        ? ParseHelper.ParseInt(vtStr) - 1 : null;

                                    int? vn = ParseHelper.SeekAndSlice(sliceStr, '/', ref slicePos, out var vnStr)
                                        ? ParseHelper.ParseInt(vnStr) - 1 : null;

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
                    // if theres more than 3 parts in this face, that means its a strip, and we need to auto-triangulate it
                    if (parts.Count > 3)
                    {
                        // convert strip faces into multiple triangles by triangulating them
                        for (int i = 1; i < parts.Count - 1; i++)
                        {
                            Add(parts[0], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                            Add(parts[i + 1], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                            Add(parts[i], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                        }
                    }
                    else
                    {
                        Add(parts[0], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                        Add(parts[2], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                        Add(parts[1], indices[currentSubMesh], rawVertices, rawNormals, rawUVs, vertices, normals, UVs);
                    }

                    // unity doesnt have separate indices for uv's and normals unlike .obj's
                    // so add the vertex properties in a fancy new way so it all lines up
                    void Add((int v, int? vt, int? vn) vertex, List<int> indices,
                            List<Vector3> rawVertices, List<Vector3> rawNormals, List<Vector2> rawUVs,
                            List<Vector3> vertices,    List<Vector3> normals,    List<Vector2> UVs
                        )
                    {
                        if (vertexCache.TryGetValue(vertex, out int existingIndice))
                        {
                            indices.Add(existingIndice);
                        }
                        else
                        {
                            vertices.Add(rawVertices[vertex.v]);

                            UVs.Add(vertex.vt.HasValue ? rawUVs[vertex.vt.Value] : Vector2.zero);
                            normals.Add(vertex.vn.HasValue ? rawNormals[vertex.vn.Value] : Vector3.zero);

                            indices.Add(vertexCache[vertex] = vertices.Count - 1);
                        }
                    }
                    break;

                // sets up the next indices to be in a diff submesh
                // and sets right material to be used for those
                case 'u' when line.StartsWith("usemtl"):
                    if (outMaterials.Count != 0)
                    {
                        currentSubMesh++;
                        indices.Add([]);
                    }

                    string mtlName = line[7..].ToString();
                    if (!materials.TryGetValue(mtlName, out Material mat))
                        mat = Assets.MissingTex_Mat;

                    outMaterials.Add(mat);
                    break;

                // loads a mtl, the doohickey which defines multiple materials and their textures + properties
                case 'm' when line.StartsWith("mtllib"):
                    string mtlPath = Path.GetFullPath(line[7..].ToString(), Path.GetDirectoryName(objPath));

                    stopwatch.Stop();
                    ExtractMTLData(mtlPath, ref materials);
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

        if (Info.Debug)
        {
            stopwatch.Stop();
            LogDebug($".OBJ mesh data extraction took {stopwatch.Elapsed.TotalMilliseconds}ms");
        }
    }

    public static void ExtractMTLData(string mtlPath, ref Dictionary<string, Material> materials)
    {
        try
        {
            if (!mtlPath.EndsWith(".mtl") || !File.Exists(mtlPath))
                throw new FileNotFoundException($"MTL Extraction error: File at '{mtlPath}' doesn't exist or isn't an .mtl file.");

            Stopwatch stopwatch;
            if (Info.Debug)
                stopwatch = Stopwatch.StartNew();

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
                            current.SetColor(UKMaster._Color, ParseHelper.ToColor(line[3..]));
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

                            OBJPlugin.Instance.StartCoroutine(AssignTextureCorountine(current, texPath));
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

            if (Info.Debug)
            {
                stopwatch.Stop();
                LogDebug($".MTL mesh data extraction took {stopwatch.Elapsed.TotalMilliseconds}ms");
            }
        }
        catch (Exception ex)
        {
            UnityDebug.LogException(ex);
        }
    }

    private static IEnumerator AssignTextureCorountine(Material target, string texPath)
    {
        if (!File.Exists(texPath))
        {
            UnityDebug.LogException(new FileNotFoundException(
                $"Texture load error: File at '{texPath}' doesn't exist."
            ));

            target.SetTexture(UKMaster._MainTex, Assets.MissingTex);
            target.mainTextureScale = new(5, 5);
            yield break;
        }

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
            throw readFile.Exception;
        }
    }
}
