using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutRepository
{
    Task<List<Workout>> GetAllAsync();
    Task<int> SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises);
    Task DeleteAsync(int workoutId);
    Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId);
    Task<List<WorkoutExercise>> GetAllExercisesAsync();
    void RenameExercise(SQLiteConnection transaction, int exerciseId, string exerciseName);
    void Clear(SQLiteConnection transaction);
}
