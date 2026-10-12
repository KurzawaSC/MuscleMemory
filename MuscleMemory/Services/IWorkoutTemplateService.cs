using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IWorkoutTemplateService
{
    Task<List<Workout>> GetAllAsync();
    Task<List<WorkoutExercise>> GetAllExercisesAsync();
    Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId);
    Task SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task DeleteAsync(int workoutId);
}
