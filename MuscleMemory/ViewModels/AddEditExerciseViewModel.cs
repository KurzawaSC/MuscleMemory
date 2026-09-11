using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class AddEditExerciseViewModel(IExerciseRepository exerciseRepository) : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
    private Exercise? _existingExercise;

    [ObservableProperty]
    public partial string Title { get; set; } = UiText.HeaderNewExercise;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = UiText.ButtonAdd;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroup SelectedMuscleGroup { get; set; } = MuscleGroup.Other;

    [ObservableProperty]
    public partial EquipmentType SelectedEquipment { get; set; } = EquipmentType.Other;

    public MuscleGroup[] MuscleGroups { get; } = Enum.GetValues<MuscleGroup>();

    public EquipmentType[] EquipmentTypes { get; } = Enum.GetValues<EquipmentType>();

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    public void BeginNew()
    {
        _existingExercise = null;
        Title = UiText.HeaderNewExercise;
        ConfirmText = UiText.ButtonAdd;
        Name = string.Empty;
        SelectedMuscleGroup = MuscleGroup.Other;
        SelectedEquipment = EquipmentType.Other;
    }

    public void BeginEdit(Exercise exercise)
    {
        _existingExercise = exercise;
        Title = UiText.HeaderEditExercise;
        ConfirmText = UiText.ButtonSave;
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.TargetMuscleGroup;
        SelectedEquipment = exercise.Equipment;
    }

    public async Task<Exercise> SaveAsync()
    {
        if (_existingExercise is { } exercise)
        {
            ApplyTo(exercise);
            await _exerciseRepository.UpdateAsync(exercise);
            return exercise;
        }

        var newExercise = new Exercise();
        ApplyTo(newExercise);
        await _exerciseRepository.AddAsync(newExercise);
        return newExercise;
    }

    private void ApplyTo(Exercise exercise)
    {
        exercise.Name = Name.Trim();
        exercise.TargetMuscleGroup = SelectedMuscleGroup;
        exercise.Equipment = SelectedEquipment;
    }
}
