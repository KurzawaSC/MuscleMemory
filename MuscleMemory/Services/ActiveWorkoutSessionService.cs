using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class ActiveWorkoutSessionService(
    IWorkoutRepository workoutRepository,
    IWorkoutSessionRepository sessionRepository,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository,
    IActiveWorkoutStateRepository activeStateRepository) : IActiveWorkoutSessionService
{
    public async Task<StartedSession> StartAsync(Workout workout)
    {
        var template = await workoutRepository.GetExercisesAsync(workout.Id);
        return await sessionRepository.CreateWithSnapshotAsync(workout, template);
    }

    public async Task<ResumableSession?> FindResumableAsync()
    {
        var state = await activeStateRepository.GetAsync();
        if (state is null)
        {
            return null;
        }

        var session = await sessionRepository.GetAsync(state.SessionId);
        if (session is not { EndTimeUtc: null })
        {
            await activeStateRepository.ClearAsync();
            return null;
        }

        return new ResumableSession(state, session);
    }

    public Task<List<SessionExercise>> GetExercisesAsync(int sessionId) => sessionExerciseRepository.GetForSessionAsync(sessionId);

    public Task SaveStateAsync(ActiveWorkoutState state) => activeStateRepository.SaveAsync(state);

    public async Task FinishAsync(int sessionId)
    {
        await sessionRepository.FinishOrDiscardAsync(sessionId);
        await activeStateRepository.ClearAsync();
    }

    public Task<List<WorkoutSet>> GetSetsAsync(int sessionExerciseId) => setRepository.GetForSessionExerciseAsync(sessionExerciseId);

    public Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId) =>
        setRepository.GetLastSessionSetsAsync(exerciseId, currentSessionId);

    public Task AddSetAsync(WorkoutSet set) => setRepository.AddAsync(set);

    public Task UpdateSetAsync(int setId, double weight, int reps) => setRepository.UpdateAsync(setId, weight, reps);

    public Task DeleteSetAsync(int setId) => setRepository.DeleteAsync(setId);

    public async Task<bool> IsPlanCompleteAsync(IReadOnlyCollection<SessionExercise> exercises)
    {
        List<SessionExercise> plannedExercises = [.. exercises.Where(exercise => exercise.PlannedSets > 0)];
        if (plannedExercises.Count == 0)
        {
            return false;
        }

        var loggedSets = await setRepository.GetForSessionExercisesAsync([.. plannedExercises.Select(exercise => exercise.Id)]);
        var loggedCounts = loggedSets.CountBy(set => set.SessionExerciseId).ToDictionary();
        return plannedExercises.All(exercise => loggedCounts.GetValueOrDefault(exercise.Id) >= exercise.PlannedSets);
    }
}
