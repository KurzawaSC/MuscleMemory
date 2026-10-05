using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSessionRepository(DatabaseContext context) : IWorkoutSessionRepository
{
    private const string SelectCompletedSessionsByIdsFormat =
        "SELECT * FROM WorkoutSession WHERE EndTimeUtc IS NOT NULL AND Id IN ({0})";
    private const string FinishSession = "UPDATE WorkoutSession SET EndTimeUtc = ? WHERE Id = ?";
    private const string DeleteSessionExercises = "DELETE FROM SessionExercise WHERE WorkoutSessionId = ?";
    private const string CountLoggedSets = """
        SELECT COUNT(*) FROM WorkoutSet loggedSet
        JOIN SessionExercise performed ON performed.Id = loggedSet.SessionExerciseId
        WHERE performed.WorkoutSessionId = ?
        """;

    public async Task<StartedSession> CreateWithSnapshotAsync(Workout workout, IReadOnlyList<WorkoutExercise> templateExercises)
    {
        var connection = await context.GetConnectionAsync();
        var session = new WorkoutSession
        {
            WorkoutId = workout.Id,
            WorkoutName = workout.Name,
            StartTimeUtc = DateTime.UtcNow
        };
        var snapshot = BuildSnapshot(templateExercises);

        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Insert(session);
            foreach (var sessionExercise in snapshot)
            {
                sessionExercise.WorkoutSessionId = session.Id;
                transaction.Insert(sessionExercise);
            }
        });

        return new StartedSession(session.Id, snapshot);
    }

    public async Task FinishOrDiscardAsync(int sessionId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            if (transaction.ExecuteScalar<int>(CountLoggedSets, sessionId) > 0)
            {
                transaction.Execute(FinishSession, DateTime.UtcNow, sessionId);
                return;
            }

            transaction.Execute(DeleteSessionExercises, sessionId);
            transaction.Delete<WorkoutSession>(sessionId);
        });
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

    public void Clear(SQLiteConnection transaction) => transaction.DeleteAll<WorkoutSession>();

    private static List<SessionExercise> BuildSnapshot(IReadOnlyList<WorkoutExercise> templateExercises) =>
    [
        .. templateExercises.Select((templateExercise, position) => new SessionExercise
        {
            ExerciseId = templateExercise.ExerciseId,
            ExerciseName = templateExercise.ExerciseName,
            Order = position,
            PlannedSets = templateExercise.Sets,
            PlannedReps = templateExercise.Reps,
            BreakTimeInSeconds = templateExercise.BreakTimeInSeconds,
            TargetRPE = templateExercise.TargetRPE
        })
    ];

    private static WorkoutSession AsUtc(WorkoutSession session)
    {
        session.StartTimeUtc = StoredDateTime.AsUtc(session.StartTimeUtc);
        session.EndTimeUtc = StoredDateTime.AsUtc(session.EndTimeUtc);

        return session;
    }
}
