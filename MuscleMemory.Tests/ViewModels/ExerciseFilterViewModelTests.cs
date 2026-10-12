using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseFilterViewModelTests
{
    private readonly ExerciseFilterViewModel _filter = new();
    private readonly ExerciseItem _bench = Item("Bench Press", MuscleGroup.Chest);
    private readonly ExerciseItem _fly = Item("Cable Fly", MuscleGroup.Chest);
    private readonly ExerciseItem _row = Item("Barbell Row", MuscleGroup.Back);
    private readonly ExerciseItem _overheadPress = Item("Overhead Press", MuscleGroup.Shoulders);

    private ExerciseItem[] AllItems => [_bench, _fly, _row, _overheadPress];

    [Fact]
    public void Apply_NoFilterAndNoSearch_ReturnsEveryItemInOrder() =>
        Assert.Equal(AllItems, _filter.Apply(AllItems));

    [Fact]
    public void Apply_MuscleGroupSelected_ReturnsOnlyThatGroup()
    {
        _filter.SelectedFilter = MuscleGroupFilter.For(MuscleGroup.Chest);

        Assert.Equal([_bench, _fly], _filter.Apply(AllItems));
    }

    [Theory]
    [InlineData("press")]
    [InlineData("PRESS")]
    [InlineData("  Press  ")]
    public void Apply_SearchText_MatchesNameIgnoringCaseAndSurroundingSpaces(string search)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _filter.SearchText = search;

        Assert.Equal([_bench, _overheadPress], _filter.Apply(AllItems));
    }

    [Fact]
    public void Apply_SearchAndGroup_CombinesBothConditions()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _filter.SearchText = "press";
        _filter.SelectedFilter = MuscleGroupFilter.For(MuscleGroup.Shoulders);

        Assert.Equal([_overheadPress], _filter.Apply(AllItems));
    }

    [Fact]
    public void Apply_NoMatch_ReturnsEmpty()
    {
        _filter.SearchText = "deadlift";

        Assert.Empty(_filter.Apply(AllItems));
    }

    [Fact]
    public void UpdateFilters_Exercises_ListsAllThenDistinctGroupsInEnumOrder()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _filter.UpdateFilters(Exercises(MuscleGroup.Shoulders, MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Chest));

        Assert.Equal(
            [null, MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders],
            _filter.Filters.Select(filter => filter.MuscleGroup));
        Assert.Equal("All", _filter.Filters[0].Label);
    }

    [Fact]
    public void UpdateFilters_SameGroups_KeepsFilterInstances()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _filter.UpdateFilters(Exercises(MuscleGroup.Chest, MuscleGroup.Back));
        var before = _filter.Filters.ToList();
        var collectionChanges = 0;
        _filter.Filters.CollectionChanged += (_, _) => collectionChanges++;

        _filter.UpdateFilters(Exercises(MuscleGroup.Back, MuscleGroup.Chest));

        Assert.Equal(0, collectionChanges);
        Assert.All(before.Zip(_filter.Filters), pair => Assert.Same(pair.First, pair.Second));
    }

    [Fact]
    public void UpdateFilters_SelectedGroupStillPresent_KeepsSelection()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _filter.UpdateFilters(Exercises(MuscleGroup.Chest, MuscleGroup.Back));
        _filter.SelectedFilter = _filter.Filters[2];

        _filter.UpdateFilters(Exercises(MuscleGroup.Back, MuscleGroup.Legs));

        Assert.Equal(MuscleGroup.Back, _filter.SelectedFilter.MuscleGroup);
        Assert.Contains(_filter.SelectedFilter, _filter.Filters);
    }

    [Fact]
    public void UpdateFilters_SelectedGroupGone_SelectsAll()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _filter.UpdateFilters(Exercises(MuscleGroup.Chest, MuscleGroup.Back));
        _filter.SelectedFilter = _filter.Filters[1];

        _filter.UpdateFilters(Exercises(MuscleGroup.Back));

        Assert.Same(_filter.Filters[0], _filter.SelectedFilter);
        Assert.Null(_filter.SelectedFilter.MuscleGroup);
    }

    [Fact]
    public void UpdateFilters_LanguageChanged_RebuildsLabelsAndKeepsSelectedGroup()
    {
        using (new CultureScope(Cultures.EnglishUs))
        {
            _filter.UpdateFilters(Exercises(MuscleGroup.Chest));
            _filter.SelectedFilter = _filter.Filters[1];
        }

        using var scope = new CultureScope(Cultures.Polish);
        _filter.UpdateFilters(Exercises(MuscleGroup.Chest));

        Assert.Equal("Wszystkie", _filter.Filters[0].Label);
        Assert.Same(_filter.Filters[1], _filter.SelectedFilter);
    }

    [Fact]
    public void Reset_AfterFiltering_ClearsSearchAndSelectsAll()
    {
        _filter.SearchText = "press";
        _filter.SelectedFilter = MuscleGroupFilter.For(MuscleGroup.Chest);

        _filter.Reset();

        Assert.Equal(string.Empty, _filter.SearchText);
        Assert.Null(_filter.SelectedFilter.MuscleGroup);
    }

    [Fact]
    public void Changed_SearchOrFilterChanges_IsRaisedForEach()
    {
        var raised = 0;
        _filter.Changed += (_, _) => raised++;

        _filter.SearchText = "press";
        _filter.SelectedFilter = MuscleGroupFilter.For(MuscleGroup.Chest);

        Assert.Equal(2, raised);
    }

    private static ExerciseItem Item(string name, MuscleGroup muscleGroup) =>
        new(new Exercise { Name = name, TargetMuscleGroup = muscleGroup }, string.Empty, string.Empty, string.Empty);

    private static Exercise[] Exercises(params MuscleGroup[] muscleGroups) =>
        [.. muscleGroups.Select(muscleGroup => new Exercise { TargetMuscleGroup = muscleGroup })];
}
