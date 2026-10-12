using MuscleMemory.Threading;

namespace MuscleMemory.Tests.Threading;

public sealed class SequentialTaskQueueTests
{
    private const int TimeoutMilliseconds = 10_000;
    private const int OperationCount = 20;

    private readonly SequentialTaskQueue _queue = new();

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_SeveralOperations_RunInEnqueueOrder()
    {
        var cancellation = TestContext.Current.CancellationToken;
        List<int> order = [];

        var tasks = Enumerable.Range(0, OperationCount)
            .Select(index => _queue.EnqueueAsync(async () =>
            {
                await Task.Yield();
                order.Add(index);
            }))
            .ToList();
        await Task.WhenAll(tasks).WaitAsync(cancellation);

        Assert.Equal(Enumerable.Range(0, OperationCount), order);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_SeveralOperations_NeverRunConcurrently()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var running = 0;
        var maximumRunning = 0;

        var tasks = Enumerable.Range(0, OperationCount)
            .Select(_ => _queue.EnqueueAsync(async () =>
            {
                maximumRunning = Math.Max(maximumRunning, Interlocked.Increment(ref running));
                await Task.Yield();
                Interlocked.Decrement(ref running);
            }))
            .ToList();
        await Task.WhenAll(tasks).WaitAsync(cancellation);

        Assert.Equal(1, maximumRunning);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_PreviousOperationPending_WaitsBeforeStarting()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var gate = NewGate();
        var secondStarted = false;

        var first = _queue.EnqueueAsync(() => gate.Task);
        var second = _queue.EnqueueAsync(() =>
        {
            secondStarted = true;
            return Task.CompletedTask;
        });

        Assert.False(secondStarted);
        Assert.False(second.IsCompleted);

        gate.SetResult();
        await Task.WhenAll(first, second).WaitAsync(cancellation);

        Assert.True(secondStarted);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_OperationFaults_ReturnedTaskCarriesException()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException();

        var task = _queue.EnqueueAsync(() => Task.FromException(failure));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(cancellation)));
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_OperationThrowsSynchronously_ReturnedTaskCarriesException()
    {
        var cancellation = TestContext.Current.CancellationToken;

        var task = _queue.EnqueueAsync(() => throw new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(cancellation));
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_PreviousOperationFaulted_StillRunsNextOperation()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var ran = false;

        var failed = _queue.EnqueueAsync(() => Task.FromException(new InvalidOperationException()));
        var next = _queue.EnqueueAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => failed.WaitAsync(cancellation));
        await next.WaitAsync(cancellation);
        Assert.True(ran);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_PreviousOperationCanceled_StillRunsNextOperation()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var ran = false;

        var canceled = _queue.EnqueueAsync(() => Task.FromCanceled(new CancellationToken(canceled: true)));
        var next = _queue.EnqueueAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.WaitAsync(cancellation));
        await next.WaitAsync(cancellation);
        Assert.True(ran);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task EnqueueAsync_CapturedContext_ResumesNextOperationOnThatContext()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var gate = NewGate();
        var context = new RecordingSynchronizationContext();
        SynchronizationContext? observedContext = null;
        Task first;
        Task second;

        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            first = _queue.EnqueueAsync(() => gate.Task);
            second = _queue.EnqueueAsync(() =>
            {
                observedContext = SynchronizationContext.Current;
                return Task.CompletedTask;
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        gate.SetResult();
        await Task.WhenAll(first, second).WaitAsync(cancellation);

        Assert.Same(context, observedContext);
        Assert.True(context.PostCount > 0);
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;

        public int PostCount => Volatile.Read(ref _postCount);

        public override void Post(SendOrPostCallback callback, object? state)
        {
            Interlocked.Increment(ref _postCount);
            ThreadPool.QueueUserWorkItem(_ => Run(callback, state));
        }

        private void Run(SendOrPostCallback callback, object? state)
        {
            var previous = Current;
            SetSynchronizationContext(this);
            try
            {
                callback(state);
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }
    }
}
