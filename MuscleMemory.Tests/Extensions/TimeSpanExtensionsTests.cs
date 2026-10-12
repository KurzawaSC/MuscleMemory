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
}
