using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class SessionExerciseRepository(DatabaseContext context) : ISessionExerciseRepository
{
    private const string SelectForSessionsFormat = "SELECT * FROM SessionExercise WHERE WorkoutSessionId IN ({0}) ORDER BY [Order]";
    private const string SelectNextOrder = "SELECT IFNULL(MAX([Order]), -1) + 1 FROM SessionExercise WHERE WorkoutSessionId = ?";
    private const string DeleteSetsForSessionExercise = "DELETE FROM WorkoutSet WHERE SessionExerciseId = ?";

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

    public async Task AppendToSessionAsync(SessionExercise sessionExercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            sessionExercise.Order = transaction.ExecuteScalar<int>(SelectNextOrder, sessionExercise.WorkoutSessionId);
            transaction.Insert(sessionExercise);
        });
    }

    public async Task DeleteAsync(int sessionExerciseId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Execute(DeleteSetsForSessionExercise, sessionExerciseId);
            transaction.Delete<SessionExercise>(sessionExerciseId);
        });
    }

    public void Clear(SQLiteConnection transaction) => transaction.DeleteAll<SessionExercise>();
}
