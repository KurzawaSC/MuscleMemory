using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseFormViewModelTests : IAsyncLifetime
{
    private const string BenchPress = "Bench Press";
    private const string OverheadPress = "Overhead Press";
    private const string NameTaken = "An exercise with this name already exists";

    private readonly TestViewModels _ui = new();
    private ExerciseFormViewModel _form = null!;
    private Exercise _bench = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest, EquipmentType.Barbell);
        await _ui.App.Data.ExerciseAsync(OverheadPress, MuscleGroup.Shoulders, EquipmentType.Barbell);
        _push = await _ui.App.Data.WorkoutAsync("Push Day", _bench);
        _form = _ui.CreateExerciseForm();
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void CanSave_BeforeTheTakenNamesLoad_IsFalse()
    {
        _form.Name = "Deadlift";

        Assert.False(_form.CanSave);
    }

    [Fact]
    public async Task BeginNew_Always_ShowsAnEmptyNewExercise()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await _form.BeginEditAsync(_bench);

        await _form.BeginNewAsync();

        Assert.Equal(("New exercise", "Add"), (_form.Title, _form.ConfirmText));
        Assert.Equal((string.Empty, MuscleGroup.Other, EquipmentType.Other), (_form.Name, _form.SelectedMuscleGroup, _form.SelectedEquipment));
        Assert.False(_form.CanSave);
    }

    [Theory]
    [InlineData("Deadlift", true, "")]
    [InlineData("", false, "")]
    [InlineData("   ", false, "")]
    [InlineData(BenchPress, false, NameTaken)]
    [InlineData("bench press", false, NameTaken)]
    [InlineData("  Bench    Press ", false, NameTaken)]
    public async Task Name_NewExercise_IsValidatedAgainstEveryExistingName(string name, bool canSave, string error)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await _form.BeginNewAsync();

        _form.Name = name;

        Assert.Equal((canSave, error), (_form.CanSave, _form.NameError));
    }

    [Fact]
    public async Task Name_Changed_RaisesCanSaveAndNameError()
    {
        await _form.BeginNewAsync();
        var changed = PropertyChanges.Record(_form);

        _form.Name = BenchPress;

        Assert.Contains(nameof(ExerciseFormViewModel.CanSave), changed);
        Assert.Contains(nameof(ExerciseFormViewModel.NameError), changed);
    }

    [Fact]
    public async Task BeginEdit_Exercise_LoadsItAndAcceptsItsOwnName()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await _form.BeginEditAsync(_bench);

        Assert.Equal(("Edit exercise", "Save"), (_form.Title, _form.ConfirmText));
        Assert.Equal((BenchPress, MuscleGroup.Chest, EquipmentType.Barbell), (_form.Name, _form.SelectedMuscleGroup, _form.SelectedEquipment));
        Assert.True(_form.CanSave);
        _form.Name = OverheadPress;
        Assert.Equal((false, NameTaken), (_form.CanSave, _form.NameError));
    }

    [Fact]
    public async Task Save_NewExercise_StoresItWithANormalizedName()
    {
        await _form.BeginNewAsync();
        _form.Name = "  Romanian   Deadlift ";
        _form.SelectedMuscleGroup = MuscleGroup.Legs;
        _form.SelectedEquipment = EquipmentType.Dumbbell;

        var saved = await _form.SaveAsync();

        var stored = Assert.Single(await _ui.App.Exercises.GetAllAsync(), exercise => exercise.Id == saved.Id);
        Assert.Equal(("Romanian Deadlift", MuscleGroup.Legs, EquipmentType.Dumbbell), (stored.Name, stored.TargetMuscleGroup, stored.Equipment));
    }

    [Fact]
    public async Task Save_EditedExercise_UpdatesItAndTheWorkoutsUsingIt()
    {
        await _form.BeginEditAsync(_bench);
        _form.Name = "Flat Bench Press";
        _form.SelectedEquipment = EquipmentType.Dumbbell;

        await _form.SaveAsync();

        var stored = Assert.Single(await _ui.App.Exercises.GetAllAsync(), exercise => exercise.Id == _bench.Id);
        Assert.Equal(("Flat Bench Press", MuscleGroup.Chest, EquipmentType.Dumbbell), (stored.Name, stored.TargetMuscleGroup, stored.Equipment));
        Assert.Equal("Flat Bench Press", Assert.Single(await _ui.App.Workouts.GetExercisesAsync(_push.Id)).ExerciseName);
    }

    [Fact]
    public async Task Save_Twice_DoesNotDuplicateTheExercise()
    {
        await _form.BeginNewAsync();
        _form.Name = "Deadlift";
        await _form.SaveAsync();

        await _form.BeginNewAsync();
        _form.Name = "deadlift";

        Assert.False(_form.CanSave);
        Assert.Equal(3, (await _ui.App.Exercises.GetAllAsync()).Count);
    }

    [Fact]
    public void Options_EveryEnumMember_IsOffered()
    {
        Assert.Equal(Enum.GetValues<MuscleGroup>().Cast<Enum>(), _form.MuscleGroups.Select(option => option.Value));
        Assert.Equal(Enum.GetValues<EquipmentType>().Cast<Enum>(), _form.EquipmentTypes.Select(option => option.Value));
    }

    [Fact]
    public async Task RefreshText_Polish_ReformatsTitlesOptionsAndError()
    {
        using (new CultureScope(Cultures.EnglishUs))
        {
            await _form.BeginEditAsync(_bench);
            _form.Name = OverheadPress;
        }

        using var polish = new CultureScope(Cultures.Polish);
        var changed = PropertyChanges.Record(_form);

        _form.RefreshText();

        Assert.Equal(("Edytuj ćwiczenie", "Zapisz"), (_form.Title, _form.ConfirmText));
        Assert.Equal("Klatka", _form.MuscleGroups[0].Label);
        Assert.Equal("Sztanga", _form.EquipmentTypes[0].Label);
        Assert.Contains(nameof(ExerciseFormViewModel.NameError), changed);
    }
}
