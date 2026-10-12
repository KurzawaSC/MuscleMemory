using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.Tests.Extensions;

public sealed class WorkoutSetExtensionsTests
{
    [Theory]
    [InlineData(80, 8, 640)]
    [InlineData(82.5, 8, 660)]
    [InlineData(0, 12, 0)]
    [InlineData(22.75, 3, 68.25)]
    public void Volume_WeightAndReps_ReturnsProduct(double weight, int reps, double expected) =>
        Assert.Equal(expected, new WorkoutSet { Weight = weight, Reps = reps }.Volume());

    [Fact]
    public void TotalVolume_SeveralSets_SumsVolumes()
    {
        WorkoutSet[] sets =
        [
            new() { Weight = 100, Reps = 5 },
            new() { Weight = 82.5, Reps = 8 },
            new() { Weight = 0, Reps = 15 }
        ];

        Assert.Equal(1160, sets.TotalVolume());
    }

    [Fact]
    public void TotalVolume_NoSets_ReturnsZero() =>
        Assert.Equal(0, Array.Empty<WorkoutSet>().TotalVolume());
}
