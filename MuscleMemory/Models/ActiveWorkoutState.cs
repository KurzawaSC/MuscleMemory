using SQLite;

namespace MuscleMemory.Models;

public class ActiveWorkoutState
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Always 1 for singleton
    public int WorkoutId { get; set; }
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public int CurrentExerciseIndex { get; set; }
    public bool IsResting { get; set; }
    public DateTime BreakEndTime { get; set; }
}
