namespace ObjImporter.Tools;

using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

#pragma warning disable RCS1203, CS9113

// it stands for ultrakill master shader
public static class UKMaster
{
    public static readonly Shader shader = DefaultReferenceManager.Instance?.masterShader
        ?? Addressables.LoadAssetAsync<Shader>("Assets/Shaders/MasterShader/ULTRAKILL-Standard.shader").WaitForCompletion();

    [Type(ShaderPropertyType.Color)]
    public static readonly int _Color = Shader.PropertyToID("_Color");

    [Type(ShaderPropertyType.Texture)]
    public static readonly int _MainTex = Shader.PropertyToID("_MainTex");

    [Type(ShaderPropertyType.Float)]
    [Enum("Opaque: 0, Cutout: 1, Transparent: 2, Advanced: 3")]
    public static readonly int _BlendMode = Shader.PropertyToID("_BlendMode");

    [Type(ShaderPropertyType.Float)]
    public static readonly int _SrcBlend = Shader.PropertyToID("_SrcBlend");

    [Type(ShaderPropertyType.Float)]
    public static readonly int _DstBlend = Shader.PropertyToID("_DstBlend");

    [Type(ShaderPropertyType.Float)]
    public static readonly int _Opacity = Shader.PropertyToID("_Opacity");

    public static readonly LocalKeyword
        ALPHA_TEST = new(shader, "ALPHA_TEST"),
        TRANSPARENCY = new(shader, "TRANSPARENCY"),
        VERTEX_LIGHTING = new(shader, "VERTEX_LIGHTING"),
        _FOG_ON = new(shader, "_FOG_ON");


    class EnumAttribute(string _) : Attribute;
    class TypeAttribute(ShaderPropertyType _) : Attribute;
}

#pragma warning restore RCS1203, CS9113