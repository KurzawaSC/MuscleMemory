using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public sealed class ProgressionSuggestionService(IWorkoutSetRepository setRepository) : IProgressionSuggestionService
{
    public const int ComparedSessions = 2;

    public async Task<ProgressionSuggestion?> SuggestAsync(SessionExercise exercise)
    {
        if (RepRange.From(exercise.MinReps, exercise.MaxReps) is not { } range)
        {
            return null;
        }

        var recentSets = await setRepository.GetRecentSessionSetsAsync(exercise.ExerciseId, ComparedSessions);
        List<PerformedSet[]> sessions = [.. recentSets.GroupBy(set => set.SessionId).Select(session => session.ToArray())];

        return DoubleProgression.Suggest(
            range,
            exercise.PlannedSets,
            sessions.ElementAtOrDefault(0) ?? [],
            sessions.ElementAtOrDefault(1) ?? [],
            DomainDefaults.WeightStepInKg);
    }
}
