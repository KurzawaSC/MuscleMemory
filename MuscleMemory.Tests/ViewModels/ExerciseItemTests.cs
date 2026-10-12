using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseItemTests
{
    [Fact]
    public void Create_English_FormatsMuscleGroupAndEquipment()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var exercise = new Exercise { Name = "Bench Press", TargetMuscleGroup = MuscleGroup.Chest, Equipment = EquipmentType.Barbell };

        var item = ExerciseItem.Create(exercise);

        Assert.Same(exercise, item.Exercise);
        Assert.Equal("Chest", item.MuscleGroupText);
        Assert.Equal("Barbell", item.EquipmentText);
        Assert.Equal("Chest · Barbell", item.DetailText);
    }

    [Fact]
    public void Create_Polish_TranslatesLabels()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = ExerciseItem.Create(new Exercise { TargetMuscleGroup = MuscleGroup.FullBody, Equipment = EquipmentType.None });

        Assert.Equal("Całe ciało · Brak", item.DetailText);
    }
}
