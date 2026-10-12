using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutTemplateService(IWorkoutRepository workoutRepository) : IWorkoutTemplateService
{
    public Task<List<Workout>> GetAllAsync() => workoutRepository.GetAllAsync();

    public Task<List<WorkoutExercise>> GetAllExercisesAsync() => workoutRepository.GetAllExercisesAsync();

    public Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId) => workoutRepository.GetExercisesAsync(workoutId);

    public Task SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises) =>
        workoutRepository.SaveWithExercisesAsync(workout, exercises);

    public Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises) =>
        workoutRepository.UpdateWithExercisesAsync(workout, exercises);

    public Task DeleteAsync(int workoutId) => workoutRepository.DeleteAsync(workoutId);
}
