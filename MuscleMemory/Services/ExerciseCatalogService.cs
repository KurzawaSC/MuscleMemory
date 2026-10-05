using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class ExerciseCatalogService(
    DatabaseContext context,
    IExerciseRepository exerciseRepository,
    IWorkoutRepository workoutRepository,
    IDataChangeNotifier dataChanges) : IExerciseCatalogService
{
    public async Task AddAsync(Exercise exercise)
    {
        await exerciseRepository.AddAsync(exercise);
        dataChanges.Notify(DataArea.Exercises);
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            exerciseRepository.Update(transaction, exercise);
            workoutRepository.RenameExercise(transaction, exercise.Id, exercise.Name);
        });
        dataChanges.Notify(DataArea.All);
    }

    public async Task DeleteAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            workoutRepository.RemoveExercise(transaction, exerciseId);
            exerciseRepository.Delete(transaction, exerciseId);
        });
        dataChanges.Notify(DataArea.All);
    }
}
