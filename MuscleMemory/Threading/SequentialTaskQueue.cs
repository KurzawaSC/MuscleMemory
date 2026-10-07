namespace MuscleMemory.Threading;

public sealed class SequentialTaskQueue
{
    private readonly Lock _gate = new();
    private Task _tail = Task.CompletedTask;

    public Task EnqueueAsync(Func<Task> operation)
    {
        lock (_gate)
        {
            _tail = RunAfterAsync(_tail, operation);
            return _tail;
        }
    }

    private static async Task RunAfterAsync(Task previous, Func<Task> operation)
    {
        await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        await operation();
    }
}
