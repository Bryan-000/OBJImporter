global using static OBJImporter.Tools;
namespace OBJImporter;

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

internal static class Tools
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float, float, float) StringToFloat3(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 IStringToVector3(string str)
    {
        (float x, float y, float z) = StringToFloat3(str);
        return new(-x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 StringToVector2(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new(float.Parse(parts[0]), float.Parse(parts[1]));
    }

    extension(string str)
    {
        public string Replace(char[] oldChars, char newChar)
        {
            string newStr = str;
            foreach (char oldChar in oldChars)
                newStr = newStr.Replace(oldChar, newChar);

            return newStr;
        }
    }

    extension(Material mat)
    {
        public void ChangeBlendModeToTransparent()
        {
            mat.SetFloat(UKMaster._BlendMode, 2);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt(UKMaster._SrcBlend, (int)BlendMode.SrcAlpha);
            mat.SetInt(UKMaster._DstBlend, (int)BlendMode.OneMinusSrcAlpha);

            mat.DisableKeyword(UKMaster.ALPHA_TEST);
            mat.EnableKeyword(UKMaster.TRANSPARENCY);
            mat.EnableKeyword(UKMaster.VERTEX_LIGHTING);
            mat.EnableKeyword(UKMaster._FOG_ON);

            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}