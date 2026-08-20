using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutRepository(DatabaseContext context) : IWorkoutRepository
{
    private const string DeleteExercisesByWorkout = "DELETE FROM WorkoutExercise WHERE WorkoutId = ?";
    private const string SelectNextExerciseOrder = "SELECT IFNULL(MAX([Order]), -1) + 1 FROM WorkoutExercise WHERE WorkoutId = ?";

    public async Task<List<Workout>> GetAllAsync()
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<Workout>().ToListAsync();
    }

    public async Task<int> SaveWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Insert(workout);
            InsertOrderedExercises(transaction, workout.Id, exercises);
        });

        return workout.Id;
    }

    public async Task UpdateWithExercisesAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Update(workout);
            transaction.Execute(DeleteExercisesByWorkout, workout.Id);
            InsertOrderedExercises(transaction, workout.Id, exercises);
        });
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
                               .OrderBy(exercise => exercise.Order)
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
        exercise.Order = await connection.ExecuteScalarAsync<int>(SelectNextExerciseOrder, exercise.WorkoutId);
        await connection.InsertAsync(exercise);
        return exercise.Id;
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<WorkoutExercise>();
        await connection.DeleteAllAsync<Workout>();
    }

    private static void InsertOrderedExercises(SQLiteConnection transaction, int workoutId, List<WorkoutExercise> exercises)
    {
        for (int position = 0; position < exercises.Count; position++)
        {
            var exercise = exercises[position];
            exercise.WorkoutId = workoutId;
            exercise.Order = position;
            exercise.Id = 0;
            transaction.Insert(exercise);
        }
    }
}
