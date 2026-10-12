using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Tests.Testing;

public sealed class TestDataBuilder(TestApp app)
{
    public const int PlannedSets = 3;
    public const int PlannedReps = 8;
    public const int BreakSeconds = 90;
    public const int TargetRpe = 7;

    private const string DeleteSessionSetsSql =
        "DELETE FROM WorkoutSet WHERE SessionExerciseId IN (SELECT Id FROM SessionExercise WHERE WorkoutSessionId = ?)";
    private const string DeleteSessionExercisesSql = "DELETE FROM SessionExercise WHERE WorkoutSessionId = ?";

    public static readonly TimeSpan SessionLength = TimeSpan.FromMinutes(45);

    public async Task<Exercise> ExerciseAsync(string name, MuscleGroup muscleGroup = MuscleGroup.Other, EquipmentType equipment = EquipmentType.Other)
    {
        var exercise = new Exercise { Name = name, TargetMuscleGroup = muscleGroup, Equipment = equipment };
        await app.Catalog.AddAsync(exercise);
        return exercise;
    }

    public async Task<Workout> WorkoutAsync(string name, params Exercise[] exercises)
    {
        var workout = new Workout { Name = name };
        await app.Workouts.SaveWithExercisesAsync(workout, [.. exercises.Select(Plan)]);
        return workout;
    }

    public Task<StartedSession> StartAsync(Workout workout) => app.ActiveSessions.StartAsync(workout);

    public async Task<WorkoutSet> LogSetAsync(SessionExercise performed, double weight, int reps)
    {
        var set = new WorkoutSet { SessionExerciseId = performed.Id, Weight = weight, Reps = reps };
        await app.ActiveSessions.AddSetAsync(set);
        return set;
    }

    public async Task<ActiveWorkoutState> SaveActiveStateAsync(StartedSession session, int exerciseIndex = 0)
    {
        var state = new ActiveWorkoutState
        {
            SessionId = session.SessionId,
            StartTimeUtc = app.Clock.GetUtcNow().UtcDateTime,
            CurrentExerciseIndex = exerciseIndex
        };
        await app.ActiveSessions.SaveStateAsync(state);
        return state;
    }

    public Task<int> CompletedSessionAsync(Workout workout, DateTime startUtc, params LoggedSet[] sets) =>
        CompletedSessionAsync(workout, startUtc, SessionLength, sets);

    public async Task<int> CompletedSessionAsync(Workout workout, DateTime startUtc, TimeSpan duration, params LoggedSet[] sets)
    {
        app.Clock.SetUtcNow(startUtc);
        var session = await StartAsync(workout);
        foreach (var set in sets)
        {
            await LogSetAsync(session.Exercises[set.ExerciseIndex], set.Weight, set.Reps);
        }

        app.Clock.Advance(duration);
        await app.ActiveSessions.FinishAsync(session.SessionId);
        return session.SessionId;
    }

    public async Task<int> EmptiedSessionFromBeforeTheFixAsync(Workout workout, DateTime startUtc, params LoggedSet[] sets)
    {
        var sessionId = await CompletedSessionAsync(workout, startUtc, sets);
        var connection = await app.Database.ConnectionAsync();
        await connection.ExecuteAsync(DeleteSessionSetsSql, sessionId);
        await connection.ExecuteAsync(DeleteSessionExercisesSql, sessionId);
        return sessionId;
    }

    public static WorkoutExercise Plan(Exercise exercise) => new()
    {
        ExerciseId = exercise.Id,
        ExerciseName = exercise.Name,
        Sets = PlannedSets,
        Reps = PlannedReps,
        BreakTimeInSeconds = BreakSeconds,
        TargetRPE = TargetRpe
    };
}
