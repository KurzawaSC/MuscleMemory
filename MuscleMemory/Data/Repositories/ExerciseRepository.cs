using MuscleMemory.Models;
using SQLite;

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

    public void Update(SQLiteConnection transaction, Exercise exercise) => transaction.Update(exercise);

    public void Delete(SQLiteConnection transaction, int exerciseId) => transaction.Delete<Exercise>(exerciseId);

    public void Clear(SQLiteConnection transaction) => transaction.DeleteAll<Exercise>();
}
