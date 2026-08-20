using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record ExerciseHistoryEntry(DateTime DateUtc, string WorkoutName, IReadOnlyList<WorkoutSet> Sets)
{
    public DateTime LocalDate => DateUtc.ToLocalTime();
}
