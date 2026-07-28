using SQLite;

namespace MuscleMemory.Models;

public class WorkoutSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Identyfikator ćwiczenia w aktualnym treningu
    public int WorkoutExerciseId { get; set; }

    public double Weight { get; set; }
    public int Reps { get; set; }
    public int SetNumber { get; set; }
}