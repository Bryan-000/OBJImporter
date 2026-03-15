global using static OBJImporter.Tools;

namespace OBJImporter;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary> Class full of useful tools :3 </summary>
public static class Tools
{
    /// <summary> Converts a string into a Vector3. </summary>
    public static Vector3 StringToVector3(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Vector3 pos = new(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));

        return pos;
    }

    /// <summary> Converts a string into a Vector2. </summary>
    public static Vector2 StringToVector2(string str)
    {
        string[] parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Vector2 pos = new(float.Parse(parts[0]), float.Parse(parts[1]));

        return pos;
    }

    extension(IEnumerable<string> strEnum)
    {
        /// <summary> Gets everything in a IEnumerable&lt;string&gt; after a specific index and joins it into one string. </summary>
        public string From(int index, string seperator = null) =>
            string.Join(seperator ?? " ", strEnum.Skip(index));
    }
}
