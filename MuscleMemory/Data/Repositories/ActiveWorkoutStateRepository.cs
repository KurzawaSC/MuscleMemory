using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class ActiveWorkoutStateRepository(DatabaseContext context) : IActiveWorkoutStateRepository
{
    public async Task SaveAsync(ActiveWorkoutState state)
    {
        var connection = await context.GetConnectionAsync();
        state.Id = DomainDefaults.ActiveWorkoutStateId;
        await connection.InsertOrReplaceAsync(state);
    }

    public async Task<ActiveWorkoutState?> GetAsync()
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<ActiveWorkoutState>().FirstOrDefaultAsync();
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<ActiveWorkoutState>();
    }
}
