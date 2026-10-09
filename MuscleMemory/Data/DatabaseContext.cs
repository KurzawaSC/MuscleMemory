using MuscleMemory.Constants;
using MuscleMemory.Models;
using SQLite;

namespace MuscleMemory.Data;

public sealed class DatabaseContext
{
    private readonly Lock _connectionGate = new();
    private Task<SQLiteAsyncConnection>? _connection;

    public DatabaseContext()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseNames.DatabaseFileName);
    }

    public static IReadOnlyList<Type> Tables { get; } =
    [
        typeof(Exercise),
        typeof(Workout),
        typeof(WorkoutExercise),
        typeof(WorkoutSession),
        typeof(SessionExercise),
        typeof(WorkoutSet),
        typeof(ActiveWorkoutState)
    ];

    public string DatabasePath { get; }

    public static SQLiteConnection OpenReadOnly(string path)
    {
        SQLitePCL.Batteries_V2.Init();
        return new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
    }

    public Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        lock (_connectionGate)
        {
            if (_connection is null or { IsFaulted: true } or { IsCanceled: true })
            {
                _connection = OpenConnectionAsync();
            }

            return _connection;
        }
    }

    private async Task<SQLiteAsyncConnection> OpenConnectionAsync()
    {
        SQLitePCL.Batteries_V2.Init();

        var connection = new SQLiteAsyncConnection(DatabasePath);

        await connection.CreateTablesAsync(CreateFlags.None, [.. Tables]);

        return connection;
    }
}
