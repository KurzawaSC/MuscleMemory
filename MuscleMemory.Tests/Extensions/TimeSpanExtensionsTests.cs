using MuscleMemory.Extensions;

namespace MuscleMemory.Tests.Extensions;

public sealed class TimeSpanExtensionsTests
{
    [Theory]
    [InlineData(0, 0u)]
    [InlineData(250, 250u)]
    [InlineData(1500, 1500u)]
    public void ToAnimationLength_WholeMilliseconds_ReturnsMilliseconds(int milliseconds, uint expected) =>
        Assert.Equal(expected, TimeSpan.FromMilliseconds(milliseconds).ToAnimationLength());

    [Fact]
    public void ToAnimationLength_FractionalMilliseconds_Truncates() =>
        Assert.Equal(1u, TimeSpan.FromMicroseconds(1900).ToAnimationLength());

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 1)]
    [InlineData(1001, 2)]
    [InlineData(59_999, 60)]
    [InlineData(60_000, 60)]
    public void RoundUpToWholeSecond_Positive_RoundsUp(int milliseconds, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), TimeSpan.FromMilliseconds(milliseconds).RoundUpToWholeSecond());

    [Fact]
    public void RoundUpToWholeSecond_OneTickOverWholeSecond_RoundsUp() =>
        Assert.Equal(TimeSpan.FromSeconds(2), (TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1)).RoundUpToWholeSecond());

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-999, 0)]
    [InlineData(-1000, -1)]
    [InlineData(-1500, -1)]
    public void RoundUpToWholeSecond_Negative_RoundsTowardZero(int milliseconds, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), TimeSpan.FromMilliseconds(milliseconds).RoundUpToWholeSecond());
}
