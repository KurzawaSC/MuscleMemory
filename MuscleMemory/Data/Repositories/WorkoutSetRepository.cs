using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSetRepository(DatabaseContext context) : IWorkoutSetRepository
{
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
        var setsToDelete = await connection.Table<WorkoutSet>()
                                           .Where(set => set.WorkoutExerciseId == workoutExerciseId && set.WorkoutSessionId == workoutSessionId)
                                           .ToListAsync();

        foreach (var set in setsToDelete)
        {
            await connection.DeleteAsync<WorkoutSet>(set.Id);
        }
    }

    public async Task<List<WorkoutSet>> GetForWorkoutExerciseAsync(int workoutExerciseId, int workoutSessionId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSet>()
                               .Where(set => set.WorkoutExerciseId == workoutExerciseId && set.WorkoutSessionId == workoutSessionId)
                               .ToListAsync();
    }

    public async Task<List<WorkoutSet>> GetAllForWorkoutExerciseAsync(int workoutExerciseId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSet>()
                               .Where(set => set.WorkoutExerciseId == workoutExerciseId)
                               .ToListAsync();
    }

    public async Task<List<WorkoutSet>> GetForSessionAsync(int workoutSessionId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSet>()
                               .Where(set => set.WorkoutSessionId == workoutSessionId)
                               .ToListAsync();
    }

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
}
