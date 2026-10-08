using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed record ExerciseItem(
    Exercise Exercise,
    string MuscleGroupText,
    string EquipmentText,
    string DetailText)
{
    public static ExerciseItem Create(Exercise exercise)
    {
        var muscleGroupText = exercise.TargetMuscleGroup.ToDisplayName();
        var equipmentText = exercise.Equipment.ToDisplayName();

        return new ExerciseItem(exercise, muscleGroupText, equipmentText, string.Format(CultureInfo.CurrentCulture, UiText.ListPairFormat, muscleGroupText, equipmentText));
    }
}
