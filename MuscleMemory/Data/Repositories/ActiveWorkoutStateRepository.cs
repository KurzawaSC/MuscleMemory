using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Threading;
using SQLite;

namespace MuscleMemory.Data.Repositories;

public sealed class ActiveWorkoutStateRepository(DatabaseContext context) : IActiveWorkoutStateRepository
{
    private const string CountOpenSession = "SELECT COUNT(*) FROM WorkoutSession WHERE Id = ? AND EndTimeUtc IS NULL";

    private readonly SequentialTaskQueue _writes = new();

    public Task SaveAsync(ActiveWorkoutState state) => _writes.EnqueueAsync(() => SaveForOpenSessionAsync(state));

    private async Task SaveForOpenSessionAsync(ActiveWorkoutState state)
    {
        var connection = await context.GetConnectionAsync();
        state.Id = DomainDefaults.ActiveWorkoutStateId;
        await connection.RunInTransactionAsync(transaction =>
        {
            if (transaction.ExecuteScalar<int>(CountOpenSession, state.SessionId) > 0)
            {
                transaction.InsertOrReplace(state);
            }
        });
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

    public Task ClearAsync() => _writes.EnqueueAsync(DeleteAllAsync);

    private async Task DeleteAllAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<ActiveWorkoutState>();
    }

    public void Clear(SQLiteConnection transaction) => transaction.DeleteAll<ActiveWorkoutState>();
}
