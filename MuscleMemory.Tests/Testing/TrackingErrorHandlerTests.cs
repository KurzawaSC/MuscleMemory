using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using NSubstitute;

namespace MuscleMemory.Tests.Testing;

public sealed class TrackingErrorHandlerTests
{
    private const int TimeoutMilliseconds = 10_000;

    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly TrackingErrorHandler _errors;

    public TrackingErrorHandlerTests() => _errors = new TrackingErrorHandler(new ErrorHandler(_dialogs));

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task DrainAsync_ReportedWork_WaitsUntilItEnds()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _errors.ReportFailures(gate.Task);

        var drain = _errors.DrainAsync();

        Assert.False(drain.IsCompleted);
        gate.SetResult();
        await drain.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task DrainAsync_WorkReportedWhileDraining_WaitsForThatToo()
    {
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _errors.ReportFailures(ReportAnotherAsync(second.Task));

        var drain = _errors.DrainAsync();

        await Task.Yield();
        Assert.False(drain.IsCompleted);
        second.SetResult();
        await drain.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task ReportFailures_FailingWork_ShowsTheFixedErrorMessage()
    {
        _errors.ReportFailures(Task.FromException(new InvalidOperationException()));

        await _errors.DrainAsync().WaitAsync(TestContext.Current.CancellationToken);

        await _dialogs.Received(1).ShowMessageAsync(AppResources.TitleError, AppResources.BodyOperationFailed);
    }

    private async Task ReportAnotherAsync(Task work)
    {
        await Task.Yield();
        _errors.ReportFailures(work);
    }
}
