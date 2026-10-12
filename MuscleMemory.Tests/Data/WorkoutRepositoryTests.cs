using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using SQLite;

namespace MuscleMemory.Tests.Data;

public sealed class WorkoutRepositoryTests : IAsyncLifetime
{
    private const string PushDay = "Push Day";
    private const string LegDay = "Leg Day";
    private const string Renamed = "Renamed";
    private const int MissingId = 999;

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
    public async Task SaveWithExercisesAsync_NewWorkout_StoresExercisesInGivenOrder()
    {
        var workout = new Workout { Name = PushDay };

        var id = await _app.Workouts.SaveWithExercisesAsync(workout, [Plan(_press), Plan(_bench)]);

        var exercises = await _app.Workouts.GetExercisesAsync(id);
        Assert.Equal(workout.Id, id);
        Assert.Equal([_press.Id, _bench.Id], exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal([0, 1], exercises.Select(exercise => exercise.Order));
        Assert.All(exercises, exercise => Assert.Equal(id, exercise.WorkoutId));
    }

    [Fact]
    public async Task SaveWithExercisesAsync_NewWorkout_StoresThePlan()
    {
        var workout = new Workout { Name = PushDay };

        await _app.Workouts.SaveWithExercisesAsync(workout, [Plan(_bench)]);

        var stored = Assert.Single(await _app.Workouts.GetExercisesAsync(workout.Id));
        Assert.Equal(
            (_bench.Name, TestDataBuilder.PlannedSets, TestDataBuilder.PlannedReps, TestDataBuilder.BreakSeconds, TestDataBuilder.TargetRpe),
            (stored.ExerciseName, stored.Sets, stored.Reps, stored.BreakTimeInSeconds, stored.TargetRPE));
    }

    [Fact]
    public async Task SaveWithExercisesAsync_DeletedExercise_SkipsItAndKeepsOrderGapless()
    {
        var workout = new Workout { Name = PushDay };
        var missing = Plan(_bench);
        missing.ExerciseId = MissingId;

        await _app.Workouts.SaveWithExercisesAsync(workout, [Plan(_press), missing, Plan(_squat)]);

        var exercises = await _app.Workouts.GetExercisesAsync(workout.Id);
        Assert.Equal([_press.Id, _squat.Id], exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal([0, 1], exercises.Select(exercise => exercise.Order));
    }

    [Fact]
    public async Task SaveWithExercisesAsync_NoExercises_StoresWorkoutOnly()
    {
        var workout = new Workout { Name = PushDay };

        await _app.Workouts.SaveWithExercisesAsync(workout, []);

        Assert.Equal(PushDay, Assert.Single(await _app.Workouts.GetAllAsync()).Name);
        Assert.Empty(await _app.Workouts.GetExercisesAsync(workout.Id));
    }

    [Fact]
    public async Task SaveWithExercisesAsync_ExerciseInsertFails_LeavesNoWorkout()
    {
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Insert);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Workouts.SaveWithExercisesAsync(new Workout { Name = PushDay }, [Plan(_bench)]));

        Assert.Empty(await _app.Workouts.GetAllAsync());
        Assert.Empty(await _app.Workouts.GetAllExercisesAsync());
    }

    [Fact]
    public async Task UpdateWithExercisesAsync_ExistingWorkout_RenamesAndReplacesExercises()
    {
        var workout = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        workout.Name = Renamed;

        await _app.Workouts.UpdateWithExercisesAsync(workout, [Plan(_squat), Plan(_bench)]);

        Assert.Equal(Renamed, Assert.Single(await _app.Workouts.GetAllAsync()).Name);
        Assert.Equal([_squat.Id, _bench.Id], (await _app.Workouts.GetExercisesAsync(workout.Id)).Select(exercise => exercise.ExerciseId));
        Assert.Equal(2, await _app.Database.CountAsync(nameof(WorkoutExercise)));
    }

