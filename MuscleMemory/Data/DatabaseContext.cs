using SQLite;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.Data;

public sealed class DatabaseContext
{
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connection;

    public DatabaseContext()
    {
        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseNames.DatabaseFileName);
        _connection = new Lazy<Task<SQLiteAsyncConnection>>(OpenConnectionAsync);
    }

    public string DatabasePath { get; }

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => _connection.Value;

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
