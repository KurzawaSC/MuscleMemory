using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed record MuscleGroupFilter(MuscleGroup? MuscleGroup, string Label)
{
    public static MuscleGroupFilter All { get; } = new(null, UiText.FilterAll);

    public static MuscleGroupFilter For(MuscleGroup muscleGroup) => new(muscleGroup, muscleGroup.ToDisplayName());

    public bool Matches(Exercise exercise) => MuscleGroup is null || exercise.TargetMuscleGroup == MuscleGroup;
}
