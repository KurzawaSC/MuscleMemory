using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed record EnumOption(Enum Value, string Label)
{
    public static EnumOption For(MuscleGroup muscleGroup) => new(muscleGroup, muscleGroup.ToDisplayName());

    public static EnumOption For(EquipmentType equipment) => new(equipment, equipment.ToDisplayName());
}
