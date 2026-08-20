using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IActiveWorkoutStateRepository
{
    Task SaveAsync(ActiveWorkoutState state);
    Task<ActiveWorkoutState?> GetAsync();
    Task ClearAsync();
}
