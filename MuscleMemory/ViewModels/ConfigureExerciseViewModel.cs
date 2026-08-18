using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ConfigureExerciseViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPopupService _popupService;

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

    public ConfigureExerciseViewModel(IPopupService popupService)
    {
        _popupService = popupService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.SelectedExercise, out var selected) && selected is Exercise exercise)
        {
            ExerciseName = exercise.Name;
        }
        else if (query.TryGetValue(QueryKeys.ExerciseToEdit, out var editable) && editable is WorkoutExercise workoutExercise)
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
        var configuration = new ExerciseConfiguration(
            ParseOrZero(SetsInput),
            ParseOrZero(RepsInput),
            ParseOrZero(BreakTimeInput),
            TargetRPE);

        await _popupService.ClosePopupAsync<ExerciseConfiguration?>(Shell.Current.Navigation, configuration);
    }

    private static int ParseOrZero(string value) => int.TryParse(value, out int parsed) ? parsed : 0;
}
