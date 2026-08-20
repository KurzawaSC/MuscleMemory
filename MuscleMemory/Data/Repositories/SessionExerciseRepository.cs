using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class SessionExerciseRepository(DatabaseContext context) : ISessionExerciseRepository
{
    private const string SelectForSessionsFormat = "SELECT * FROM SessionExercise WHERE WorkoutSessionId IN ({0}) ORDER BY [Order]";
    private const string SelectNextOrder = "SELECT IFNULL(MAX([Order]), -1) + 1 FROM SessionExercise WHERE WorkoutSessionId = ?";

    public async Task<List<SessionExercise>> CreateSnapshotAsync(int workoutSessionId, IReadOnlyList<WorkoutExercise> templateExercises)
    {
        var snapshot = BuildSnapshot(workoutSessionId, templateExercises);

        if (snapshot.Count == 0)
        {
            return snapshot;
        }

        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            foreach (var sessionExercise in snapshot)
            {
                transaction.Insert(sessionExercise);
            }
        });

        return snapshot;
    }

    public async Task<List<SessionExercise>> GetForSessionAsync(int workoutSessionId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<SessionExercise>()
                               .Where(sessionExercise => sessionExercise.WorkoutSessionId == workoutSessionId)
                               .OrderBy(sessionExercise => sessionExercise.Order)
                               .ToListAsync();
    }

    public async Task<List<SessionExercise>> GetForSessionsAsync(IReadOnlyCollection<int> workoutSessionIds)
    {
        if (workoutSessionIds.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(SelectForSessionsFormat, SqlPlaceholders.For(workoutSessionIds.Count));

        return await connection.QueryAsync<SessionExercise>(query, [.. workoutSessionIds.Select(id => (object)id)]);
    }

    public async Task<List<SessionExercise>> GetForExerciseAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<SessionExercise>()
                               .Where(sessionExercise => sessionExercise.ExerciseId == exerciseId)
                               .ToListAsync();
    }

    public async Task<SessionExercise> AppendToSessionAsync(SessionExercise sessionExercise)
    {
        var connection = await context.GetConnectionAsync();
        sessionExercise.Order = await connection.ExecuteScalarAsync<int>(SelectNextOrder, sessionExercise.WorkoutSessionId);
        await connection.InsertAsync(sessionExercise);

        return sessionExercise;
    }

    public async Task DeleteAsync(int sessionExerciseId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAsync<SessionExercise>(sessionExerciseId);
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<SessionExercise>();
    }

    private static List<SessionExercise> BuildSnapshot(int workoutSessionId, IReadOnlyList<WorkoutExercise> templateExercises) =>
    [
        .. templateExercises.Select((templateExercise, position) => new SessionExercise
        {
            WorkoutSessionId = workoutSessionId,
            ExerciseId = templateExercise.ExerciseId,
            ExerciseName = templateExercise.ExerciseName,
            Order = position,
            PlannedSets = templateExercise.Sets,
            PlannedReps = templateExercise.Reps,
            BreakTimeInSeconds = templateExercise.BreakTimeInSeconds,
            TargetRPE = templateExercise.TargetRPE
        })
    ];
}
