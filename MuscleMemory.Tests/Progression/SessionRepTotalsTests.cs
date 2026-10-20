using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class SessionRepTotalsTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(0.25, false)]
    public void IsBodyweight_HeaviestWeight_IsTrueOnlyWithoutLoad(double heaviest, bool expected)
    {
        Assert.Equal(expected, new SessionRepTotals { HeaviestWeight = heaviest, Reps = 5 }.IsBodyweight);
    }

    [Theory]
    [InlineData(90, 3, 99)]
    [InlineData(99, 1, 99)]
    public void EstimatedOneRepMax_HeaviestWeightAndReps_IsTheDomainEstimate(double heaviest, int reps, double expected)
    {
        Assert.Equal(expected, new SessionRepTotals { HeaviestWeight = heaviest, Reps = reps }.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_MoreThanTwelveReps_IsUndefined()
    {
        Assert.Null(new SessionRepTotals { HeaviestWeight = 50, Reps = 13 }.EstimatedOneRepMax);
    }
}
