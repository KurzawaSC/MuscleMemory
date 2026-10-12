using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExercisePickerViewModelTests : IAsyncLifetime
{
    private const string BenchPress = "Bench Press";
    private const string InclinePress = "Incline Dumbbell Press";
    private const string BarbellRow = "Barbell Row";

    private readonly TestViewModels _ui = new();
    private ExercisePickerViewModel _picker = null!;

    public ValueTask InitializeAsync()
    {
        _picker = _ui.CreateExercisePicker();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public async Task Load_NoExercises_ShowsNoNoMatchesMessage()
    {
        await _picker.LoadAsync();

        Assert.Empty(_picker.Exercises);
        Assert.False(_picker.HasNoMatches);
    }

    [Fact]
    public async Task Load_Exercises_ListsEveryExerciseUnderAll()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await _picker.LoadAsync();

        Assert.Equal([BenchPress, InclinePress, BarbellRow], _picker.Exercises.Select(item => item.Exercise.Name));
        Assert.Equal(["All", "Chest", "Back"], _picker.Filter.Filters.Select(filter => filter.Label));
    }

    [Fact]
    public async Task Filter_GroupAndSearch_NarrowTheSameItems()
    {
        await SeedAsync();
        await _picker.LoadAsync();
        var incline = _picker.Exercises[1];

        _picker.Filter.SelectedFilter = _picker.Filter.Filters.Single(filter => filter.MuscleGroup == MuscleGroup.Chest);
        _picker.Filter.SearchText = "dumbbell";

        Assert.Same(incline, Assert.Single(_picker.Exercises));
    }

    [Fact]
    public async Task Filter_NothingMatches_ShowsNoMatches()
    {
        await SeedAsync();
        await _picker.LoadAsync();

        _picker.Filter.SearchText = "Squat";

        Assert.Empty(_picker.Exercises);
        Assert.True(_picker.HasNoMatches);
    }

    [Fact]
    public async Task Load_Again_ResetsThePreviousFilter()
    {
        await SeedAsync();
        await _picker.LoadAsync();
        _picker.Filter.SelectedFilter = _picker.Filter.Filters.Single(filter => filter.MuscleGroup == MuscleGroup.Back);
        _picker.Filter.SearchText = "row";

        await _picker.LoadAsync();

        Assert.Equal((string.Empty, null), (_picker.Filter.SearchText, _picker.Filter.SelectedFilter.MuscleGroup));
        Assert.Equal(3, _picker.Exercises.Count);
        Assert.False(_picker.HasNoMatches);
    }

    private async Task SeedAsync()
    {
        await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest, EquipmentType.Barbell);
        await _ui.App.Data.ExerciseAsync(InclinePress, MuscleGroup.Chest, EquipmentType.Dumbbell);
        await _ui.App.Data.ExerciseAsync(BarbellRow, MuscleGroup.Back, EquipmentType.Barbell);
    }
}
