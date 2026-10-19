using MuscleMemory.Models;
using MuscleMemory.Progression;
using SQLite;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutSetRepository(DatabaseContext context) : IWorkoutSetRepository
{
    private const string SetNotFoundMessage = "The set to update no longer exists.";
    private const string UpdateValues = "UPDATE WorkoutSet SET Weight = ?, Reps = ? WHERE Id = ?";
    private const string RenumberForSessionExercise = """
        UPDATE WorkoutSet
        SET SetNumber = (
            SELECT COUNT(*) FROM WorkoutSet earlier
            WHERE earlier.SessionExerciseId = WorkoutSet.SessionExerciseId
              AND earlier.Id <= WorkoutSet.Id)
        WHERE SessionExerciseId = ?
        """;
    private const string SelectForSessionExercisesFormat = "SELECT * FROM WorkoutSet WHERE SessionExerciseId IN ({0})";
    private const string SelectLastSessionSets = """
        SELECT * FROM WorkoutSet
        WHERE SessionExerciseId = (
            SELECT earlier.Id FROM SessionExercise earlier
            JOIN WorkoutSet earlierSet ON earlierSet.SessionExerciseId = earlier.Id
            JOIN WorkoutSession session ON session.Id = earlier.WorkoutSessionId
            WHERE earlier.ExerciseId = ? AND earlier.WorkoutSessionId <> ? AND session.EndTimeUtc IS NOT NULL
            ORDER BY session.StartTimeUtc DESC, earlier.[Order] DESC
            LIMIT 1)
        ORDER BY SetNumber
        """;
    private const string SelectRepMaxesFormat = """
        SELECT SetId, SessionId, ExerciseId, DateUtc, Weight, Reps FROM (
            SELECT logged.Id AS SetId,
                   session.Id AS SessionId,
                   performed.ExerciseId AS ExerciseId,
                   session.StartTimeUtc AS DateUtc,
                   logged.Weight AS Weight,
                   logged.Reps AS Reps,
                   ROW_NUMBER() OVER (
                       PARTITION BY performed.ExerciseId, logged.Reps
                       ORDER BY logged.Weight DESC, session.StartTimeUtc, logged.Id) AS Rank
            FROM WorkoutSet logged
            JOIN SessionExercise performed ON performed.Id = logged.SessionExerciseId
            JOIN WorkoutSession session ON session.Id = performed.WorkoutSessionId
            WHERE session.EndTimeUtc IS NOT NULL AND logged.Reps >= ? AND performed.ExerciseId IN ({0}))
        WHERE Rank = 1
        """;
    private const string SelectSessionVolumes = """
        SELECT session.Id AS SessionId, session.StartTimeUtc AS DateUtc, SUM(logged.Weight * logged.Reps) AS Volume
        FROM WorkoutSet logged
        JOIN SessionExercise performed ON performed.Id = logged.SessionExerciseId
        JOIN WorkoutSession session ON session.Id = performed.WorkoutSessionId
        WHERE session.EndTimeUtc IS NOT NULL AND logged.Reps >= ? AND performed.ExerciseId = ?
        GROUP BY session.Id, session.StartTimeUtc
        """;

    public async Task AddAsync(WorkoutSet set)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(set.SessionExerciseId);

        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Insert(set);
            transaction.Execute(RenumberForSessionExercise, set.SessionExerciseId);
            set.SetNumber = transaction.Get<WorkoutSet>(set.Id).SetNumber;
        });
    }

    public async Task UpdateAsync(int setId, double weight, int reps)
    {
        var connection = await context.GetConnectionAsync();
        var updatedRows = await connection.ExecuteAsync(UpdateValues, weight, reps, setId);

        if (updatedRows == 0)
        {
            throw new InvalidOperationException(SetNotFoundMessage);
        }
    }

    public async Task DeleteAsync(int setId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            var set = transaction.Find<WorkoutSet>(setId);
            if (set is null)
            {
                return;
            }

            transaction.Delete<WorkoutSet>(setId);
            transaction.Execute(RenumberForSessionExercise, set.SessionExerciseId);
        });
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
        return await connection.QueryAsync<WorkoutSet>(SelectLastSessionSets, exerciseId, currentSessionId);
    }

    public async Task<List<PerformedSet>> GetRepMaxesAsync(IReadOnlyCollection<int> exerciseIds)
    {
        if (exerciseIds.Count == 0)
        {
            return [];
        }

        var connection = await context.GetConnectionAsync();
        var query = string.Format(SelectRepMaxesFormat, SqlPlaceholders.For(exerciseIds.Count));
        var repMaxes = await connection.QueryAsync<PerformedSet>(query, [PersonalRecords.MinReps, .. exerciseIds.Select(id => (object)id)]);

        return [.. repMaxes.Select(set => set with { DateUtc = StoredDateTime.AsUtc(set.DateUtc) })];
    }

    public async Task<List<SessionVolume>> GetSessionVolumesAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        var volumes = await connection.QueryAsync<SessionVolume>(SelectSessionVolumes, PersonalRecords.MinReps, exerciseId);

        return [.. volumes.Select(volume => volume with { DateUtc = StoredDateTime.AsUtc(volume.DateUtc) })];
    }

    public void Clear(SQLiteConnection transaction) => transaction.DeleteAll<WorkoutSet>();
}
