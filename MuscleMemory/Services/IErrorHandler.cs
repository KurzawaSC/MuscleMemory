using System.Runtime.CompilerServices;

namespace MuscleMemory.Services;

public interface IErrorHandler
{
    Task RunAsync(Func<Task> operation, string? message = null, [CallerMemberName] string context = "");

    void ReportFailures(Task task, string? message = null, [CallerMemberName] string context = "");
}
