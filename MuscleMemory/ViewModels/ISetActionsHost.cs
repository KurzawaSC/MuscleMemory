using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public interface ISetActionsHost
{
    bool CanShowSetActions { get; }

    Task UpdateSetAsync(WorkoutSet set, SetValues values);

    Task DeleteSetAsync(WorkoutSet set);

    Task RefreshSetsAsync(int sessionExerciseId);
}
