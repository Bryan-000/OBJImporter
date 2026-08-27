namespace OBJImporter;

using System;
using UnityEngine;
using UnityEngine.Rendering;

#pragma warning disable RCS1203, CS9113

// it stands for ultrakill master shader
public static class UKMaster
{
    static Shader MasterShader = DefaultReferenceManager.Instance?.masterShader ?? throw new NullReferenceException("ffs the fucking defaultreferencemanager is null");

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

    [Type(ShaderPropertyType.Float)]
    public static readonly int _ZWrite = Shader.PropertyToID("_ZWrite");

    public static readonly LocalKeyword
        ALPHA_TEST = new(MasterShader, "ALPHA_TEST"),
        TRANSPARENCY = new(MasterShader, "TRANSPARENCY"),
        VERTEX_LIGHTING = new(MasterShader, "VERTEX_LIGHTING"),
        _FOG_ON = new(MasterShader, "_FOG_ON");


    class EnumAttribute(string _) : Attribute;
    class TypeAttribute(ShaderPropertyType _) : Attribute;
}

#pragma warning restore RCS1203, CS9113
