using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data;

public class DatabaseContext
{
    private SQLiteAsyncConnection? _connection;

    // 1. Zmodyfikuj istniejącą metodę InitAsync, aby tworzyła nowe tabele
    private async Task InitAsync()
    {
        if (_connection != null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MuscleMemory.db3");
        _connection = new SQLiteAsyncConnection(dbPath);

        await _connection.CreateTableAsync<Exercise>();
        // DODANE LINIE:
        await _connection.CreateTableAsync<Workout>();
        await _connection.CreateTableAsync<WorkoutExercise>();
    }

    // Metoda do pobierania wszystkich ćwiczeń
    public async Task<List<Exercise>> GetExercisesAsync()
    {
        await InitAsync();
        return await _connection.Table<Exercise>().ToListAsync();
    }

    // Metoda do zapisywania nowego ćwiczenia
    public async Task<int> AddExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return await _connection.InsertAsync(exercise);
    }

    // Opcjonalnie: Metoda do usuwania bazy (przyda się do ustawień z ekranu Settings)
    public void EraseDatabase()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MuscleMemory.db3");
        if (File.Exists(dbPath))
        {
            _connection?.CloseAsync().Wait();
            _connection = null;
            File.Delete(dbPath);
        }
    }

    // 2. Dodaj metodę do pobierania listy wszystkich szablonów treningów
    public async Task<List<Workout>> GetWorkoutsAsync()
    {
        await InitAsync();
        return await _connection.Table<Workout>().ToListAsync();
    }

    // 3. Dodaj funkcję tworzącą nowy Trening wraz z jego ćwiczeniami (Zapis transakcyjny)
    public async Task<int> SaveFullWorkoutAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        await InitAsync();

        // Zapisujemy sam Trening (SQLite automatycznie nada mu ID)
        await _connection.InsertAsync(workout);

        // Przypisujemy ID nowego Treningu do wszystkich ćwiczeń i zapisujemy je
        foreach (var exerciseDetails in exercises)
        {
            exerciseDetails.WorkoutId = workout.Id;
            await _connection.InsertAsync(exerciseDetails);
        }

        return workout.Id; // Zwracamy ID, gdybyśmy chcieli od razu przejść do tego treningu
    }

    public async Task ClearAllDataAsync()
    {
        await InitAsync();

        // Kasujemy zawartość wszystkich tabel
        await _connection!.DeleteAllAsync<WorkoutExercise>();
        await _connection!.DeleteAllAsync<Workout>();
        await _connection!.DeleteAllAsync<Exercise>();
    }
}