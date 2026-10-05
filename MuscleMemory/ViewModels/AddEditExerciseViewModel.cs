using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class AddEditExerciseViewModel(IExerciseRepository exerciseRepository, IExerciseCatalogService exerciseCatalog) : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
    private readonly IExerciseCatalogService _exerciseCatalog = exerciseCatalog;
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
    private partial IReadOnlySet<string>? TakenNames { get; set; }

    public MuscleGroup[] MuscleGroups { get; } = Enum.GetValues<MuscleGroup>();

    public EquipmentType[] EquipmentTypes { get; } = Enum.GetValues<EquipmentType>();

    public bool CanSave => TakenNames is not null && !string.IsNullOrWhiteSpace(Name) && !IsNameTaken;

    public string NameError => IsNameTaken ? UiText.ExerciseNameTakenError : string.Empty;

    private bool IsNameTaken => TakenNames?.Contains(Name.Trim()) == true;

    public async Task BeginNewAsync()
    {
        _existingExercise = null;
        TakenNames = null;
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
        TakenNames = null;
        TakenNames = await LoadTakenNamesAsync();
        Title = UiText.HeaderEditExercise;
        ConfirmText = UiText.ButtonSave;
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.TargetMuscleGroup;
        SelectedEquipment = exercise.Equipment;
    }

    public async Task<Exercise> SaveAsync()
    {
        if (_existingExercise is { } existingExercise)
        {
            var updatedExercise = BuildExercise(existingExercise.Id);
            await _exerciseCatalog.UpdateAsync(updatedExercise);
            return updatedExercise;
        }

        var newExercise = BuildExercise();
        await _exerciseCatalog.AddAsync(newExercise);
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

    private Exercise BuildExercise(int id = 0) => new()
    {
        Id = id,
        Name = Name.Trim(),
        TargetMuscleGroup = SelectedMuscleGroup,
        Equipment = SelectedEquipment
    };
}
