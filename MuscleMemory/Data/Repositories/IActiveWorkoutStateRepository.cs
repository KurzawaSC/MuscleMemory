using MuscleMemory.Models;
using SQLite;

namespace MuscleMemory.Data.Repositories;

public interface IActiveWorkoutStateRepository
{
    Task SaveAsync(ActiveWorkoutState state);
    Task<ActiveWorkoutState?> GetAsync();
    Task ClearAsync();
    void Clear(SQLiteConnection transaction);
}
