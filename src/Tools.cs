global using static OBJImporter.Tools;
namespace OBJImporter;

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Tools
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 StringToVector3(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new(-float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
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
}