using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync();
    Task AddAsync(Exercise exercise);
    void Update(SQLiteConnection transaction, Exercise exercise);
    Task DeleteAsync(int exerciseId);
    void Clear(SQLiteConnection transaction);
}
