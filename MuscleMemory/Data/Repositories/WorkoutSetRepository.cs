using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSetRepository(DatabaseContext context) : IWorkoutSetRepository
{
    private const string DeleteForSessionExercise = "DELETE FROM WorkoutSet WHERE SessionExerciseId = ?";
    private const string SelectForSessionExercisesFormat = "SELECT * FROM WorkoutSet WHERE SessionExerciseId IN ({0})";
    private const string SelectLastSessionSets = """
        SELECT loggedSet.* FROM WorkoutSet loggedSet
        JOIN SessionExercise performed ON performed.Id = loggedSet.SessionExerciseId
        WHERE performed.ExerciseId = ? AND performed.WorkoutSessionId = (
            SELECT earlier.WorkoutSessionId FROM SessionExercise earlier
            JOIN WorkoutSet earlierSet ON earlierSet.SessionExerciseId = earlier.Id
            JOIN WorkoutSession session ON session.Id = earlier.WorkoutSessionId
            WHERE earlier.ExerciseId = ? AND earlier.WorkoutSessionId <> ?
            ORDER BY session.StartTimeUtc DESC
            LIMIT 1)
        ORDER BY loggedSet.SetNumber
        """;

    public async Task AddAsync(WorkoutSet set)
    {
        var connection = await context.GetConnectionAsync();
        await connection.InsertAsync(set);
    }

    public async Task UpdateAsync(WorkoutSet set)
    {
        var connection = await context.GetConnectionAsync();
        await connection.UpdateAsync(set);
    }

    public async Task DeleteAsync(int setId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAsync<WorkoutSet>(setId);
    }

    public async Task DeleteForSessionExerciseAsync(int sessionExerciseId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(DeleteForSessionExercise, sessionExerciseId);
    }

    public async Task<List<WorkoutSet>> GetForSessionExerciseAsync(int sessionExerciseId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSet>()
                               .Where(set => set.SessionExerciseId == sessionExerciseId)
                               .OrderBy(set => set.SetNumber)
                               .ToListAsync();
    }

    public async Task<List<WorkoutSet>> GetForSessionExercisesAsync(IReadOnlyCollection<int> sessionExerciseIds)
    {
        if (sessionExerciseIds.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(SelectForSessionExercisesFormat, SqlPlaceholders.For(sessionExerciseIds.Count));

        return await connection.QueryAsync<WorkoutSet>(query, [.. sessionExerciseIds.Select(id => (object)id)]);
    }

    public async Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.QueryAsync<WorkoutSet>(SelectLastSessionSets, exerciseId, exerciseId, currentSessionId);
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutSet>();
    }
}
