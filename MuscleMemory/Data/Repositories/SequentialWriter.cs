namespace MuscleMemory.Data.Repositories;

internal sealed class SequentialWriter
{
    private readonly Lock _gate = new();
    private Task _tail = Task.CompletedTask;

    public Task EnqueueAsync(Func<Task> write)
    {
        lock (_gate)
        {
            _tail = RunAfterAsync(_tail, write);
            return _tail;
        }
    }

    private static async Task RunAfterAsync(Task previous, Func<Task> write)
    {
        await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await write();
    }
}
