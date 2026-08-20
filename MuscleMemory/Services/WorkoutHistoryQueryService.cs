using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutHistoryQueryService(
    IWorkoutRepository workoutRepository,
    IWorkoutSessionRepository sessionRepository,
    IWorkoutSetRepository setRepository) : IWorkoutHistoryQueryService
{
    public async Task<IReadOnlyList<ExerciseHistoryEntry>> GetExerciseHistoryAsync(int exerciseId)
    {
        var workoutExercises = await workoutRepository.GetExercisesForExerciseAsync(exerciseId);
        var sets = await setRepository.GetForWorkoutExercisesAsync([.. workoutExercises.Select(exercise => exercise.Id)]);

        if (sets.Count == 0)
        {
            return [];
        }

        var sessions = await sessionRepository.GetByIdsAsync([.. sets.Select(set => set.WorkoutSessionId).Distinct()]);
        var sessionsById = sessions.ToDictionary(session => session.Id);
        var workoutsById = (await workoutRepository.GetAllAsync()).ToDictionary(workout => workout.Id);

        var history = new List<ExerciseHistoryEntry>();

        foreach (var loggedSets in sets.GroupBy(set => (set.WorkoutExerciseId, set.WorkoutSessionId)))
        {
            if (!sessionsById.TryGetValue(loggedSets.Key.WorkoutSessionId, out var session))
            {
                continue;
            }

            workoutsById.TryGetValue(session.WorkoutId, out var workout);

            history.Add(new ExerciseHistoryEntry
            {
                Date = session.StartTime,
                WorkoutName = workout?.Name ?? UiText.UnknownWorkoutName,
                Sets = [.. loggedSets.OrderBy(set => set.SetNumber)]
            });
        }

        return [.. history.OrderByDescending(entry => entry.Date)];
    }

    public async Task<IReadOnlyList<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId)
    {
        var sessions = await sessionRepository.GetForWorkoutAsync(workoutId);
        var workoutExercises = await workoutRepository.GetExercisesAsync(workoutId);

        var setsBySession = (await setRepository.GetForSessionsAsync([.. sessions.Select(session => session.Id)]))
            .GroupBy(set => set.WorkoutSessionId)
            .ToDictionary(loggedSets => loggedSets.Key, loggedSets => loggedSets.ToList());

        var history = new List<WorkoutHistorySession>();

        foreach (var session in sessions)
        {
            if (setsBySession.TryGetValue(session.Id, out var sets))
            {
                history.Add(BuildSession(session, sets, workoutExercises));
            }
        }

        return history;
    }

    private static WorkoutHistorySession BuildSession(
        WorkoutSession session,
        List<WorkoutSet> sets,
        List<WorkoutExercise> workoutExercises)
    {
        var historySession = new WorkoutHistorySession
        {
            SessionId = session.Id,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            TotalVolume = sets.Sum(set => set.Weight * set.Reps)
        };

        foreach (var exerciseSets in sets.GroupBy(set => set.WorkoutExerciseId))
        {
            var workoutExercise = workoutExercises.FirstOrDefault(candidate => candidate.Id == exerciseSets.Key);

            var historyExercise = new WorkoutHistoryExercise
            {
                WorkoutExerciseId = exerciseSets.Key,
                WorkoutSessionId = session.Id,
                ExerciseName = workoutExercise?.ExerciseName ?? UiText.UnknownExerciseName
            };

            foreach (var set in exerciseSets.OrderBy(set => set.SetNumber))
            {
                historyExercise.Sets.Add(set);
            }

            historySession.Exercises.Add(historyExercise);
        }

        return historySession;
    }
}
