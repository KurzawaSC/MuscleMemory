using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class MuscleGroupFilterTests
{
    private readonly Exercise _chestExercise = new() { TargetMuscleGroup = MuscleGroup.Chest };
    private readonly Exercise _backExercise = new() { TargetMuscleGroup = MuscleGroup.Back };

    [Fact]
    public void All_English_HasNoMuscleGroupAndAllLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var filter = MuscleGroupFilter.All();

        Assert.Null(filter.MuscleGroup);
        Assert.Equal("All", filter.Label);
    }

    [Fact]
    public void All_Polish_IsBuiltInCurrentLanguage()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Wszystkie", MuscleGroupFilter.All().Label);
    }

    [Fact]
    public void For_MuscleGroup_CarriesGroupAndLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var filter = MuscleGroupFilter.For(MuscleGroup.Chest);

        Assert.Equal(MuscleGroup.Chest, filter.MuscleGroup);
        Assert.Equal("Chest", filter.Label);
    }

    [Fact]
    public void Matches_AllFilter_MatchesEveryExercise()
    {
        var filter = MuscleGroupFilter.All();

        Assert.True(filter.Matches(_chestExercise));
        Assert.True(filter.Matches(_backExercise));
    }

    [Fact]
    public void Matches_GroupFilter_MatchesOnlyThatGroup()
    {
        var filter = MuscleGroupFilter.For(MuscleGroup.Chest);

        Assert.True(filter.Matches(_chestExercise));
        Assert.False(filter.Matches(_backExercise));
    }

    [Fact]
    public void Equals_SameGroupAndLabel_IsEqual() =>
        Assert.Equal(MuscleGroupFilter.For(MuscleGroup.Back), MuscleGroupFilter.For(MuscleGroup.Back));
}
