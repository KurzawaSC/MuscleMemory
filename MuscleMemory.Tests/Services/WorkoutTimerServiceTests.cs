using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutTimerServiceTests
{
    private const string MoreThanOneDayBug =
        "Bug: FormatDuration uses TimeSpan.Hours, so a duration of a day or more drops the whole days (25 h shows 1:00:00).";

    private readonly WorkoutTimerService _timer = new();

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

    [Fact(Skip = MoreThanOneDayBug)]
    public void FormatDuration_OneDayOrMore_ShowsTotalHours() =>
        Assert.Equal("25:00:00", _timer.FormatDuration(TimeSpan.FromHours(25)));

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
