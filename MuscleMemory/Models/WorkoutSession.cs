using SQLite;

namespace MuscleMemory.Models;

public class WorkoutSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int WorkoutId { get; set; }

    public string WorkoutName { get; set; } = string.Empty;

    public DateTime StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
}
