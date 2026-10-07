using SQLite;
using MuscleMemory.Constants;

namespace MuscleMemory.Models;

public class Workout
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(DomainDefaults.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
}
