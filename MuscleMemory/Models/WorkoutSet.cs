using SQLite;

namespace MuscleMemory.Models;

public class WorkoutSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionExerciseId { get; set; }

    public double Weight { get; set; }
    public int Reps { get; set; }
    public int SetNumber { get; set; }
}
