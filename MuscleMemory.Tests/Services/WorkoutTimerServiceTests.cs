using Microsoft.Extensions.Time.Testing;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using NSubstitute;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutTimerServiceTests
{
    private static readonly DateTime StartUtc = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _clock = new(StartUtc);
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly IDispatcherTimer _dispatcherTimer = Substitute.For<IDispatcherTimer>();
    private readonly WorkoutTimerService _timer;

    public WorkoutTimerServiceTests()
    {
        _dispatcher.CreateTimer().Returns(_dispatcherTimer);
        _timer = new WorkoutTimerService(_clock, _dispatcher);
    }

    [Fact]
    public void Start_FirstTime_StartsOneSecondDispatcherTimer()
    {
        _timer.Start();

        _dispatcherTimer.Received(1).Start();
        Assert.Equal(TimeSpan.FromSeconds(1), _dispatcherTimer.Interval);
    }

    [Fact]
    public void Start_Again_ReusesTheSameTimer()
    {
        _timer.Start();
        _timer.Stop();

        _timer.Start();

        _dispatcher.Received(1).CreateTimer();
        _dispatcherTimer.Received(2).Start();
        _dispatcherTimer.Received(1).Stop();
    }

    [Fact]
    public void Stop_NeverStarted_CreatesNoTimer()
    {
        _timer.Stop();

        _dispatcher.DidNotReceive().CreateTimer();
    }

    [Fact]
    public void DispatcherTimerTick_Started_RaisesTicked()
    {
        var ticks = 0;
        _timer.Ticked += (_, _) => ticks++;
        _timer.Start();

        _dispatcherTimer.Tick += Raise.Event();
        _dispatcherTimer.Tick += Raise.Event();

        Assert.Equal(2, ticks);
    }

    [Fact]
    public void ElapsedSince_ClockAdvanced_FormatsTimeSinceStart()
    {
        _clock.Advance(new TimeSpan(1, 2, 3));

        Assert.Equal("1:02:03", _timer.ElapsedSince(StartUtc));
    }

    [Fact]
    public void RemainingUntil_ClockAdvanced_ReturnsTimeLeft()
    {
        _clock.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(TimeSpan.FromSeconds(40), _timer.RemainingUntil(StartUtc.AddMinutes(1)));
    }

    [Fact]
    public void RemainingUntil_EndPassed_ReturnsNegative()
    {
        _clock.Advance(TimeSpan.FromSeconds(61));

        Assert.Equal(TimeSpan.FromSeconds(-1), _timer.RemainingUntil(StartUtc.AddMinutes(1)));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(1, "00:01")]
    [InlineData(59, "00:59")]
    [InlineData(60, "01:00")]
    [InlineData(90, "01:30")]
    [InlineData(3599, "59:59")]
    public void FormatDuration_UnderOneHour_ShowsMinutesAndSeconds(int seconds, string expected) =>
        Assert.Equal(expected, _timer.FormatDuration(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(1, 0, 0, "1:00:00")]
    [InlineData(1, 2, 3, "1:02:03")]
    [InlineData(10, 0, 0, "10:00:00")]
    [InlineData(23, 59, 59, "23:59:59")]
    public void FormatDuration_OneHourOrMore_ShowsHours(int hours, int minutes, int seconds, string expected) =>
        Assert.Equal(expected, _timer.FormatDuration(new TimeSpan(hours, minutes, seconds)));

    [Theory]
    [InlineData(24, 0, 0, "24:00:00")]
    [InlineData(25, 0, 0, "25:00:00")]
    [InlineData(99, 59, 59, "99:59:59")]
    [InlineData(100, 5, 3, "100:05:03")]
    [InlineData(250, 0, 1, "250:00:01")]
    public void FormatDuration_OneDayOrMore_ShowsTotalHours(int hours, int minutes, int seconds, string expected) =>
        Assert.Equal(expected, _timer.FormatDuration(new TimeSpan(hours, minutes, seconds)));

    [Fact]
    public void FormatDuration_JustUnderOneDay_KeepsHoursUnderTwentyFour() =>
        Assert.Equal("23:59:59", _timer.FormatDuration(TimeSpan.FromDays(1) - TimeSpan.FromMilliseconds(1)));

    [Fact]
    public void FormatDuration_PartialSecond_TruncatesToWholeSecond() =>
        Assert.Equal("00:59", _timer.FormatDuration(TimeSpan.FromMilliseconds(59_900)));

    [Theory]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void FormatDuration_Negative_ShowsZero(int seconds) =>
        Assert.Equal("00:00", _timer.FormatDuration(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void FormatDuration_IndependentOfCulture_UsesColons()
    {
        using var scope = new CultureScope(Cultures.German);

        Assert.Equal("1:02:03", _timer.FormatDuration(new TimeSpan(1, 2, 3)));
    }
}
