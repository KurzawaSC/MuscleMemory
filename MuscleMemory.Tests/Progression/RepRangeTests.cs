using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class RepRangeTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(8, 12)]
    [InlineData(10, 10)]
    [InlineData(1, 30)]
    [InlineData(30, 30)]
    public void IsValid_WithinOneToThirtyAndOrdered_IsTrue(int min, int max)
    {
        Assert.True(RepRange.IsValid(min, max));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 5)]
    [InlineData(8, 31)]
    [InlineData(31, 31)]
    [InlineData(12, 8)]
    [InlineData(2, 1)]
    public void IsValid_OutsideOneToThirtyOrReversed_IsFalse(int min, int max)
    {
        Assert.False(RepRange.IsValid(min, max));
    }

    [Fact]
    public void From_ValidBounds_KeepsThem()
    {
        var range = RepRange.From(8, 12);

        Assert.NotNull(range);
        Assert.Equal((8, 12), (range.Min, range.Max));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(8, null)]
    [InlineData(null, 12)]
    [InlineData(12, 8)]
    [InlineData(0, 8)]
    [InlineData(8, 31)]
    public void From_MissingOrInvalidBounds_IsNoRange(int? min, int? max)
    {
        Assert.Null(RepRange.From(min, max));
    }
}
