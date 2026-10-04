using System.Runtime.CompilerServices;
#if ANDROID
using AndroidLog = Android.Util.Log;
#endif

namespace MuscleMemory.Diagnostics;

public static class AppLog
{
    private const string Tag = "MuscleMemory";

    public static void Error(Exception exception, string context)
    {
#if ANDROID
        AndroidLog.Error(Tag, $"{context}: {exception}");
#endif
    }

    public static void LogFailures(Task task, [CallerMemberName] string context = "") =>
        _ = LogFailuresAsync(task, context);

    private static async Task LogFailuresAsync(Task task, string context)
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
