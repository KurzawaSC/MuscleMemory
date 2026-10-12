using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ExerciseFormViewModel(IExerciseCatalogService exerciseCatalog) : ObservableObject
{
    private Exercise? _existingExercise;

    [ObservableProperty]
    public partial string Title { get; set; } = AppResources.HeaderNewExercise;

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = AppResources.ButtonAdd;

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

    public ObservableCollection<EnumOption> MuscleGroups { get; } = [.. MuscleGroupOptions()];

    public ObservableCollection<EnumOption> EquipmentTypes { get; } = [.. EquipmentOptions()];

    public bool CanSave => TakenNames is not null && !string.IsNullOrWhiteSpace(Name) && !IsNameTaken;

    public string NameError => IsNameTaken ? AppResources.ExerciseNameTakenError : string.Empty;

    private bool IsNameTaken => TakenNames?.Contains(NormalizeName(Name)) == true;

    public async Task BeginNewAsync()
    {
        _existingExercise = null;
        TakenNames = null;
        TakenNames = await LoadTakenNamesAsync();
        ShowTitles();
        Name = string.Empty;
        SelectedMuscleGroup = MuscleGroup.Other;
        SelectedEquipment = EquipmentType.Other;
    }

    public async Task BeginEditAsync(Exercise exercise)
    {
        _existingExercise = exercise;
        TakenNames = null;
        TakenNames = await LoadTakenNamesAsync();
        ShowTitles();
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.TargetMuscleGroup;
        SelectedEquipment = exercise.Equipment;
    }

    public void RefreshText()
    {
        ShowTitles();
        MuscleGroups.ReplaceAll(MuscleGroupOptions());
        EquipmentTypes.ReplaceAll(EquipmentOptions());
        OnPropertyChanged(nameof(NameError));
    }

    private void ShowTitles()
    {
        var isEditing = _existingExercise is not null;
        Title = isEditing ? AppResources.HeaderEditExercise : AppResources.HeaderNewExercise;
        ConfirmText = isEditing ? AppResources.ButtonSave : AppResources.ButtonAdd;
    }

    private static List<EnumOption> MuscleGroupOptions() => [.. Enum.GetValues<MuscleGroup>().Select(EnumOption.For)];

    private static List<EnumOption> EquipmentOptions() => [.. Enum.GetValues<EquipmentType>().Select(EnumOption.For)];

    public async Task<Exercise> SaveAsync()
    {
        if (_existingExercise is { } existingExercise)
        {
            var updatedExercise = BuildExercise(existingExercise.Id);
            await exerciseCatalog.UpdateAsync(updatedExercise);
            return updatedExercise;
        }

        var newExercise = BuildExercise();
        await exerciseCatalog.AddAsync(newExercise);
        return newExercise;
    }

    private async Task<IReadOnlySet<string>> LoadTakenNamesAsync()
    {
        var exercises = await exerciseCatalog.GetAllAsync();
        return exercises
            .Where(exercise => exercise.Id != _existingExercise?.Id)
            .Select(exercise => NormalizeName(exercise.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private Exercise BuildExercise(int id = 0) => new()
    {
        Id = id,
        Name = NormalizeName(Name),
        TargetMuscleGroup = SelectedMuscleGroup,
        Equipment = SelectedEquipment
    };

    private static string NormalizeName(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
