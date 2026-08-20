using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutRepository
{
    Task<List<Workout>> GetAllAsync();
    Task<int> SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task DeleteAsync(int workoutId);
    Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId);
    Task ClearAsync();
}
