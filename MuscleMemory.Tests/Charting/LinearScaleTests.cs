using MuscleMemory.Charting;

namespace MuscleMemory.Tests.Charting;

public sealed class LinearScaleTests
{
    [Theory]
    [InlineData(80, 200)]
    [InlineData(100, 100)]
    [InlineData(120, 0)]
    [InlineData(90, 150)]
    public void Map_ValueInDomain_IsInterpolatedOntoAnInvertedRange(double value, float expected)
    {
        Assert.Equal(expected, new LinearScale(80, 120, 200, 0).Map(value));
    }

    [Fact]
    public void Map_EmptyDomain_IsTheMiddleOfTheRange()
    {
        Assert.Equal(150, new LinearScale(5, 5, 100, 200).Map(5));
    }
}
