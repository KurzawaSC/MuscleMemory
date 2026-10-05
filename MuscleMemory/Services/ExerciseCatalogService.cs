using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class ExerciseCatalogService(
    DatabaseContext context,
    IExerciseRepository exerciseRepository,
    IWorkoutRepository workoutRepository) : IExerciseCatalogService
{
    public async Task UpdateAsync(Exercise exercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            exerciseRepository.Update(transaction, exercise);
            workoutRepository.RenameExercise(transaction, exercise.Id, exercise.Name);
        });
    }
}
