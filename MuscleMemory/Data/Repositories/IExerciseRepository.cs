using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync();
    Task AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int exerciseId);
    Task ClearAsync();
}
