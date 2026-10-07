using MuscleMemory.Models;
using SQLite;

namespace MuscleMemory.Data.Repositories;

public interface ISessionExerciseRepository
{
    Task<List<SessionExercise>> GetForSessionAsync(int workoutSessionId);
    Task<List<SessionExercise>> GetForSessionsAsync(IReadOnlyCollection<int> workoutSessionIds);
    Task<List<SessionExercise>> GetForExerciseAsync(int exerciseId);
    Task AppendToSessionAsync(SessionExercise sessionExercise);
    Task DeleteAsync(int sessionExerciseId);
    void Clear(SQLiteConnection transaction);
}
