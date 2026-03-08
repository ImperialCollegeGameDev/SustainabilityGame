using UnityEngine;
using System.Diagnostics;
using System.Text;

public static class Debugger
{
    // LOG
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(params object[] args)
    {
        UnityEngine.Debug.Log(FormatMultipleArgs(args));
    }

    // WARNING
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(params object[] args)
    {
        UnityEngine.Debug.LogWarning(FormatMultipleArgs(args));
    }

    // ERROR
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(params object[] args)
    {
        UnityEngine.Debug.LogError(FormatMultipleArgs(args));
    }

    /// <summary>
    /// Converts all objects to strings and joins them with a space.
    /// </summary>
    private static string FormatMultipleArgs(object[] args)
    {
        if (args == null || args.Length == 0) return "";
        if (args.Length == 1) return args[0]?.ToString() ?? "null";

        return string.Join(" ", args);
    }
}