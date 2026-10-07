using SQLite;
using MuscleMemory.Constants;

namespace MuscleMemory.Models;

public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(DomainDefaults.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    public MuscleGroup TargetMuscleGroup { get; set; } = MuscleGroup.Other;
    public EquipmentType Equipment { get; set; } = EquipmentType.Other;
}