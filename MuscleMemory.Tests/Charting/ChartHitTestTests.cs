using MuscleMemory.Charting;

namespace MuscleMemory.Tests.Charting;

public sealed class ChartHitTestTests
{
    private const float Tolerance = 24;
    private static readonly float[] PointXs = [10, 100, 200, 260];

    [Theory]
    [InlineData(12, 0)]
    [InlineData(90, 1)]
    [InlineData(124, 1)]
    [InlineData(222, 2)]
    [InlineData(275, 3)]
    public void Nearest_TouchNearAPoint_SelectsTheClosestByX(float touchX, int expected)
    {
        Assert.Equal(expected, ChartHitTest.Nearest(PointXs, touchX, Tolerance));
    }

    [Theory]
    [InlineData(55)]
    [InlineData(150)]
    [InlineData(300)]
    public void Nearest_TouchFartherThanTheTolerance_SelectsNothing(float touchX)
    {
        Assert.Null(ChartHitTest.Nearest(PointXs, touchX, Tolerance));
    }

    [Fact]
    public void Nearest_ExactlyAtTheTolerance_SelectsThePoint()
    {
        Assert.Equal(1, ChartHitTest.Nearest(PointXs, 100 + Tolerance, Tolerance));
    }

    [Fact]
    public void Nearest_HalfwayBetweenTwoPoints_SelectsTheLaterOne()
    {
        Assert.Equal(3, ChartHitTest.Nearest([200f, 220f, 240f, 230f], 235, Tolerance));
    }

    [Fact]
    public void Nearest_PointsOnTheSameX_SelectsTheLastDrawn()
    {
        Assert.Equal(2, ChartHitTest.Nearest([50f, 50f, 50f], 50, Tolerance));
    }

    [Fact]
    public void Nearest_NoPoints_SelectsNothing()
    {
        Assert.Null(ChartHitTest.Nearest([], 50, Tolerance));
    }
}
