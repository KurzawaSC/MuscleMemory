using AndroidLog = Android.Util.Log;

namespace MuscleMemory.Diagnostics;

public static partial class AppLog
{
    private const string Tag = "MuscleMemory";

    static partial void WriteError(string message) =>
        AndroidLog.Error(Tag, message);

    static partial void WriteWarning(string message) =>
        AndroidLog.Warn(Tag, message);
}
