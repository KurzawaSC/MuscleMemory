using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSetRepository(DatabaseContext context) : IWorkoutSetRepository
{
    private const string DeleteSetsForLoggedExercise = "DELETE FROM WorkoutSet WHERE WorkoutExerciseId = ? AND WorkoutSessionId = ?";
    private const string SelectSetsForWorkoutExercisesFormat = "SELECT * FROM WorkoutSet WHERE WorkoutExerciseId IN ({0})";
    private const string SelectSetsForSessionsFormat = "SELECT * FROM WorkoutSet WHERE WorkoutSessionId IN ({0})";

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

    public async Task DeleteForLoggedExerciseAsync(int workoutExerciseId, int workoutSessionId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(DeleteSetsForLoggedExercise, workoutExerciseId, workoutSessionId);
    }

    public async Task<List<WorkoutSet>> GetForWorkoutExerciseAsync(int workoutExerciseId, int workoutSessionId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSet>()
                               .Where(set => set.WorkoutExerciseId == workoutExerciseId && set.WorkoutSessionId == workoutSessionId)
                               .ToListAsync();
    }

    public Task<List<WorkoutSet>> GetForWorkoutExercisesAsync(IReadOnlyCollection<int> workoutExerciseIds) =>
        QueryByIdsAsync(SelectSetsForWorkoutExercisesFormat, workoutExerciseIds);

    public Task<List<WorkoutSet>> GetForSessionsAsync(IReadOnlyCollection<int> workoutSessionIds) =>
        QueryByIdsAsync(SelectSetsForSessionsFormat, workoutSessionIds);

    public async Task<List<WorkoutSet>> GetLastSessionSetsAsync(int workoutExerciseId, int currentSessionId)
    {
        var connection = await context.GetConnectionAsync();
        var lastSet = await connection.Table<WorkoutSet>()
                                      .Where(set => set.WorkoutExerciseId == workoutExerciseId && set.WorkoutSessionId < currentSessionId)
                                      .OrderByDescending(set => set.WorkoutSessionId)
                                      .FirstOrDefaultAsync();

        if (lastSet is null)
        {
            return [];
        }

        return await connection.Table<WorkoutSet>()
                               .Where(set => set.WorkoutExerciseId == workoutExerciseId && set.WorkoutSessionId == lastSet.WorkoutSessionId)
                               .OrderBy(set => set.SetNumber)
                               .ToListAsync();
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutSet>();
    }

    private async Task<List<WorkoutSet>> QueryByIdsAsync(string queryFormat, IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(queryFormat, SqlPlaceholders.For(ids.Count));

        return await connection.QueryAsync<WorkoutSet>(query, [.. ids.Select(id => (object)id)]);
    }
}
