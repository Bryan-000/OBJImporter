namespace OBJImporter.API;

using OBJImporter.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

/// <summary> API wrapper for <see cref="Parser"/> to make importing .obj files and converting them into meshes/GameObjects much easier. </summary>
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
        try
        {
            // clean the path for this specific OS
            path = path.Replace(['\\', '/'], Path.DirectorySeparatorChar);

            if (!path.EndsWith(".obj") || !File.Exists(path))
                throw new FileNotFoundException($"File at '{path}' doesn't exist or isn't an obj file.");

            Parser.CreateMesh(path, out Mesh result, out List<Material> materials, lighting);

            return (result, [.. materials]);
        }
        catch (Exception ex)
        {
            UnityDebug.LogException(ex);

            // fallback model
            return (Assets.ErrorModel, [Assets.ErrorModel_Mat]);
        }
    }
}