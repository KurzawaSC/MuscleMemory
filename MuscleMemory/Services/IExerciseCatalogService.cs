using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IExerciseCatalogService
{
    Task<List<Exercise>> GetAllAsync();
    Task<int> CountWorkoutsContainingAsync(int exerciseId);
    Task AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int exerciseId);
}
