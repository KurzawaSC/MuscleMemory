using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class OneRepMaxTests
{
    [Theory]
    [InlineData(60, 60)]
    [InlineData(62.5, 62.5)]
    [InlineData(100, 100)]
    public void Estimate_SingleRep_IsTheWeightItself(double weight, double expected)
    {
        Assert.Equal(expected, OneRepMax.Estimate(weight, 1));
    }

    [Theory]
    [InlineData(60, 2, 64)]
    [InlineData(60, 5, 70)]
    [InlineData(60, 10, 80)]
    [InlineData(60, 12, 84)]
    [InlineData(100, 2, 106.66666666666667)]
    [InlineData(100, 5, 116.66666666666667)]
    [InlineData(100, 10, 133.33333333333334)]
    [InlineData(100, 12, 140)]
    public void Estimate_TwoToTwelveReps_FollowsEpley(double weight, int reps, double expected)
    {
        Assert.Equal(expected, OneRepMax.Estimate(weight, reps));
    }

    [Theory]
    [InlineData(2, 66.66666666666667)]
    [InlineData(5, 72.91666666666667)]
    [InlineData(10, 83.33333333333333)]
    [InlineData(12, 87.5)]
    public void Estimate_FractionalWeight_KeepsFullPrecision(int reps, double expected)
    {
        Assert.Equal(expected, OneRepMax.Estimate(62.5, reps));
    }

    [Fact]
    public void Estimate_RepLimit_IsTwelve()
    {
        Assert.Equal(12, OneRepMax.MaxReps);
        Assert.Equal(30, OneRepMax.EpleyDivisor);
    }

    [Theory]
    [InlineData(60, 13)]
    [InlineData(60, 20)]
    [InlineData(0, 5)]
    [InlineData(0, 1)]
    [InlineData(-10, 5)]
    [InlineData(60, 0)]
    [InlineData(60, -1)]
    public void Estimate_OutsideTheDefinition_IsUndefined(double weight, int reps)
    {
        Assert.Null(OneRepMax.Estimate(weight, reps));
    }

    [Theory]
    [InlineData(90, 3, 99, 1)]
    [InlineData(105, 2, 96, 5)]
    [InlineData(97.5, 8, 123.5, 1)]
    public void Estimate_MathematicallyEqualSets_AreExactlyEqual(double weight, int reps, double otherWeight, int otherReps)
    {
        Assert.Equal(OneRepMax.Estimate(otherWeight, otherReps), OneRepMax.Estimate(weight, reps));
    }
}
