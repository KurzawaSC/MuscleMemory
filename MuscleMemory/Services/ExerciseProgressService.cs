using MuscleMemory.Data.Repositories;
using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public sealed class ExerciseProgressService(IWorkoutSetRepository setRepository, TimeProvider clock) : IExerciseProgressService
{
    public async Task<ExerciseProgress> GetProgressAsync(int exerciseId) =>
        ProgressSeries.Build(await setRepository.GetSessionRepTotalsAsync(exerciseId), clock.LocalTimeZone);
}
