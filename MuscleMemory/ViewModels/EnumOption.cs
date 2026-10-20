using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Progression;

namespace MuscleMemory.ViewModels;

public sealed record EnumOption(Enum Value, string Label, string Description)
{
    public EnumOption(Enum value, string label)
        : this(value, label, label)
    {
    }

    public static EnumOption For(MuscleGroup muscleGroup) => new(muscleGroup, muscleGroup.ToDisplayName());

    public static EnumOption For(EquipmentType equipment) => new(equipment, equipment.ToDisplayName());

    public static EnumOption For(ProgressMetric metric) => new(metric, metric.ToDisplayName(), metric.ToDescription());

    public static EnumOption For(ProgressRange range) => new(range, range.ToDisplayName(), range.ToDescription());
}
