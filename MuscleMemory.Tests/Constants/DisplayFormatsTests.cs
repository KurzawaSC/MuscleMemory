using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Constants;

public sealed class DisplayFormatsTests
{
    private const string GroupMarker = "|";

    [Theory]
    [InlineData(0, "0")]
    [InlineData(640, "640")]
    [InlineData(1000, "1,000")]
    [InlineData(1234.5, "1,234.5")]
    [InlineData(1234567, "1,234,567")]
    [InlineData(82.25, "82.3")]
    [InlineData(99.96, "100")]
    public void VolumeNumber_EnglishUs_GroupsThousandsAndShowsOneDecimal(double volume, string expected) =>
        Assert.Equal(expected, FormatVolume(Cultures.EnglishUs, volume));

    [Theory]
    [InlineData(640, "640")]
    [InlineData(1000, "1|000")]
    [InlineData(1234.5, "1|234,5")]
    [InlineData(1234567, "1|234|567")]
    public void VolumeNumber_Polish_UsesPolishSeparators(double volume, string expectedWithGroupMarker) =>
        Assert.Equal(expectedWithGroupMarker.Replace(GroupMarker, Cultures.PolishGroupSeparator), FormatVolume(Cultures.Polish, volume));

    [Theory]
    [InlineData(0, 0, 0, "00:00")]
    [InlineData(0, 4, 5, "04:05")]
    [InlineData(0, 59, 59, "59:59")]
    public void Elapsed_UnderOneHour_ShowsMinutesAndSeconds(int hours, int minutes, int seconds, string expected) =>
        Assert.Equal(expected, new TimeSpan(hours, minutes, seconds).ToString(DisplayFormats.Elapsed, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(1, 0, 0, "1:00:00")]
    [InlineData(12, 3, 4, "12:03:04")]
    [InlineData(100, 5, 3, "100:05:03")]
    public void ElapsedWithHours_TotalHoursAndDuration_ShowsHoursMinutesAndSeconds(int hours, int minutes, int seconds, string expected) =>
        Assert.Equal(expected, string.Format(CultureInfo.InvariantCulture, DisplayFormats.ElapsedWithHours, hours, new TimeSpan(hours, minutes, seconds)));

    private static string FormatVolume(string culture, double volume)
    {
        using var scope = new CultureScope(culture);
        return string.Format(CultureInfo.CurrentCulture, DisplayFormats.VolumeNumber, volume);
    }
}
