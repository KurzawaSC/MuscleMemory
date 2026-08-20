using SQLite;
using MuscleMemory.Constants;

namespace MuscleMemory.Models;

public class SessionExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutSessionId { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int Order { get; set; }

    public int PlannedSets { get; set; }
    public int PlannedReps { get; set; }
    public int BreakTimeInSeconds { get; set; }
    public int TargetRPE { get; set; } = DomainDefaults.TargetRPE;
}
