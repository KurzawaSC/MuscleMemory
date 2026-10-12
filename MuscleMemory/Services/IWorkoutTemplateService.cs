using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IWorkoutTemplateService
{
    Task<List<Workout>> GetAllAsync();
    Task<List<WorkoutExercise>> GetAllExercisesAsync();
    Task DeleteAsync(int workoutId);
}
