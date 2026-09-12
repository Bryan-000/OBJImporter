namespace ObjImporter.Tools;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ParseHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(ReadOnlySpan<char> str)
    {
        int cursor = 0;
        return new(ReadFloat(str, ref cursor), ReadFloat(str, ref cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color ToColor(ReadOnlySpan<char> str)
    {
        int cursor = 0;
        return new(ReadFloat(str, ref cursor), ReadFloat(str, ref cursor), ReadFloat(str, ref cursor));
    }

    // the I stands for inverted, since it inverts the x axis
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 I_ToVector3(ReadOnlySpan<char> str) => I_ToVector3(str, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 I_ToVector3(ReadOnlySpan<char> str, out int cursor)
    {
        cursor = 0;
        return new(-ReadFloat(str, ref cursor), ReadFloat(str, ref cursor), ReadFloat(str, ref cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ParseInt(ReadOnlySpan<char> str) =>
        int.Parse(str, NumberStyles.Integer, CultureInfo.InvariantCulture);

    public static bool Contains(ReadOnlySpan<char> str, char search)
    {
        for (int cursor = 0; cursor < str.Length; cursor++)
        {
            if (str[cursor] == search)
                return true;
        }

        return false;
    }

    public static bool SeekAndSlice(ReadOnlySpan<char> str, char search, ref int cursor, out ReadOnlySpan<char> slice)
    {
        int length = str.Length;
        if (cursor >= length)
        {
            slice = [];
            return false;
        }

        // skip past any leading search char to find the start of the next token
        while (cursor < length && str[cursor] == search)
            cursor++;

        int start = cursor;
        while (cursor < length && str[cursor] != search)
            cursor++;

        // nothing
        if (start == cursor)
        {
            slice = [];
            return false;
        }

        slice = str[start..cursor];
        return true;
    }

    public static bool TryReadFloat(ReadOnlySpan<char> str, ref int cursor, out float value)
    {
        int length = str.Length;

        // loop until the cursor isnt pointing at whitespace, to find the start of the float chars
        while (cursor < length && str[cursor] == ' ')
            cursor++;

        // store the starting pos of the float chars
        int start = cursor;

        // loop until the cursor IS pointing at whitespace, meaning we've found the end of the float chars
        while (cursor < length && str[cursor] != ' ')
            cursor++;

        if (start == cursor)
        {
            value = 0f;
            return false;
        }

        return float.TryParse(str[start..cursor], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static float ReadFloat(ReadOnlySpan<char> str, ref int cursor)
    {
        int length = str.Length;

        // loop until the cursor isnt pointing at whitespace, to find the start of the float chars
        while (cursor < length && str[cursor] == ' ')
            cursor++;

        // store the starting pos of the float chars
        int start = cursor;

        // loop until the cursor IS pointing at whitespace, meaning we've found the end of the float chars
        while (cursor < length && str[cursor] != ' ')
            cursor++;

        try
        {
            return float.Parse(str[start..cursor], NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new FormatException("Parse failure! String is malformed and couldn't be parsed.\n"
                + $"str: '{str.ToString()}', start: '{start}', cursor: '{cursor}', str[start..cursor]: '{str[start..cursor].ToString()}'",
                ex
            );
        }
    }
}