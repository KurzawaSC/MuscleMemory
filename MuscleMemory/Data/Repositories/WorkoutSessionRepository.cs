using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSessionRepository(DatabaseContext context) : IWorkoutSessionRepository
{
    private const string SelectSessionsByIdsFormat = "SELECT * FROM WorkoutSession WHERE Id IN ({0})";

    public async Task<int> CreateAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        var session = new WorkoutSession
        {
            WorkoutId = workoutId,
            StartTime = DateTime.UtcNow
        };

        await connection.InsertAsync(session);
        return session.Id;
    }

    public async Task FinishAsync(int sessionId)
    {
        var connection = await context.GetConnectionAsync();
        var session = await connection.Table<WorkoutSession>()
                                      .Where(candidate => candidate.Id == sessionId)
                                      .FirstOrDefaultAsync();

        if (session is null)
        {
            return;
        }

        session.EndTime = DateTime.UtcNow;
        await connection.UpdateAsync(session);
    }

    public async Task<List<WorkoutSession>> GetByIdsAsync(IReadOnlyCollection<int> sessionIds)
    {
        if (sessionIds.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(SelectSessionsByIdsFormat, SqlPlaceholders.For(sessionIds.Count));

        return await connection.QueryAsync<WorkoutSession>(query, [.. sessionIds.Select(id => (object)id)]);
    }

    public async Task<List<WorkoutSession>> GetForWorkoutAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutSession>()
                               .Where(session => session.WorkoutId == workoutId)
                               .OrderByDescending(session => session.StartTime)
                               .ToListAsync();
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutSession>();
    }
}
