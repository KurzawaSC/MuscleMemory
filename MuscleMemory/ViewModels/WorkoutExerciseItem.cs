using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed partial class WorkoutExerciseItem : ObservableObject
{
    public WorkoutExerciseItem(WorkoutExercise exercise)
    {
        Exercise = exercise;
        Sets = exercise.Sets;
        Reps = exercise.Reps;
        BreakTimeInSeconds = exercise.BreakTimeInSeconds;
        TargetRPE = exercise.TargetRPE;
    }

    public WorkoutExercise Exercise { get; }

    public string ExerciseName => Exercise.ExerciseName;

    [ObservableProperty]
    public partial int Sets { get; private set; }

    [ObservableProperty]
    public partial int Reps { get; private set; }

    [ObservableProperty]
    public partial int BreakTimeInSeconds { get; private set; }

    [ObservableProperty]
    public partial int TargetRPE { get; private set; }

    public void Apply(ExerciseConfiguration configuration)
    {
        Sets = configuration.Sets;
        Reps = configuration.Reps;
        BreakTimeInSeconds = configuration.BreakTimeInSeconds;
        TargetRPE = configuration.TargetRPE;
    }

    partial void OnSetsChanged(int value) => Exercise.Sets = value;

    partial void OnRepsChanged(int value) => Exercise.Reps = value;

    partial void OnBreakTimeInSecondsChanged(int value) => Exercise.BreakTimeInSeconds = value;

    partial void OnTargetRPEChanged(int value) => Exercise.TargetRPE = value;
}
