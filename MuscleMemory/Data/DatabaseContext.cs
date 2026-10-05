using SQLite;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.Data;

public sealed class DatabaseContext
{
    private readonly Lock _connectionGate = new();
    private Task<SQLiteAsyncConnection>? _connection;

    public DatabaseContext()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseNames.DatabaseFileName);
    }

    public string DatabasePath { get; }

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

        await connection.CreateTableAsync<Exercise>();
        await connection.CreateTableAsync<Workout>();
        await connection.CreateTableAsync<WorkoutExercise>();
        await connection.CreateTableAsync<WorkoutSession>();
        await connection.CreateTableAsync<SessionExercise>();
        await connection.CreateTableAsync<WorkoutSet>();
        await connection.CreateTableAsync<ActiveWorkoutState>();

        return connection;
    }
}
