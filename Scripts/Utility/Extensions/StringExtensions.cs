using Elythia;
using System;
using Godot;

public static class StringExtensions
{
    public static string ToCapitalized(this string str)
    {
        if (str.Length == 0) return string.Empty;
        if (str.Length == 1) return str.ToUpper();
        return str.Substring(0, 1).ToLower() + str.Substring(1);
    }

    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool ContainsString(this string source, string toCheck, StringComparison comp = StringComparison.OrdinalIgnoreCase)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }

    public static string FileName(this string source)
    {
        return source?.Substring(source.LastIndexOf("/") + 1);
    }

}