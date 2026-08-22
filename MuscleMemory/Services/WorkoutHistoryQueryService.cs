using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutHistoryQueryService(
    IWorkoutSessionRepository sessionRepository,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository) : IWorkoutHistoryQueryService
{
    public async Task<IReadOnlyList<ExerciseHistoryEntry>> GetExerciseHistoryAsync(int exerciseId)
    {
        var performances = await sessionExerciseRepository.GetForExerciseAsync(exerciseId);
        var setsByPerformance = await GetSetsByPerformanceAsync(performances);

        if (setsByPerformance.Count == 0)
        {
            return [];
        }

        var sessionsById = (await sessionRepository.GetCompletedByIdsAsync([.. performances.Select(performance => performance.WorkoutSessionId).Distinct()]))
            .ToDictionary(session => session.Id);

        var history = new List<ExerciseHistoryEntry>();

        foreach (var performance in performances)
        {
            if (!setsByPerformance.TryGetValue(performance.Id, out var sets)
                || !sessionsById.TryGetValue(performance.WorkoutSessionId, out var session))
            {
                continue;
            }

            history.Add(new ExerciseHistoryEntry(session.StartTimeUtc, session.WorkoutName, sets));
        }

        return [.. history.OrderByDescending(entry => entry.DateUtc)];
    }

    public async Task<IReadOnlyList<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId)
    {
        var sessions = await sessionRepository.GetCompletedForWorkoutAsync(workoutId);
        var performances = await sessionExerciseRepository.GetForSessionsAsync([.. sessions.Select(session => session.Id)]);
        var setsByPerformance = await GetSetsByPerformanceAsync(performances);

        var performancesBySession = performances.GroupBy(performance => performance.WorkoutSessionId)
                                                .ToDictionary(group => group.Key, group => group.ToList());

        var history = new List<WorkoutHistorySession>();

        foreach (var session in sessions)
        {
            if (session.EndTimeUtc is not { } endTimeUtc
                || !performancesBySession.TryGetValue(session.Id, out var sessionPerformances))
            {
                continue;
            }

            var loggedExercises = BuildLoggedExercises(sessionPerformances, setsByPerformance);

            if (loggedExercises.Count == 0)
            {
                continue;
            }

            history.Add(new WorkoutHistorySession(
                session.Id,
                session.StartTimeUtc,
                endTimeUtc,
                loggedExercises.Sum(exercise => exercise.Sets.Sum(set => set.Weight * set.Reps)),
                loggedExercises));
        }

        return history;
    }

    private static List<WorkoutHistoryExercise> BuildLoggedExercises(
        List<SessionExercise> performances,
        Dictionary<int, List<WorkoutSet>> setsByPerformance) =>
    [
        .. performances.OrderBy(performance => performance.Order)
                       .Where(performance => setsByPerformance.ContainsKey(performance.Id))
                       .Select(performance => new WorkoutHistoryExercise(
                           performance.Id,
                           performance.ExerciseName,
                           setsByPerformance[performance.Id]))
    ];

    private async Task<Dictionary<int, List<WorkoutSet>>> GetSetsByPerformanceAsync(List<SessionExercise> performances)
    {
        var sets = await setRepository.GetForSessionExercisesAsync([.. performances.Select(performance => performance.Id)]);

        return sets.GroupBy(set => set.SessionExerciseId)
                   .ToDictionary(group => group.Key, group => group.OrderBy(set => set.SetNumber).ToList());
    }
}
