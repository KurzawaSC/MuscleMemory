using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ConfigureExerciseViewModel(IPopupService popupService) : ObservableObject, IQueryAttributable
{
    private readonly IPopupService _popupService = popupService;

    private static readonly NumericField SetsField =
        new(UiText.FieldSets, DomainDefaults.MinSets, DomainDefaults.MaxSets);

    private static readonly NumericField RepsField =
        new(UiText.FieldReps, DomainDefaults.MinReps, DomainDefaults.MaxReps);

    private static readonly NumericField BreakTimeField =
        new(UiText.FieldBreakTime, DomainDefaults.MinBreakTimeInSeconds, DomainDefaults.MaxBreakTimeInSeconds);

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SetsInput { get; set; } = DomainDefaults.Sets.ToString();

    [ObservableProperty]
    public partial string RepsInput { get; set; } = DomainDefaults.Reps.ToString();

    [ObservableProperty]
    public partial string BreakTimeInput { get; set; } = DomainDefaults.BreakTimeInSeconds.ToString();

    [ObservableProperty]
    public partial int TargetRPE { get; set; } = DomainDefaults.TargetRPE;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = UiText.ButtonAdd;

    public IReadOnlyList<int> TargetRpeOptions { get; } =
        [.. Enumerable.Range(DomainDefaults.MinTargetRPE, DomainDefaults.MaxTargetRPE - DomainDefaults.MinTargetRPE + 1)];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.SelectedExercise, out var selected) && selected is Exercise exercise)
        {
            ExerciseName = exercise.Name;
        }
        else if (query.TryGetValue(QueryKeys.WorkoutExerciseToEdit, out var editable) && editable is WorkoutExercise workoutExercise)
        {
            LoadExerciseToEdit(workoutExercise);
        }
    }

    private void LoadExerciseToEdit(WorkoutExercise workoutExercise)
    {
        ExerciseName = workoutExercise.ExerciseName;
        SetsInput = workoutExercise.Sets.ToString();
        RepsInput = workoutExercise.Reps.ToString();
        BreakTimeInput = workoutExercise.BreakTimeInSeconds.ToString();
        TargetRPE = workoutExercise.TargetRPE;
        ConfirmText = UiText.ButtonSave;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _popupService.ClosePopupAsync<ExerciseConfiguration?>(Shell.Current.Navigation, null);
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (SetsField.Parse(SetsInput) is not int sets)
        {
            await ShowRangeAlertAsync(SetsField);
            return;
        }

        if (RepsField.Parse(RepsInput) is not int reps)
        {
            await ShowRangeAlertAsync(RepsField);
            return;
        }

        if (BreakTimeField.Parse(BreakTimeInput) is not int breakTimeInSeconds)
        {
            await ShowRangeAlertAsync(BreakTimeField);
            return;
        }

        await _popupService.ClosePopupAsync<ExerciseConfiguration?>(
            Shell.Current.Navigation,
            new ExerciseConfiguration(sets, reps, breakTimeInSeconds, TargetRPE));
    }

    private static Task ShowRangeAlertAsync(NumericField field) =>
        Shell.Current.DisplayAlertAsync(UiText.TitleInvalidInput, field.RangeMessage, UiText.ButtonOk);

    private sealed record NumericField(string Label, int Minimum, int Maximum)
    {
        public int? Parse(string input) =>
            int.TryParse(input, out int value) && value >= Minimum && value <= Maximum ? value : null;

        public string RangeMessage => string.Format(UiText.NumericRangeFormat, Label, Minimum, Maximum);
    }
}
