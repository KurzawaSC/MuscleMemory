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
        var state = await connection.Table<ActiveWorkoutState>().FirstOrDefaultAsync();

        if (state is null)
        {
            return null;
        }

        state.StartTimeUtc = StoredDateTime.AsUtc(state.StartTimeUtc);
        state.BreakEndTimeUtc = StoredDateTime.AsUtc(state.BreakEndTimeUtc);

        return state;
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<ActiveWorkoutState>();
    }
}
