using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IExerciseCatalogService
{
    Task<List<Exercise>> GetAllAsync();
    Task AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int exerciseId);
}
