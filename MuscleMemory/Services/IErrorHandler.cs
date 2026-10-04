using System.Runtime.CompilerServices;
using MuscleMemory.Constants;

namespace MuscleMemory.Services;

public interface IErrorHandler
{
    Task RunAsync(Func<Task> operation, string message = UiText.BodyOperationFailed, [CallerMemberName] string context = "");

    void ReportFailures(Task task, string message = UiText.BodyOperationFailed, [CallerMemberName] string context = "");
}
