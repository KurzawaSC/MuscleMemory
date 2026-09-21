using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ConfigureExerciseViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = UiText.ButtonAddToWorkout;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial int Sets { get; set; } = DomainDefaults.Sets;

    [ObservableProperty]
    public partial int Reps { get; set; } = DomainDefaults.Reps;

    [ObservableProperty]
    public partial int BreakTimeInSeconds { get; set; } = DomainDefaults.BreakTimeInSeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomRest))]
    public partial RestPreset SelectedRestPreset { get; set; } = RestPreset.For(DomainDefaults.BreakTimeInSeconds);

    [ObservableProperty]
    public partial int TargetRPE { get; set; } = DomainDefaults.TargetRPE;

    public IReadOnlyList<RestPreset> RestPresets { get; } =
        [.. DomainDefaults.BreakTimePresetsInSeconds.Select(RestPreset.For), RestPreset.Custom];

    public ObservableCollection<LevelSegment> RpeLevels { get; } = [.. BuildRpeLevels(DomainDefaults.TargetRPE)];

    public bool IsCustomRest => SelectedRestPreset.IsCustom;

    partial void OnSelectedRestPresetChanged(RestPreset value)
    {
        if (value.Seconds is int seconds)
        {
            BreakTimeInSeconds = seconds;
        }
    }

    partial void OnTargetRPEChanged(int value) => RpeLevels.ReplaceAll(BuildRpeLevels(value));

    public void BeginNew(string exerciseName)
    {
        ExerciseName = exerciseName;
        IsEditing = false;
        ConfirmText = UiText.ButtonAddToWorkout;
        Apply(new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE));
    }

    public void BeginEdit(WorkoutExercise exercise)
    {
        ExerciseName = exercise.ExerciseName;
        IsEditing = true;
        ConfirmText = UiText.ButtonSave;
        Apply(new ExerciseConfiguration(exercise.Sets, exercise.Reps, exercise.BreakTimeInSeconds, exercise.TargetRPE));
    }

    public ExerciseConfiguration ToConfiguration() => new(Sets, Reps, BreakTimeInSeconds, TargetRPE);

    [RelayCommand]
    private void SelectRpe(int value) => TargetRPE = value;

    private void Apply(ExerciseConfiguration configuration)
    {
        Sets = configuration.Sets;
        Reps = configuration.Reps;
        SelectedRestPreset = RestPresets.FirstOrDefault(preset => preset.Seconds == configuration.BreakTimeInSeconds) ?? RestPreset.Custom;
        BreakTimeInSeconds = configuration.BreakTimeInSeconds;
        TargetRPE = configuration.TargetRPE;
    }

    private static IEnumerable<LevelSegment> BuildRpeLevels(int targetRpe) =>
        Enumerable.Range(DomainDefaults.MinTargetRPE, DomainDefaults.MaxTargetRPE - DomainDefaults.MinTargetRPE + 1)
                  .Select(value => new LevelSegment(value, value <= targetRpe));
}
