using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IExerciseCatalogService
{
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int exerciseId);
}
