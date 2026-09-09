global using static OBJImporter.Tools.Extensions;
namespace OBJImporter.Tools;

using UnityEngine;
using UnityEngine.Rendering;

internal static class Extensions
{
    extension(string str)
    {
        // why doesnt this exist
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
        public Material AddMasterShader()
        {
            mat.shader = UKMaster.shader;

            return mat;
        }

        public void ChangeBlendModeToTransparent()
        {
            mat.SetFloat(UKMaster._BlendMode, 2);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt(UKMaster._SrcBlend, (int)BlendMode.SrcAlpha);
            mat.SetInt(UKMaster._DstBlend, (int)BlendMode.OneMinusSrcAlpha);

            mat.DisableKeyword(UKMaster.ALPHA_TEST);
            mat.EnableKeyword(UKMaster.TRANSPARENCY);
            mat.EnableKeyword(UKMaster._FOG_ON);

            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}