using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class WorkoutTemplateService(IWorkoutRepository workoutRepository) : IWorkoutTemplateService
{
    public Task<List<Workout>> GetAllAsync() => workoutRepository.GetAllAsync();

    public Task<List<WorkoutExercise>> GetAllExercisesAsync() => workoutRepository.GetAllExercisesAsync();

    public Task DeleteAsync(int workoutId) => workoutRepository.DeleteAsync(workoutId);
}
