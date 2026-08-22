using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSessionRepository(DatabaseContext context) : IWorkoutSessionRepository
{
    private const string SelectCompletedSessionsByIdsFormat =
        "SELECT * FROM WorkoutSession WHERE EndTimeUtc IS NOT NULL AND Id IN ({0})";

    public async Task<WorkoutSession> CreateAsync(Workout workout)
    {
        var connection = await context.GetConnectionAsync();
        var session = new WorkoutSession
        {
            WorkoutId = workout.Id,
            WorkoutName = workout.Name,
            StartTimeUtc = DateTime.UtcNow
        };

        await connection.InsertAsync(session);
        return session;
    }

    public async Task FinishAsync(int sessionId)
    {
        var connection = await context.GetConnectionAsync();
        var session = await GetAsync(sessionId);

        if (session is null)
        {
            return;
        }

        session.EndTimeUtc = DateTime.UtcNow;
        await connection.UpdateAsync(session);
    }

    public async Task<WorkoutSession?> GetAsync(int sessionId)
    {
        var connection = await context.GetConnectionAsync();
        var session = await connection.Table<WorkoutSession>()
                                      .Where(candidate => candidate.Id == sessionId)
                                      .FirstOrDefaultAsync();

        return session is null ? null : AsUtc(session);
    }

    public async Task<List<WorkoutSession>> GetCompletedByIdsAsync(IReadOnlyCollection<int> sessionIds)
    {
        if (sessionIds.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(SelectCompletedSessionsByIdsFormat, SqlPlaceholders.For(sessionIds.Count));
        var sessions = await connection.QueryAsync<WorkoutSession>(query, [.. sessionIds.Select(id => (object)id)]);

        return [.. sessions.Select(AsUtc)];
    }

    public async Task<List<WorkoutSession>> GetCompletedForWorkoutAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        var sessions = await connection.Table<WorkoutSession>()
                                       .Where(session => session.WorkoutId == workoutId && session.EndTimeUtc != null)
                                       .OrderByDescending(session => session.StartTimeUtc)
                                       .ToListAsync();

        return [.. sessions.Select(AsUtc)];
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutSession>();
    }

    private static WorkoutSession AsUtc(WorkoutSession session)
    {
        session.StartTimeUtc = StoredDateTime.AsUtc(session.StartTimeUtc);
        session.EndTimeUtc = StoredDateTime.AsUtc(session.EndTimeUtc);

        return session;
    }
}
