using Microsoft.Extensions.Time.Testing;

namespace MuscleMemory.Tests.Testing;

public sealed class TestClock(DateTimeOffset startUtc) : FakeTimeProvider(startUtc)
{
    private readonly Lock _gate = new();
    private TaskCompletionSource _pauseStarted = NewSignal();
    private TaskCompletionSource? _heldPauses;

    public Task PauseStarted
    {
        get
        {
            lock (_gate)
            {
                return _pauseStarted.Task;
            }
        }
    }

    public void HoldPauses()
    {
        lock (_gate)
        {
            _heldPauses ??= NewSignal();
            _pauseStarted = NewSignal();
        }
    }

    public void ReleasePauses()
    {
        TaskCompletionSource? held;
        lock (_gate)
        {
            held = _heldPauses;
            _heldPauses = null;
        }

        held?.TrySetResult();
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        if (period != Timeout.InfiniteTimeSpan)
        {
            return base.CreateTimer(callback, state, dueTime, period);
        }

        Task release;
        lock (_gate)
        {
            release = _heldPauses?.Task ?? Task.CompletedTask;
            _pauseStarted.TrySetResult();
        }

        release.ContinueWith(_ => callback(state), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        return new ElapsedPause();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ElapsedPause : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
