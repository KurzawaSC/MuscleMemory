using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public partial class ExerciseConfigurationViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = AppResources.ButtonAddToWorkout;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial int Sets { get; set; } = DomainDefaults.Sets;

    [ObservableProperty]
    public partial int Reps { get; set; } = DomainDefaults.Reps;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BreakTimeText))]
    public partial int BreakTimeInSeconds { get; set; } = DomainDefaults.BreakTimeInSeconds;

    public string BreakTimeText => string.Format(CultureInfo.CurrentCulture, AppResources.RestDurationFormat, BreakTimeInSeconds);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomRest))]
    public partial RestPreset SelectedRestPreset { get; set; } = RestPreset.For(DomainDefaults.BreakTimeInSeconds);

    [ObservableProperty]
    public partial int TargetRPE { get; set; } = DomainDefaults.TargetRPE;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(MinRepsError))]
    [NotifyPropertyChangedFor(nameof(MaxRepsError))]
    public partial string MinRepsInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(MinRepsError))]
    [NotifyPropertyChangedFor(nameof(MaxRepsError))]
    public partial string MaxRepsInput { get; set; } = string.Empty;

    public bool IsValid => TryReadRange(out _, out _);

    public string MinRepsError => DescribeBoundError(MinRepsInput, MaxRepsInput);

    public string MaxRepsError => DescribeBoundError(MaxRepsInput, MinRepsInput) is { Length: > 0 } boundError ? boundError : DescribeOrderError();

    public IReadOnlyList<RestPreset> RestPresets { get; } =
        [.. DomainDefaults.BreakTimePresetsInSeconds.Select(RestPreset.For), RestPreset.Custom()];

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
        ConfirmText = AppResources.ButtonAddToWorkout;
        Apply(new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE, null, null));
    }

    public void BeginEdit(WorkoutExercise exercise)
    {
        ExerciseName = exercise.ExerciseName;
        IsEditing = true;
        ConfirmText = AppResources.ButtonSave;
        Apply(new ExerciseConfiguration(exercise.Sets, exercise.Reps, exercise.BreakTimeInSeconds, exercise.TargetRPE, exercise.MinReps, exercise.MaxReps));
    }

    public ExerciseConfiguration ToConfiguration()
    {
        TryReadRange(out var minReps, out var maxReps);
        return new(Sets, Reps, BreakTimeInSeconds, TargetRPE, minReps, maxReps);
    }

    [RelayCommand]
    private void SelectRpe(int value) => TargetRPE = value;

    [RelayCommand]
    private void IncreaseMinReps() => MinRepsInput = StepBound(MinRepsInput, DomainDefaults.RepsStep);

    [RelayCommand]
    private void DecreaseMinReps() => MinRepsInput = StepBound(MinRepsInput, -DomainDefaults.RepsStep);

    [RelayCommand]
    private void IncreaseMaxReps() => MaxRepsInput = StepBound(MaxRepsInput, DomainDefaults.RepsStep);

    [RelayCommand]
    private void DecreaseMaxReps() => MaxRepsInput = StepBound(MaxRepsInput, -DomainDefaults.RepsStep);

    private void Apply(ExerciseConfiguration configuration)
    {
        Sets = configuration.Sets;
        Reps = configuration.Reps;
        SelectedRestPreset = RestPresets.FirstOrDefault(preset => preset.Seconds == configuration.BreakTimeInSeconds) ?? RestPresets.First(preset => preset.IsCustom);
        BreakTimeInSeconds = configuration.BreakTimeInSeconds;
        TargetRPE = configuration.TargetRPE;
        MinRepsInput = FormatBound(configuration.MinReps);
        MaxRepsInput = FormatBound(configuration.MaxReps);
    }

    private bool TryReadRange(out int? minReps, out int? maxReps)
    {
        minReps = null;
        maxReps = null;

        if (MinRepsInput.Length == 0 && MaxRepsInput.Length == 0)
        {
            return true;
        }

        if (!TryParseBound(MinRepsInput, out var min) || !TryParseBound(MaxRepsInput, out var max) || !RepRange.IsValid(min, max))
        {
            return false;
        }

        minReps = min;
        maxReps = max;
        return true;
    }

    private string DescribeOrderError() =>
        TryParseBound(MinRepsInput, out var min) && TryParseBound(MaxRepsInput, out var max) && max < min
            ? string.Format(CultureInfo.CurrentCulture, AppResources.RepRangeOrderErrorFormat, min)
            : string.Empty;

    private static string DescribeBoundError(string input, string otherInput)
    {
        if (input.Length > 0 && !TryParseBound(input, out _))
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RepsRangeErrorFormat, RepRange.Lowest, RepRange.Highest);
        }

        return input.Length == 0 && otherInput.Length > 0 ? AppResources.RepRangeMissingBound : string.Empty;
    }

    private static bool TryParseBound(string input, out int reps) =>
        int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out reps) && reps is >= RepRange.Lowest and <= RepRange.Highest;

    private static string StepBound(string input, int step)
    {
        var current = int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var reps) ? reps : 0;
        return FormatBound(Math.Clamp(current + step, RepRange.Lowest, RepRange.Highest));
    }

    private static string FormatBound(int? reps) => reps?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

    private static IEnumerable<LevelSegment> BuildRpeLevels(int targetRpe) =>
        Enumerable.Range(DomainDefaults.MinTargetRPE, DomainDefaults.MaxTargetRPE - DomainDefaults.MinTargetRPE + 1)
                  .Select(value => new LevelSegment(value, value <= targetRpe));
}
