using SQLite;

namespace MuscleMemory.Models;

public class WorkoutExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutId { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    public int Sets { get; set; }
    public int Reps { get; set; }
    public int BreakTimeInSeconds { get; set; }

    public string ExerciseName { get; set; } = string.Empty;
}