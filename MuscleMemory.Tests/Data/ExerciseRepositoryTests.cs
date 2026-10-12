using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Data;

public sealed class ExerciseRepositoryTests : IAsyncLifetime
{
    private const string Bench = "Bench Press";
    private const string Squat = "Back Squat";

    private readonly TestApp _app = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task AddAsync_NewExercise_AssignsIdAndStoresEveryField()
    {
        var exercise = new Exercise { Name = Bench, TargetMuscleGroup = MuscleGroup.Chest, Equipment = EquipmentType.Barbell };

        await _app.Exercises.AddAsync(exercise);

        var stored = Assert.Single(await _app.Exercises.GetAllAsync());
        Assert.True(exercise.Id > 0);
        Assert.Equal((exercise.Id, Bench, MuscleGroup.Chest, EquipmentType.Barbell), (stored.Id, stored.Name, stored.TargetMuscleGroup, stored.Equipment));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsNothing() =>
        Assert.Empty(await _app.Exercises.GetAllAsync());

    [Fact]
    public async Task GetAllAsync_SeveralExercises_ReturnsEach()
    {
        await _app.Exercises.AddAsync(new Exercise { Name = Bench });
        await _app.Exercises.AddAsync(new Exercise { Name = Squat });

        Assert.Equal([Squat, Bench], (await _app.Exercises.GetAllAsync()).Select(exercise => exercise.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Update_InTransaction_ChangesTheRow()
    {
        var exercise = new Exercise { Name = Bench };
        await _app.Exercises.AddAsync(exercise);
        exercise.Name = Squat;
        exercise.TargetMuscleGroup = MuscleGroup.Legs;

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(transaction => _app.Exercises.Update(transaction, exercise));

        var stored = Assert.Single(await _app.Exercises.GetAllAsync());
        Assert.Equal((Squat, MuscleGroup.Legs), (stored.Name, stored.TargetMuscleGroup));
    }

    [Fact]
    public async Task Delete_InTransaction_RemovesOnlyThatExercise()
    {
        var bench = new Exercise { Name = Bench };
        await _app.Exercises.AddAsync(bench);
        await _app.Exercises.AddAsync(new Exercise { Name = Squat });

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(transaction => _app.Exercises.Delete(transaction, bench.Id));

        Assert.Equal(Squat, Assert.Single(await _app.Exercises.GetAllAsync()).Name);
    }

    [Fact]
    public async Task Clear_InTransaction_RemovesEveryExercise()
    {
        await _app.Exercises.AddAsync(new Exercise { Name = Bench });
        await _app.Exercises.AddAsync(new Exercise { Name = Squat });

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.Exercises.Clear);

        Assert.Empty(await _app.Exercises.GetAllAsync());
    }
}
