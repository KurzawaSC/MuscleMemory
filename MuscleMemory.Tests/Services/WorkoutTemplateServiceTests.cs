using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using SQLite;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutTemplateServiceTests : IAsyncLifetime
{
    private const string PushDay = "Push Day";
    private const string LegDay = "Leg Day";

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Exercise _squat = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _squat = await _app.Data.ExerciseAsync("Back Squat");
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetAllAsync_NoWorkouts_ReturnsNothing() =>
        Assert.Empty(await _app.Templates.GetAllAsync());

    [Fact]
    public async Task GetAllAsync_TwoWorkouts_ReturnsBoth()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        var legs = await _app.Data.WorkoutAsync(LegDay, _squat);

        var workouts = await _app.Templates.GetAllAsync();

        Assert.Equal([(push.Id, PushDay), (legs.Id, LegDay)], workouts.Select(workout => (workout.Id, workout.Name)).OrderBy(workout => workout.Id));
    }

    [Fact]
    public async Task GetAllExercisesAsync_TwoWorkouts_ReturnsEveryExerciseByWorkoutThenOrder()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _press, _bench);
        var legs = await _app.Data.WorkoutAsync(LegDay, _squat);

        var exercises = await _app.Templates.GetAllExercisesAsync();

        Assert.Equal(
            [(push.Id, _press.Id, 0), (push.Id, _bench.Id, 1), (legs.Id, _squat.Id, 0)],
            exercises.Select(exercise => (exercise.WorkoutId, exercise.ExerciseId, exercise.Order)));
    }

    [Fact]
    public async Task GetExercisesAsync_TwoWorkouts_ReturnsOnlyThatWorkoutInOrder()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _press, _bench);
        await _app.Data.WorkoutAsync(LegDay, _squat);

        Assert.Equal([_press.Id, _bench.Id], (await _app.Templates.GetExercisesAsync(push.Id)).Select(exercise => exercise.ExerciseId));
    }

    [Fact]
    public async Task SaveWithExercisesAsync_NewWorkout_AssignsItsIdAndStoresTheExercisesInOrder()
    {
        var workout = new Workout { Name = PushDay };

        await _app.Templates.SaveWithExercisesAsync(workout, [TestDataBuilder.Plan(_press), TestDataBuilder.Plan(_bench)]);

        Assert.Equal(PushDay, Assert.Single(await _app.Templates.GetAllAsync(), stored => stored.Id == workout.Id).Name);
        Assert.Equal(
            [(_press.Id, 0), (_bench.Id, 1)],
            (await _app.Templates.GetExercisesAsync(workout.Id)).Select(exercise => (exercise.ExerciseId, exercise.Order)));
    }

    [Fact]
    public async Task UpdateWithExercisesAsync_RenamedAndReplaced_StoresTheNewNameAndExercises()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench, _press);

        await _app.Templates.UpdateWithExercisesAsync(new Workout { Id = push.Id, Name = LegDay }, [TestDataBuilder.Plan(_squat)]);

        Assert.Equal(LegDay, Assert.Single(await _app.Templates.GetAllAsync()).Name);
        Assert.Equal(_squat.Id, Assert.Single(await _app.Templates.GetExercisesAsync(push.Id)).ExerciseId);
    }

    [Fact]
    public async Task UpdateWithExercisesAsync_ExerciseInsertFails_KeepsTheOldNameAndExercises()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Insert);

        await Assert.ThrowsAsync<SQLiteException>(() =>
            _app.Templates.UpdateWithExercisesAsync(new Workout { Id = push.Id, Name = LegDay }, [TestDataBuilder.Plan(_squat)]));

        Assert.Equal(PushDay, Assert.Single(await _app.Templates.GetAllAsync()).Name);
        Assert.Equal(_bench.Id, Assert.Single(await _app.Templates.GetExercisesAsync(push.Id)).ExerciseId);
    }

    [Fact]
    public async Task DeleteAsync_Workout_RemovesItAndOnlyItsExercises()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        var legs = await _app.Data.WorkoutAsync(LegDay, _squat);

        await _app.Templates.DeleteAsync(push.Id);

        Assert.Equal(legs.Id, Assert.Single(await _app.Templates.GetAllAsync()).Id);
        Assert.Equal(legs.Id, Assert.Single(await _app.Templates.GetAllExercisesAsync()).WorkoutId);
    }

    [Fact]
    public async Task DeleteAsync_WorkoutWithHistory_KeepsTheLoggedSession()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        var sessionId = await _app.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        await _app.Templates.DeleteAsync(push.Id);

        var session = Assert.Single(await _app.History.GetWorkoutHistoryAsync(push.Id));
        Assert.Equal(sessionId, session.SessionId);
        Assert.Single(Assert.Single(session.Exercises).Sets);
    }

    [Fact]
    public async Task DeleteAsync_ExerciseDeleteFails_KeepsTheWorkoutAndItsExercises()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Delete);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Templates.DeleteAsync(push.Id));

        Assert.Equal(push.Id, Assert.Single(await _app.Templates.GetAllAsync()).Id);
        Assert.Single(await _app.Templates.GetAllExercisesAsync());
    }
}
