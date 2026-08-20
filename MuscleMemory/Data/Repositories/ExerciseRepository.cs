using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed class ExerciseRepository(DatabaseContext context) : IExerciseRepository
{
    public async Task<List<Exercise>> GetAllAsync()
    {
        var connection = await context.GetConnectionAsync();
        return await connection.Table<Exercise>().ToListAsync();
    }

    public async Task AddAsync(Exercise exercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.InsertAsync(exercise);
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        var connection = await context.GetConnectionAsync();
        await connection.UpdateAsync(exercise);
    }

    public async Task DeleteAsync(int exerciseId)
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAsync<Exercise>(exerciseId);
    }

    public async Task ClearAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.DeleteAllAsync<Exercise>();
    }
}
