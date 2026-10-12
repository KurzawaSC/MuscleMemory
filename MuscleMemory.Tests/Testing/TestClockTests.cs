namespace MuscleMemory.Tests.Testing;

public sealed class TestClockTests
{
    private const int TimeoutMilliseconds = 10_000;
    private const int PeriodicTicks = 3;

    private static readonly DateTime StartUtc = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Pause = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(1);

    private readonly TestClock _clock = new(StartUtc);

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task Delay_PausesNotHeld_CompletesWithoutMovingTheClock()
    {
        await Task.Delay(Pause, _clock, TestContext.Current.CancellationToken);

        Assert.Equal(StartUtc, _clock.GetUtcNow().UtcDateTime);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task Delay_PausesHeld_CompletesOnlyOnceReleased()
    {
        var cancellation = TestContext.Current.CancellationToken;
        _clock.HoldPauses();

        var pause = Task.Delay(Pause, _clock, cancellation);
        await _clock.PauseStarted.WaitAsync(cancellation);

        Assert.False(pause.IsCompleted);
        _clock.ReleasePauses();
        await pause.WaitAsync(cancellation);
        Assert.Equal(StartUtc, _clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void CreateTimer_Periodic_FiresOnlyAsTheClockAdvances()
    {
        var ticks = 0;
        using var timer = _clock.CreateTimer(_ => ticks++, null, Period, Period);

        Assert.Equal(0, ticks);
        _clock.Advance(Period * PeriodicTicks);
        Assert.Equal(PeriodicTicks, ticks);
    }
}
