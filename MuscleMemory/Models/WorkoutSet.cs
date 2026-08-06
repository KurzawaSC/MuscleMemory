using SQLite;

namespace MuscleMemory.Models;

public class WorkoutSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Identyfikator ćwiczenia w aktualnym treningu
    public int WorkoutExerciseId { get; set; }

    // Identyfikator sesji treningowej (epoch timestamp) — izoluje serie bieżącej sesji od historycznych
    public int WorkoutSessionId { get; set; }

    public double Weight { get; set; }
    public int Reps { get; set; }
    public int SetNumber { get; set; }
}