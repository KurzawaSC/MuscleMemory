using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public interface IExerciseProgressService
{
    Task<ExerciseProgress> GetProgressAsync(int exerciseId);
}
