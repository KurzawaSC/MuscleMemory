using System.Runtime.CompilerServices;
using AndroidLog = Android.Util.Log;

namespace MuscleMemory.Diagnostics;

public static class AppLog
{
    private const string Tag = "MuscleMemory";

    public static void Error(Exception exception, string context) =>
        AndroidLog.Error(Tag, $"{context}: {exception}");

    public static void LogFailures(Task task, [CallerMemberName] string context = "") =>
        _ = LogFailuresAsync(task, context);

    public static async Task LogFailuresAsync(Task task, [CallerMemberName] string context = "")
    {
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            Error(exception, context);
        }
    }
}
