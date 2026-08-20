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
        var workouts = await workoutRepository.GetAllAsync();

        var history = new List<ExerciseHistoryEntry>();

        foreach (var workoutExercise in workoutExercises)
        {
            var sets = await setRepository.GetAllForWorkoutExerciseAsync(workoutExercise.Id);

            foreach (var sessionSets in sets.GroupBy(set => set.WorkoutSessionId))
            {
                var session = await sessionRepository.GetAsync(sessionSets.Key);
                if (session is null)
                {
                    continue;
                }

                var workout = workouts.FirstOrDefault(candidate => candidate.Id == session.WorkoutId);

                history.Add(new ExerciseHistoryEntry
                {
                    Date = session.StartTime,
                    WorkoutName = workout?.Name ?? UiText.UnknownWorkoutName,
                    Sets = [.. sessionSets.OrderBy(set => set.SetNumber)]
                });
            }
        }

        return [.. history.OrderByDescending(entry => entry.Date)];
    }

    public async Task<IReadOnlyList<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId)
    {
        var sessions = await sessionRepository.GetForWorkoutAsync(workoutId);
        var workoutExercises = await workoutRepository.GetExercisesAsync(workoutId);

        var history = new List<WorkoutHistorySession>();

        foreach (var session in sessions)
        {
            var sets = await setRepository.GetForSessionAsync(session.Id);
            if (sets.Count == 0)
            {
                continue;
            }

            history.Add(BuildSession(session, sets, workoutExercises));
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
