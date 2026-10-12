using Microsoft.Extensions.Time.Testing;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class RestTimerViewModelTests
{
    private const int RestSeconds = 60;
    private const string FullMinute = "01:00";
    private const string Zero = "00:00";

    private static readonly DateTime StartUtc = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _clock = new(StartUtc);
    private readonly IRestTimerHost _host = Substitute.For<IRestTimerHost>();
    private readonly IHapticService _haptics = Substitute.For<IHapticService>();
    private readonly IAudioCueService _audioCues = Substitute.For<IAudioCueService>();
    private readonly IErrorHandler _errors = Substitute.For<IErrorHandler>();
    private readonly RestTimerViewModel _rest;

    public RestTimerViewModelTests()
    {
        _errors.RunAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(call => call.Arg<Func<Task>>()());
        _rest = new RestTimerViewModel(_host, new WorkoutTimerService(_clock), _clock, _haptics, _audioCues, _errors);
    }

    [Fact]
    public void Start_AtStart_ShowsFullDuration()
    {
        _rest.Start(RestSeconds);

        Assert.True(_rest.IsResting);
        Assert.Equal(FullMinute, _rest.TimerText);
    }

    [Theory]
    [InlineData(1, "01:00")]
    [InlineData(500, "01:00")]
    [InlineData(999, "01:00")]
    [InlineData(1000, "00:59")]
    [InlineData(1001, "00:59")]
    [InlineData(1999, "00:59")]
    [InlineData(2000, "00:58")]
    [InlineData(30_500, "00:30")]
    [InlineData(59_000, "00:01")]
    [InlineData(59_999, "00:01")]
    public void Tick_DuringRest_RoundsRemainingUpToWholeSecond(int elapsedMilliseconds, string expected)
    {
        _rest.Start(RestSeconds);
        _clock.Advance(TimeSpan.FromMilliseconds(elapsedMilliseconds));

        _rest.Tick();

        Assert.True(_rest.IsResting);
        Assert.Equal(expected, _rest.TimerText);
    }

    [Fact]
    public void Tick_LastMillisecond_KeepsRestingWithoutCue()
    {
        _rest.Start(RestSeconds);
        _clock.Advance(TimeSpan.FromSeconds(RestSeconds) - TimeSpan.FromMilliseconds(1));

        _rest.Tick();

        Assert.True(_rest.IsResting);
        _haptics.DidNotReceive().RestFinished();
        _ = _audioCues.DidNotReceive().PlayBreakEndAsync();
    }

    [Fact]
    public void Tick_RestEnded_ShowsZeroAndFiresCues()
    {
        _rest.Start(RestSeconds);
        _clock.Advance(TimeSpan.FromSeconds(RestSeconds));

        _rest.Tick();

        Assert.False(_rest.IsResting);
        Assert.Equal(Zero, _rest.TimerText);
        _haptics.Received(1).RestFinished();
        _ = _audioCues.Received(1).PlayBreakEndAsync();
        _ = _host.Received(1).SaveStateAsync();
    }

    [Fact]
    public async Task Extend_MidSecond_RoundsExtendedRemainingUp()
    {
        _rest.Start(RestSeconds);
        _clock.Advance(TimeSpan.FromMilliseconds(10_500));

        await _rest.ExtendCommand.ExecuteAsync(null);

        Assert.Equal("01:20", _rest.TimerText);
    }

    [Fact]
    public void Restore_MidSecond_RoundsRemainingUp()
    {
        var state = new ActiveWorkoutState
        {
            IsResting = true,
            BreakEndTimeUtc = StartUtc.AddMilliseconds(45_250),
            RestDurationSeconds = RestSeconds
        };

        _rest.Restore(state);

        Assert.True(_rest.IsResting);
        Assert.Equal("00:46", _rest.TimerText);
    }

    [Fact]
    public void Restore_RestAlreadyEnded_ClearsToZero()
    {
        var state = new ActiveWorkoutState
        {
            IsResting = true,
            BreakEndTimeUtc = StartUtc,
            RestDurationSeconds = RestSeconds
        };

        _rest.Restore(state);

        Assert.False(_rest.IsResting);
        Assert.Equal(Zero, _rest.TimerText);
    }
}
