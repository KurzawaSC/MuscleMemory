using MuscleMemory.Models;

namespace MuscleMemory.Extensions;

public static class WorkoutSetExtensions
{
    public static double Volume(this WorkoutSet set) => set.Weight * set.Reps;

    public static double TotalVolume(this IEnumerable<WorkoutSet> sets) => sets.Sum(Volume);
}
