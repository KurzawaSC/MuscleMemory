using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IExerciseCatalogService
{
    Task AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int exerciseId);
}
