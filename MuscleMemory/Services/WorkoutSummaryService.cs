using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutSummaryService(IWorkoutSetRepository setRepository) : IWorkoutSummaryService
{
    public async Task<WorkoutSummary> BuildAsync(IReadOnlyList<SessionExercise> performedExercises)
    {
        var loggedSets = await setRepository.GetForSessionExercisesAsync([.. performedExercises.Select(performed => performed.Id)]);

        var setsByExercise = loggedSets
            .GroupBy(set => set.SessionExerciseId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WorkoutSet>)[.. group.OrderBy(set => set.SetNumber)]);

        List<CompletedExerciseSummary> completedExercises = [];
        double totalVolume = 0;

        foreach (var performedExercise in performedExercises)
        {
            if (!setsByExercise.TryGetValue(performedExercise.Id, out var sets))
            {
                continue;
            }

            totalVolume += sets.Sum(set => set.Weight * set.Reps);
            completedExercises.Add(new CompletedExerciseSummary(performedExercise.ExerciseName, sets));
        }

        return new WorkoutSummary(totalVolume, completedExercises);
    }
}
