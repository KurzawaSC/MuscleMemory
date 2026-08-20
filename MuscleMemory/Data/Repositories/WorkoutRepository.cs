using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutRepository(DatabaseContext context) : IWorkoutRepository
{
    private const string DeleteExercisesByWorkout = "DELETE FROM WorkoutExercise WHERE WorkoutId = ?";

    public async Task<List<Workout>> GetAllAsync()
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<Workout>().ToListAsync();
    }

    public async Task<int> SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        var connection = await context.GetConnectionAsync();
        await connection.InsertAsync(workout);

        foreach (var exercise in exercises)
        {
            exercise.WorkoutId = workout.Id;
            await connection.InsertAsync(exercise);
        }

        return workout.Id;
    }

    public async Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        var connection = await context.GetConnectionAsync();
        await connection.UpdateAsync(workout);
        await connection.ExecuteAsync(DeleteExercisesByWorkout, workout.Id);

        foreach (var exercise in exercises)
        {
            exercise.WorkoutId = workout.Id;
            exercise.Id = 0;
            await connection.InsertAsync(exercise);
        }
    }

    public async Task DeleteAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAsync<Workout>(workoutId);
        await connection.ExecuteAsync(DeleteExercisesByWorkout, workoutId);
    }

    public async Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutExercise>()
                               .Where(exercise => exercise.WorkoutId == workoutId)
                               .ToListAsync();
    }

    public async Task<List<WorkoutExercise>> GetExercisesForExerciseAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutExercise>()
                               .Where(exercise => exercise.ExerciseId == exerciseId)
                               .ToListAsync();
    }

    public async Task<int> AddExerciseAsync(WorkoutExercise exercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.InsertAsync(exercise);
        return exercise.Id;
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutExercise>();
        await connection.DeleteAllAsync<Workout>();
    }
}
