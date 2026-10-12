using Microsoft.Maui.Graphics;
using MuscleMemory.Extensions;

namespace MuscleMemory.Tests.Extensions;

public sealed class ColorExtensionsTests
{
    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#F6F6F7")]
    [InlineData("#FFEFEF")]
    [InlineData("#BCBCBC")]
    public void PrefersDarkForeground_LightBackground_ReturnsTrue(string background) =>
        Assert.True(Color.FromArgb(background).PrefersDarkForeground());

    [Theory]
    [InlineData("#000000")]
    [InlineData("#101013")]
    [InlineData("#FF4040")]
    [InlineData("#BBBBBB")]
    public void PrefersDarkForeground_DarkBackground_ReturnsFalse(string background) =>
        Assert.False(Color.FromArgb(background).PrefersDarkForeground());

    [Theory]
    [InlineData("#00FF00", true)]
    [InlineData("#FF0000", false)]
    [InlineData("#0000FF", false)]
    public void PrefersDarkForeground_PrimaryChannel_WeighsChannelsByLuminance(string background, bool expected) =>
        Assert.Equal(expected, Color.FromArgb(background).PrefersDarkForeground());
}
