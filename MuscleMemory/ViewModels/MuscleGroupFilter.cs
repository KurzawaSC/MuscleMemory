using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record MuscleGroupFilter(MuscleGroup? MuscleGroup, string Label)
{
    public static MuscleGroupFilter All() => new(null, AppResources.FilterAll);

    public static MuscleGroupFilter For(MuscleGroup muscleGroup) => new(muscleGroup, muscleGroup.ToDisplayName());

    public bool Matches(Exercise exercise) => MuscleGroup is null || exercise.TargetMuscleGroup == MuscleGroup;
}
