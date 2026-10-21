using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

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
    [NotifyPropertyChangedFor(nameof(SetsByRepsText))]
    public partial int Sets { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetsByRepsText))]
    public partial int Reps { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestText))]
    public partial int BreakTimeInSeconds { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RpeText))]
    public partial int TargetRPE { get; private set; }

    public string SetsByRepsText => string.Format(CultureInfo.CurrentCulture, AppResources.SetsByRepsFormat, Sets, Reps);

    public string RestText => string.Format(CultureInfo.CurrentCulture, AppResources.RestChipFormat, BreakTimeInSeconds);

    public string RpeText => string.Format(CultureInfo.CurrentCulture, AppResources.TargetRpeFormat, TargetRPE);

    public void Apply(ExerciseConfiguration configuration)
    {
        Sets = configuration.Sets;
        Reps = configuration.Reps;
        BreakTimeInSeconds = configuration.BreakTimeInSeconds;
        TargetRPE = configuration.TargetRPE;
        Exercise.MinReps = configuration.MinReps;
        Exercise.MaxReps = configuration.MaxReps;
    }

    partial void OnSetsChanged(int value) => Exercise.Sets = value;

    partial void OnRepsChanged(int value) => Exercise.Reps = value;

    partial void OnBreakTimeInSecondsChanged(int value) => Exercise.BreakTimeInSeconds = value;

    partial void OnTargetRPEChanged(int value) => Exercise.TargetRPE = value;
}
