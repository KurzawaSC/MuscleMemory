using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class AddEditExerciseViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroup SelectedMuscleGroup { get; set; } = MuscleGroup.Other;

    [ObservableProperty]
    public partial EquipmentType SelectedEquipment { get; set; } = EquipmentType.Other;

    public MuscleGroup[] MuscleGroups { get; } = (MuscleGroup[])Enum.GetValues(typeof(MuscleGroup));
    public EquipmentType[] EquipmentTypes { get; } = (EquipmentType[])Enum.GetValues(typeof(EquipmentType));

    public Exercise? ExistingExercise { get; set; }

    public void LoadExercise(Exercise exercise)
    {
        ExistingExercise = exercise;
        Name = exercise.Name;
        SelectedMuscleGroup = exercise.TargetMuscleGroup;
        SelectedEquipment = exercise.Equipment;
    }
    
    public Exercise GetExercise()
    {
        if (ExistingExercise != null)
        {
            ExistingExercise.Name = Name;
            ExistingExercise.TargetMuscleGroup = SelectedMuscleGroup;
            ExistingExercise.Equipment = SelectedEquipment;
            return ExistingExercise;
        }
        
        return new Exercise
        {
            Name = Name,
            TargetMuscleGroup = SelectedMuscleGroup,
            Equipment = SelectedEquipment
        };
    }
}
