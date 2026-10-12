using System.Runtime.CompilerServices;
using MuscleMemory.Services;

namespace MuscleMemory.Tests.Testing;

public sealed class TrackingErrorHandler(IErrorHandler handler) : IErrorHandler
{
    private readonly Lock _gate = new();
    private readonly List<Task> _reported = [];

    public Task RunAsync(Func<Task> operation, string? message = null, [CallerMemberName] string context = "") =>
        handler.RunAsync(operation, message, context);

    public void ReportFailures(Task task, string? message = null, [CallerMemberName] string context = "")
    {
        var handled = handler.RunAsync(() => task, message, context);
        lock (_gate)
        {
            _reported.Add(handled);
        }
    }

    public async Task DrainAsync()
    {
        while (TakeReported() is { Count: > 0 } reported)
        {
            await Task.WhenAll(reported);
        }
    }

    private List<Task> TakeReported()
    {
        lock (_gate)
        {
            List<Task> taken = [.. _reported];
            _reported.Clear();
            return taken;
        }
    }
}
