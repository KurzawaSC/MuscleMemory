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
    [NotifyPropertyChangedFor(nameof(NameError))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroup SelectedMuscleGroup { get; set; } = MuscleGroup.Other;

    [ObservableProperty]
    public partial EquipmentType SelectedEquipment { get; set; } = EquipmentType.Other;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(NameError))]
    private partial IReadOnlySet<string> TakenNames { get; set; } = new HashSet<string>();

    public MuscleGroup[] MuscleGroups { get; } = Enum.GetValues<MuscleGroup>();

    public EquipmentType[] EquipmentTypes { get; } = Enum.GetValues<EquipmentType>();

    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && !IsNameTaken;

    public string NameError => IsNameTaken ? UiText.ExerciseNameTakenError : string.Empty;

    private bool IsNameTaken => TakenNames.Contains(Name.Trim());

    public async Task BeginNewAsync()
    {
        _existingExercise = null;
        TakenNames = await LoadTakenNamesAsync();
        Title = UiText.HeaderNewExercise;
        ConfirmText = UiText.ButtonAdd;
        Name = string.Empty;
        SelectedMuscleGroup = MuscleGroup.Other;
        SelectedEquipment = EquipmentType.Other;
    }

    public async Task BeginEditAsync(Exercise exercise)
    {
        _existingExercise = exercise;
        TakenNames = await LoadTakenNamesAsync();
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

    private async Task<IReadOnlySet<string>> LoadTakenNamesAsync()
    {
        var exercises = await _exerciseRepository.GetAllAsync();
        return exercises
            .Where(exercise => exercise.Id != _existingExercise?.Id)
            .Select(exercise => exercise.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void ApplyTo(Exercise exercise)
    {
        exercise.Name = Name.Trim();
        exercise.TargetMuscleGroup = SelectedMuscleGroup;
        exercise.Equipment = SelectedEquipment;
    }
}
