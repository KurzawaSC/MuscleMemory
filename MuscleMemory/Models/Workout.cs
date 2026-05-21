using SQLite;

namespace MuscleMemory.Models;

public class Workout
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}