    [Fact]
    public async Task UpdateWithExercisesAsync_MissingWorkout_ThrowsAndWritesNothing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _app.Workouts.UpdateWithExercisesAsync(new Workout { Id = MissingId, Name = PushDay }, [Plan(_bench)]));

        Assert.Empty(await _app.Workouts.GetAllAsync());
        Assert.Empty(await _app.Workouts.GetAllExercisesAsync());
    }

    [Fact]
    public async Task UpdateWithExercisesAsync_ExerciseInsertFails_KeepsPreviousVersion()
    {
        var workout = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Insert);

        await Assert.ThrowsAsync<SQLiteException>(() =>
            _app.Workouts.UpdateWithExercisesAsync(new Workout { Id = workout.Id, Name = Renamed }, [Plan(_squat)]));

        Assert.Equal(PushDay, Assert.Single(await _app.Workouts.GetAllAsync()).Name);
        Assert.Equal([_bench.Id, _press.Id], (await _app.Workouts.GetExercisesAsync(workout.Id)).Select(exercise => exercise.ExerciseId));
    }

    [Fact]
    public async Task DeleteAsync_Workout_RemovesItAndOnlyItsExercises()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        var legs = await _app.Data.WorkoutAsync(LegDay, _squat);

        await _app.Workouts.DeleteAsync(push.Id);

        Assert.Equal(LegDay, Assert.Single(await _app.Workouts.GetAllAsync()).Name);
        Assert.Equal(legs.Id, Assert.Single(await _app.Workouts.GetAllExercisesAsync()).WorkoutId);
    }

    [Fact]
    public async Task DeleteAsync_WorkoutWithHistory_KeepsLoggedSessions()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        var sessionId = await _app.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        await _app.Workouts.DeleteAsync(push.Id);

        Assert.Equal(sessionId, Assert.Single(await _app.Sessions.GetCompletedForWorkoutAsync(push.Id)).Id);
        Assert.Equal(1, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task DeleteAsync_ExerciseDeleteFails_KeepsTheWorkout()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);
        await _app.Database.FailOnAsync(nameof(WorkoutExercise), TableOperation.Delete);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Workouts.DeleteAsync(push.Id));

        Assert.Single(await _app.Workouts.GetAllAsync());
        Assert.Single(await _app.Workouts.GetExercisesAsync(push.Id));
    }

    [Fact]
    public async Task GetExercisesAsync_TwoWorkouts_ReturnsOnlyThatWorkoutInOrder()
    {
        var push = await _app.Data.WorkoutAsync(PushDay, _press, _bench);
        await _app.Data.WorkoutAsync(LegDay, _squat);

        Assert.Equal([_press.Id, _bench.Id], (await _app.Workouts.GetExercisesAsync(push.Id)).Select(exercise => exercise.ExerciseId));
    }

    [Fact]
    public async Task GetAllExercisesAsync_TwoWorkouts_OrdersByWorkoutThenPosition()
    {
        var legs = await _app.Data.WorkoutAsync(LegDay, _squat, _press);
        var push = await _app.Data.WorkoutAsync(PushDay, _bench);

        var exercises = await _app.Workouts.GetAllExercisesAsync();

        Assert.Equal(
            [(legs.Id, _squat.Id), (legs.Id, _press.Id), (push.Id, _bench.Id)],
            exercises.Select(exercise => (exercise.WorkoutId, exercise.ExerciseId)));
    }

    [Fact]
    public async Task CountWorkoutsContainingAsync_ExerciseTwiceInOneWorkout_CountsWorkoutsOnce()
    {
        await _app.Data.WorkoutAsync(PushDay, _bench, _press, _bench);
        await _app.Data.WorkoutAsync(LegDay, _squat, _bench);

        Assert.Equal(2, await _app.Workouts.CountWorkoutsContainingAsync(_bench.Id));
        Assert.Equal(1, await _app.Workouts.CountWorkoutsContainingAsync(_squat.Id));
        Assert.Equal(0, await _app.Workouts.CountWorkoutsContainingAsync(MissingId));
    }

    [Fact]
    public async Task RenameExercise_InTransaction_RenamesEveryTemplateRow()
    {
        await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        await _app.Data.WorkoutAsync(LegDay, _bench);

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(transaction => _app.Workouts.RenameExercise(transaction, _bench.Id, Renamed));

        var exercises = await _app.Workouts.GetAllExercisesAsync();
        Assert.All(exercises.Where(exercise => exercise.ExerciseId == _bench.Id), exercise => Assert.Equal(Renamed, exercise.ExerciseName));
        Assert.Equal(_press.Name, Assert.Single(exercises, exercise => exercise.ExerciseId == _press.Id).ExerciseName);
    }

    [Fact]
    public async Task RemoveExercise_InTransaction_RemovesItFromEveryTemplate()
    {
        await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        await _app.Data.WorkoutAsync(LegDay, _bench);

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(transaction => _app.Workouts.RemoveExercise(transaction, _bench.Id));

        Assert.Equal(_press.Id, Assert.Single(await _app.Workouts.GetAllExercisesAsync()).ExerciseId);
    }

    [Fact]
    public async Task Clear_InTransaction_RemovesWorkoutsAndTemplates()
    {
        await _app.Data.WorkoutAsync(PushDay, _bench);

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.Workouts.Clear);

        Assert.Empty(await _app.Workouts.GetAllAsync());
        Assert.Empty(await _app.Workouts.GetAllExercisesAsync());
    }

    private static WorkoutExercise Plan(Exercise exercise) => TestDataBuilder.Plan(exercise);
}
