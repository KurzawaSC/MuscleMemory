using System.Runtime.CompilerServices;
using MuscleMemory.Diagnostics;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Services;

public sealed class ErrorHandler(IDialogService dialogs) : IErrorHandler
{
    private bool _isShowingError;

    public async Task RunAsync(Func<Task> operation, string? message = null, [CallerMemberName] string context = "")
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            AppLog.Error(exception, context);
            await TellUserAsync(message ?? AppResources.BodyOperationFailed, context);
        }
    }

    public void ReportFailures(Task task, string? message = null, [CallerMemberName] string context = "") =>
        _ = RunAsync(() => task, message, context);

    private async Task TellUserAsync(string message, string context)
    {
        if (_isShowingError)
        {
            return;
        }

        _isShowingError = true;
        try
        {
            await dialogs.ShowMessageAsync(AppResources.TitleError, message);
        }
        catch (Exception exception)
        {
            AppLog.Error(exception, context);
        }
        finally
        {
            _isShowingError = false;
        }
    }
}
