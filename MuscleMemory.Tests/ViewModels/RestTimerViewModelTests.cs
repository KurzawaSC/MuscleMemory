using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class RestTimerViewModelTests : IAsyncLifetime
{
    private const int RestSeconds = 60;
    private const int PlannedRestSeconds = TestDataBuilder.BreakSeconds;
    private const string FullMinute = "01:00";
    private const string Zero = "00:00";

    private readonly TestViewModels _ui = new();
    private Workout _push = null!;

    private RestTimerViewModel Rest => _ui.ActiveWorkout.Rest;

    private DateTime Now => _ui.Clock.GetUtcNow().UtcDateTime;

    public async ValueTask InitializeAsync()
    {
        var bench = await _ui.App.Data.ExerciseAsync("Bench Press");
        _push = await _ui.App.Data.WorkoutAsync("Push Day", bench);
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void Start_AtStart_ShowsFullDurationAndTotal()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Rest.Start(RestSeconds);

        Assert.True(Rest.IsResting);
        Assert.Equal((FullMinute, "of 60 s"), (Rest.TimerText, Rest.TotalText));
        Assert.Equal((1.0, false), (Rest.Progress, Rest.IsEnding));
        Assert.Equal(Now.AddSeconds(RestSeconds), Rest.BreakEndTimeUtc);
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
    public async Task Tick_DuringRest_RoundsRemainingUpToWholeSecond(int elapsedMilliseconds, string expected)
    {
        await StartAsync();
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromMilliseconds(elapsedMilliseconds));

        _ui.Tick();

        Assert.True(Rest.IsResting);
        Assert.Equal(expected, Rest.TimerText);
    }

    [Fact]
    public async Task Tick_Halfway_ShowsHalfTheRingWithoutPulse()
    {
        await StartAsync();
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromSeconds(RestSeconds / 2));

        _ui.Tick();

        Assert.Equal((0.5, false), (Rest.Progress, Rest.IsEnding));
    }

    [Fact]
    public async Task Tick_LastSeconds_Pulses()
    {
        await StartAsync();
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromSeconds(RestSeconds) - UiTiming.RestEndingPulse);

        _ui.Tick();

        Assert.True(Rest.IsEnding);
        Assert.True(Rest.IsResting);
    }

    [Fact]
    public async Task Tick_LastMillisecond_KeepsRestingWithoutCue()
    {
        await StartAsync();
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromSeconds(RestSeconds) - TimeSpan.FromMilliseconds(1));

        _ui.Tick();

        Assert.True(Rest.IsResting);
        _ui.Haptics.DidNotReceive().RestFinished();
        _ = _ui.AudioCues.DidNotReceive().PlayBreakEndAsync();
    }

    [Fact]
    public async Task Tick_RestEndedDuringWorkout_ClearsFiresEachCueOnceAndSaves()
    {
        await StartRestingWorkoutAsync();
        _ui.Clock.Advance(TimeSpan.FromSeconds(PlannedRestSeconds));

        _ui.Tick();
        _ui.Tick();
        await _ui.SettleAsync();

        Assert.False(Rest.IsResting);
        Assert.Equal(Zero, Rest.TimerText);
        _ui.Haptics.Received(1).RestFinished();
        _ = _ui.AudioCues.Received(1).PlayBreakEndAsync();
        Assert.False((await _ui.App.ActiveState.GetAsync())?.IsResting);
    }

    [Fact]
    public async Task Extend_DuringRest_AddsThirtySecondsAndSaves()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartRestingWorkoutAsync();
        var breakEnd = Rest.BreakEndTimeUtc;

        await Rest.ExtendCommand.ExecuteAsync(null);

        Assert.Equal(("02:00", "of 120 s"), (Rest.TimerText, Rest.TotalText));
        Assert.Equal(breakEnd.AddSeconds(DomainDefaults.RestExtensionInSeconds), Rest.BreakEndTimeUtc);
        var state = await _ui.App.ActiveState.GetAsync();
        Assert.Equal((Rest.BreakEndTimeUtc, PlannedRestSeconds + DomainDefaults.RestExtensionInSeconds), (state?.BreakEndTimeUtc, state?.RestDurationSeconds));
    }

    [Fact]
    public async Task Extend_MidSecond_RoundsExtendedRemainingUp()
    {
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromMilliseconds(10_500));

        await Rest.ExtendCommand.ExecuteAsync(null);

        Assert.Equal("01:20", Rest.TimerText);
    }

    [Fact]
    public async Task Extend_NotResting_ChangesNothing()
    {
        await Rest.ExtendCommand.ExecuteAsync(null);

        Assert.False(Rest.IsResting);
        Assert.Equal((0, Zero), (Rest.DurationSeconds, Rest.TimerText));
    }

    [Fact]
    public async Task Skip_DuringRest_ClearsStopsTheCueAndSaves()
    {
        await StartRestingWorkoutAsync();

        await Rest.SkipCommand.ExecuteAsync(null);

        Assert.False(Rest.IsResting);
        _ui.AudioCues.Received(1).Stop();
        _ui.Haptics.DidNotReceive().RestFinished();
        var state = await _ui.App.ActiveState.GetAsync();
        Assert.Equal((false, default(DateTime), 0), (state?.IsResting, state?.BreakEndTimeUtc, state?.RestDurationSeconds));
    }

    [Fact]
    public async Task Clear_DuringRest_ResetsEveryValue()
    {
        await StartAsync();
        Rest.Start(RestSeconds);
        _ui.Clock.Advance(TimeSpan.FromSeconds(57));
        _ui.Tick();

        Rest.Clear();

        Assert.Equal(
            (false, default(DateTime), 0, 0.0, false, Zero, string.Empty),
            (Rest.IsResting, Rest.BreakEndTimeUtc, Rest.DurationSeconds, Rest.Progress, Rest.IsEnding, Rest.TimerText, Rest.TotalText));
    }

    [Fact]
    public async Task NextSession_AfterFinishingDuringARest_StartsWithoutRest()
    {
        await StartRestingWorkoutAsync();
        _ui.AnswerConfirmations(true);
        await _ui.ActiveWorkout.FinishWorkoutCommand.ExecuteAsync(null);
        await _ui.ActiveWorkout.ExitWorkoutCommand.ExecuteAsync(null);

        await StartAsync();

        Assert.False(Rest.IsResting);
        Assert.Equal(default, Rest.BreakEndTimeUtc);
        Assert.False((await _ui.App.ActiveState.GetAsync())?.IsResting);
    }

    [Fact]
    public async Task NextSession_AfterResetDuringARest_StartsWithoutRest()
    {
        await StartRestingWorkoutAsync();
        await _ui.App.Maintenance.ClearAllDataAsync();
        _ui.ActiveWorkout.Reset();
        var bench = await _ui.App.Data.ExerciseAsync("Bench Press");
        _push = await _ui.App.Data.WorkoutAsync("Push Day", bench);

        await StartAsync();

        Assert.False(Rest.IsResting);
        Assert.Equal(default, Rest.BreakEndTimeUtc);
        Assert.False((await _ui.App.ActiveState.GetAsync())?.IsResting);
    }

    [Fact]
    public void Restore_MidSecond_RoundsRemainingUp()
    {
        Rest.Restore(new ActiveWorkoutState
        {
            IsResting = true,
            BreakEndTimeUtc = Now.AddMilliseconds(45_250),
            RestDurationSeconds = RestSeconds
        });

        Assert.True(Rest.IsResting);
        Assert.Equal("00:46", Rest.TimerText);
    }

    [Fact]
    public void Restore_RestAlreadyEnded_ClearsToZero()
    {
        Rest.Restore(new ActiveWorkoutState
        {
            IsResting = true,
            BreakEndTimeUtc = Now,
            RestDurationSeconds = RestSeconds
        });

        Assert.False(Rest.IsResting);
        Assert.Equal(Zero, Rest.TimerText);
    }

    [Fact]
    public void ExtensionText_EnglishUs_ShowsTheExtension()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("+30 s", Rest.ExtensionText);
    }

    private async Task StartAsync()
    {
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = _push });
        await _ui.SettleAsync();
    }

    private async Task StartRestingWorkoutAsync()
    {
        await StartAsync();
        _ui.ActiveWorkout.SetInput.Fill(80, 5);
        await _ui.ActiveWorkout.SaveSetCommand.ExecuteAsync(null);
        Assert.True(Rest.IsResting);
    }
}
