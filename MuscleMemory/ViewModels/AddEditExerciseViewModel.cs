using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class AddEditExerciseViewModel : ObservableObject, IQueryAttributable
{
    private readonly IPopupService _popupService;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroup SelectedMuscleGroup { get; set; } = MuscleGroup.Other;

    [ObservableProperty]
    public partial EquipmentType SelectedEquipment { get; set; } = EquipmentType.Other;

    public MuscleGroup[] MuscleGroups { get; } = Enum.GetValues<MuscleGroup>();
    public EquipmentType[] EquipmentTypes { get; } = Enum.GetValues<EquipmentType>();

    private Exercise? _existingExercise;

    public AddEditExerciseViewModel(IPopupService popupService)
    {
        _popupService = popupService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.ExerciseToEdit, out var editable) && editable is Exercise exercise)
        {
            LoadExercise(exercise);
        }
    }

    private void LoadExercise(Exercise exercise)
    {
        _existingExercise = exercise;
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.TargetMuscleGroup;
        SelectedEquipment = exercise.Equipment;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _popupService.ClosePopupAsync<Exercise?>(Shell.Current.Navigation, null);
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        await _popupService.ClosePopupAsync<Exercise?>(Shell.Current.Navigation, BuildExercise());
    }

    private Exercise BuildExercise()
    {
        if (_existingExercise != null)
        {
            _existingExercise.Name = Name;
            _existingExercise.TargetMuscleGroup = SelectedMuscleGroup;
            _existingExercise.Equipment = SelectedEquipment;
            return _existingExercise;
        }

        return new Exercise
        {
            Name = Name,
            TargetMuscleGroup = SelectedMuscleGroup,
            Equipment = SelectedEquipment
        };
    }
}
