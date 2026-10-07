using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class WorkoutRepository(DatabaseContext context) : IWorkoutRepository
{
    private const string WorkoutNotFoundMessage = "The workout to update no longer exists.";
    private const string DeleteExercisesByWorkout = "DELETE FROM WorkoutExercise WHERE WorkoutId = ?";
    private const string RenameExerciseInTemplates = "UPDATE WorkoutExercise SET ExerciseName = ? WHERE ExerciseId = ?";
    private const string DeleteExerciseFromTemplates = "DELETE FROM WorkoutExercise WHERE ExerciseId = ?";
    private const string SelectExistingExerciseIdsFormat = "SELECT Id FROM Exercise WHERE Id IN ({0})";
    private const string CountWorkoutsContainingExercise = "SELECT COUNT(DISTINCT WorkoutId) FROM WorkoutExercise WHERE ExerciseId = ?";

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
            if (transaction.Update(workout) == 0)
            {
                throw new InvalidOperationException(WorkoutNotFoundMessage);
            }

            transaction.Execute(DeleteExercisesByWorkout, workout.Id);
            InsertOrderedExercises(transaction, workout.Id, exercises);
        });
    }

    public async Task DeleteAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Delete<Workout>(workoutId);
            transaction.Execute(DeleteExercisesByWorkout, workoutId);
        });
    }

    public async Task<List<WorkoutExercise>> GetExercisesAsync(int workoutId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutExercise>()
                               .Where(exercise => exercise.WorkoutId == workoutId)
                               .OrderBy(exercise => exercise.Order)
                               .ToListAsync();
    }

    public async Task<List<WorkoutExercise>> GetAllExercisesAsync()
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<WorkoutExercise>()
                               .OrderBy(exercise => exercise.WorkoutId)
                               .ThenBy(exercise => exercise.Order)
                               .ToListAsync();
    }

    public async Task<int> CountWorkoutsContainingAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(CountWorkoutsContainingExercise, exerciseId);
    }

    public void RenameExercise(SQLiteConnection transaction, int exerciseId, string exerciseName) =>
        transaction.Execute(RenameExerciseInTemplates, exerciseName, exerciseId);

    public void RemoveExercise(SQLiteConnection transaction, int exerciseId) =>
        transaction.Execute(DeleteExerciseFromTemplates, exerciseId);

    public void Clear(SQLiteConnection transaction)
    {
        transaction.DeleteAll<WorkoutExercise>();
        transaction.DeleteAll<Workout>();
    }

    private static void InsertOrderedExercises(SQLiteConnection transaction, int workoutId, List<WorkoutExercise> exercises)
    {
        var existingExerciseIds = SelectExistingExerciseIds(transaction, exercises);
        var insertable = exercises.Where(exercise => existingExerciseIds.Contains(exercise.ExerciseId)).ToList();

        for (var position = 0; position < insertable.Count; position++)
        {
            var exercise = insertable[position];
            exercise.WorkoutId = workoutId;
            exercise.Order = position;
            exercise.Id = 0;
            transaction.Insert(exercise);
        }
    }

    private static HashSet<int> SelectExistingExerciseIds(SQLiteConnection transaction, List<WorkoutExercise> exercises)
    {
        if (exercises.Count == 0)
        {
            return [];
        }

        var exerciseIds = exercises.Select(exercise => exercise.ExerciseId).Distinct().ToList();
        var query = string.Format(SelectExistingExerciseIdsFormat, SqlPlaceholders.For(exerciseIds.Count));

        return [.. transaction.QueryScalars<int>(query, [.. exerciseIds.Select(id => (object)id)])];
    }
}
