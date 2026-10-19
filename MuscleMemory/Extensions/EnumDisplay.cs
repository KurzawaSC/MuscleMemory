using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.Extensions;

public static class EnumDisplay
{
    public static string ToDisplayName(this MuscleGroup muscleGroup) => muscleGroup switch
    {
        MuscleGroup.Chest => AppResources.MuscleGroupChest,
        MuscleGroup.Back => AppResources.MuscleGroupBack,
        MuscleGroup.Shoulders => AppResources.MuscleGroupShoulders,
        MuscleGroup.Biceps => AppResources.MuscleGroupBiceps,
        MuscleGroup.Triceps => AppResources.MuscleGroupTriceps,
        MuscleGroup.Legs => AppResources.MuscleGroupLegs,
        MuscleGroup.Calves => AppResources.MuscleGroupCalves,
        MuscleGroup.Abs => AppResources.MuscleGroupAbs,
        MuscleGroup.FullBody => AppResources.MuscleGroupFullBody,
        MuscleGroup.Other => AppResources.MuscleGroupOther,
        _ => throw new ArgumentOutOfRangeException(nameof(muscleGroup), muscleGroup, null)
    };

    public static string ToDisplayName(this EquipmentType equipment) => equipment switch
    {
        EquipmentType.Barbell => AppResources.EquipmentBarbell,
        EquipmentType.Dumbbell => AppResources.EquipmentDumbbell,
        EquipmentType.Machine => AppResources.EquipmentMachine,
        EquipmentType.Cable => AppResources.EquipmentCable,
        EquipmentType.Kettlebell => AppResources.EquipmentKettlebell,
        EquipmentType.Band => AppResources.EquipmentBand,
        EquipmentType.None => AppResources.EquipmentNone,
        EquipmentType.Other => AppResources.EquipmentOther,
        _ => throw new ArgumentOutOfRangeException(nameof(equipment), equipment, null)
    };

    public static string ToDisplayName(this ThemePreference theme) => theme switch
    {
        ThemePreference.System => AppResources.ThemeNameSystem,
        ThemePreference.Light => AppResources.ThemeNameLight,
        ThemePreference.Dark => AppResources.ThemeNameDark,
        _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
    };

    public static string ToDisplayName(this RecordKind kind) => kind switch
    {
        RecordKind.HeaviestWeight => AppResources.RecordHeaviestWeight,
        RecordKind.BestOneRepMax => AppResources.RecordBestOneRepMax,
        RecordKind.BestSessionVolume => AppResources.RecordBestSessionVolume,
        RecordKind.MostReps => AppResources.RecordMostReps,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static string ToDisplayName(this ProgressMetric metric) => metric switch
    {
        ProgressMetric.OneRepMax => AppResources.ProgressMetricOneRepMax,
        ProgressMetric.Volume => AppResources.ProgressMetricVolume,
        ProgressMetric.Reps => AppResources.ProgressMetricReps,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };

    public static string ToDescription(this ProgressMetric metric) => metric switch
    {
        ProgressMetric.OneRepMax => AppResources.ProgressMetricOneRepMaxName,
        ProgressMetric.Volume => AppResources.ProgressMetricVolumeName,
        ProgressMetric.Reps => AppResources.ProgressMetricRepsName,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };

    public static string ToDisplayName(this ProgressRange range) => range switch
    {
        ProgressRange.ThreeMonths => AppResources.ProgressRangeThreeMonths,
        ProgressRange.SixMonths => AppResources.ProgressRangeSixMonths,
        ProgressRange.TwelveMonths => AppResources.ProgressRangeTwelveMonths,
        ProgressRange.All => AppResources.ProgressRangeAll,
        _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
    };

    public static string ToDescription(this ProgressRange range) => range switch
    {
        ProgressRange.ThreeMonths => AppResources.ProgressRangeThreeMonthsName,
        ProgressRange.SixMonths => AppResources.ProgressRangeSixMonthsName,
        ProgressRange.TwelveMonths => AppResources.ProgressRangeTwelveMonthsName,
        ProgressRange.All => AppResources.ProgressRangeAllName,
        _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
    };
}
