using SQLite;
using MuscleMemory.Constants;

namespace MuscleMemory.Models;

public class ActiveWorkoutState
{
    [PrimaryKey]
    public int Id { get; set; } = DomainDefaults.ActiveWorkoutStateId;

    public int SessionId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public int CurrentExerciseIndex { get; set; }
    public bool IsResting { get; set; }
    public DateTime BreakEndTimeUtc { get; set; }
}
