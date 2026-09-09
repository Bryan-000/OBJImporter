namespace OBJImporter;

using OBJImporter.Tools;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class Assets
{
    private static AssetBundle bundle
    {
        get
        {
            if (!field)
            {
                Assembly asm = typeof(Assets).Assembly;
                using Stream bundleStream = asm.GetManifestResourceStream("objimporter-assets.bundle");

                field = AssetBundle.LoadFromStream(bundleStream);

                Debug.Log("assets: " + string.Join(", ", field.GetAllAssetNames()));
            }

            return field;
        }
    }

    public static Mesh      ErrorModel => field ??= bundle.LoadAsset<Mesh>("Assets/error.fbx");
    public static Texture2D MissingTex => field ??= bundle.LoadAsset<Texture2D>("Assets/missing texture.png");

    public static Material ErrorModel_Mat => field ??= bundle.LoadAsset<Material>("Assets/error.mat").AddMasterShader();
    public static Material MissingTex_Mat => field ??= bundle.LoadAsset<Material>("Assets/missing material.mat").AddMasterShader();
}