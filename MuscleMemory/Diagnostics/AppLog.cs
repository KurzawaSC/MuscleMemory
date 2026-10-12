using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MuscleMemory.Diagnostics;

public static partial class AppLog
{
    public static void Error(Exception exception, string context) =>
        WriteError($"{context}: {exception}");

    [Conditional("DEBUG")]
    public static void DebugWarning(string message) =>
        WriteWarning(message);

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

    static partial void WriteError(string message);

    static partial void WriteWarning(string message);
}
