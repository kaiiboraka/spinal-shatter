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

    /// <summary>
    /// Retrieves the human-readable name of the first key/button bound to an action.
    /// </summary>
    /// <param name="actionName">The name of the action in the Input Map.</param>
    /// <returns>The string representation of the key, or "N/A" if none found.</returns>
    public static string GetActionKeyName(this string actionName)
    {
        var events = InputMap.ActionGetEvents(actionName);
        if (events.Count > 0)
        {
            string primaryEvent = events[0].AsText();
            return primaryEvent.Left(primaryEvent.IndexOf(' '));
        }
        return "N/A";
    }


}