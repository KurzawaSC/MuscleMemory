using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutSummaryService(IWorkoutSetRepository setRepository, IPersonalRecordService personalRecords) : IWorkoutSummaryService
{
    public async Task<WorkoutSummary> BuildAsync(IReadOnlyList<SessionExercise> performedExercises)
    {
        var loggedSets = await setRepository.GetForSessionExercisesAsync([.. performedExercises.Select(performed => performed.Id)]);
        var newRecords = await personalRecords.FindNewRecordsAsync(performedExercises);

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

            totalVolume += sets.TotalVolume();
            completedExercises.Add(new CompletedExerciseSummary(performedExercise.ExerciseName, sets, sets.Count(set => newRecords.Contains(set.Id))));
        }

        return new WorkoutSummary(totalVolume, completedExercises);
    }
}
