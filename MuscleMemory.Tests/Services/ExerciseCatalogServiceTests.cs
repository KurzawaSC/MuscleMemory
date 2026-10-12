using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using NSubstitute;
using SQLite;

namespace MuscleMemory.Tests.Services;

public sealed class ExerciseCatalogServiceTests : IAsyncLifetime
{
    private const string Bench = "Bench Press";
    private const string Renamed = "Paused Bench Press";
    private const string Squat = "Back Squat";

    private readonly TestApp _app = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetAllAsync_EmptyCatalog_ReturnsNothing() =>
        Assert.Empty(await _app.Catalog.GetAllAsync());

    [Fact]
    public async Task GetAllAsync_SeveralExercises_ReturnsEachWithItsDetails()
    {
        var bench = await _app.Data.ExerciseAsync(Bench, MuscleGroup.Chest, EquipmentType.Barbell);
        var squat = await _app.Data.ExerciseAsync(Squat, MuscleGroup.Legs, EquipmentType.Barbell);

        var exercises = await _app.Catalog.GetAllAsync();

        Assert.Equal(
            [(bench.Id, Bench, MuscleGroup.Chest), (squat.Id, Squat, MuscleGroup.Legs)],
            exercises.Select(exercise => (exercise.Id, exercise.Name, exercise.TargetMuscleGroup)).OrderBy(exercise => exercise.Id));
    }

    [Fact]
    public async Task CountWorkoutsContainingAsync_InNoWorkout_ReturnsZero()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        await _app.Data.WorkoutAsync("Leg Day", await _app.Data.ExerciseAsync(Squat));

        Assert.Equal(0, await _app.Catalog.CountWorkoutsContainingAsync(bench.Id));
    }

    [Fact]
    public async Task CountWorkoutsContainingAsync_TwiceInOneWorkoutAndOnceInAnother_CountsEachWorkoutOnce()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        await _app.Data.WorkoutAsync("Push Day", bench, bench);
        await _app.Data.WorkoutAsync("Upper Day", bench);

        Assert.Equal(2, await _app.Catalog.CountWorkoutsContainingAsync(bench.Id));
    }

    [Fact]
    public async Task AddAsync_NewExercise_StoresItAndNotifiesExercisesOnce()
    {
        var exercise = new Exercise { Name = Bench, TargetMuscleGroup = MuscleGroup.Chest };

        await _app.Catalog.AddAsync(exercise);

        Assert.Equal(exercise.Id, Assert.Single(await _app.Exercises.GetAllAsync()).Id);
        _app.DataChanges.Received(1).Notify(DataArea.Exercises);
        _app.DataChanges.ReceivedWithAnyArgs(1).Notify(default);
    }

    [Fact]
    public async Task AddAsync_InsertFails_DoesNotNotify()
    {
        await _app.Database.FailOnAsync(nameof(Exercise), TableOperation.Insert);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Catalog.AddAsync(new Exercise { Name = Bench }));

        _app.DataChanges.DidNotReceiveWithAnyArgs().Notify(default);
    }

    [Fact]
    public async Task UpdateAsync_Renamed_RenamesTemplatesAndNotifiesEveryAreaOnce()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        var push = await _app.Data.WorkoutAsync("Push Day", bench);
        _app.DataChanges.ClearReceivedCalls();
        bench.Name = Renamed;

        await _app.Catalog.UpdateAsync(bench);

        Assert.Equal(Renamed, Assert.Single(await _app.Exercises.GetAllAsync()).Name);
        Assert.Equal(Renamed, Assert.Single(await _app.Workouts.GetExercisesAsync(push.Id)).ExerciseName);
        _app.DataChanges.Received(1).Notify(DataArea.All);
        _app.DataChanges.ReceivedWithAnyArgs(1).Notify(default);
    }

    [Fact]
    public async Task UpdateAsync_TemplateRenameFails_KeepsTheOldNameAndDoesNotNotify()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        var push = await _app.Data.WorkoutAsync("Push Day", bench);
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Update);
        _app.DataChanges.ClearReceivedCalls();
        bench.Name = Renamed;

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Catalog.UpdateAsync(bench));

        Assert.Equal(Bench, Assert.Single(await _app.Exercises.GetAllAsync()).Name);
        Assert.Equal(Bench, Assert.Single(await _app.Workouts.GetExercisesAsync(push.Id)).ExerciseName);
        _app.DataChanges.DidNotReceiveWithAnyArgs().Notify(default);
    }

    [Fact]
    public async Task DeleteAsync_ExerciseInTemplates_RemovesItEverywhereAndNotifiesEveryAreaOnce()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        var press = await _app.Data.ExerciseAsync("Overhead Press");
        var push = await _app.Data.WorkoutAsync("Push Day", bench, press);
        _app.DataChanges.ClearReceivedCalls();

        await _app.Catalog.DeleteAsync(bench.Id);

        Assert.Equal(press.Id, Assert.Single(await _app.Exercises.GetAllAsync()).Id);
        Assert.Equal(press.Id, Assert.Single(await _app.Workouts.GetExercisesAsync(push.Id)).ExerciseId);
        _app.DataChanges.Received(1).Notify(DataArea.All);
        _app.DataChanges.ReceivedWithAnyArgs(1).Notify(default);
    }

    [Fact]
    public async Task DeleteAsync_ExerciseDeleteFails_KeepsTemplatesAndDoesNotNotify()
    {
        var bench = await _app.Data.ExerciseAsync(Bench);
        var push = await _app.Data.WorkoutAsync("Push Day", bench);
        await _app.Database.FailOnAsync(nameof(Exercise), TableOperation.Delete);
        _app.DataChanges.ClearReceivedCalls();

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Catalog.DeleteAsync(bench.Id));

        Assert.Single(await _app.Exercises.GetAllAsync());
        Assert.Single(await _app.Workouts.GetExercisesAsync(push.Id));
        _app.DataChanges.DidNotReceiveWithAnyArgs().Notify(default);
    }
}
