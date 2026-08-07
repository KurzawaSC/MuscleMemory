using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data;

public class DatabaseContext
{
    private SQLiteAsyncConnection? _connection;
    private async Task InitAsync()
    {
        if (_connection != null)
            return;

        SQLitePCL.Batteries_V2.Init();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MuscleMemory.db3");
        _connection = new SQLiteAsyncConnection(dbPath);

        await _connection!.CreateTableAsync<Exercise>();
        await _connection!.CreateTableAsync<Workout>();
        await _connection!.CreateTableAsync<WorkoutExercise>();
        await _connection!.CreateTableAsync<WorkoutSet>();
        await _connection!.CreateTableAsync<WorkoutSession>();
        try
        {
            await _connection!.ExecuteAsync("ALTER TABLE WorkoutSet ADD COLUMN WorkoutSessionId INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
        }
    }
    public async Task<List<Exercise>> GetExercisesAsync()
    {
        await InitAsync();
        return await _connection!.Table<Exercise>().ToListAsync();
    }
    public async Task<int> AddExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return await _connection!.InsertAsync(exercise);
    }
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
    public async Task<List<Workout>> GetWorkoutsAsync()
    {
        await InitAsync();
        return await _connection!.Table<Workout>().ToListAsync();
    }
    public async Task<int> SaveFullWorkoutAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        await InitAsync();
        await _connection!.InsertAsync(workout);
        foreach (var exerciseDetails in exercises)
        {
            exerciseDetails.WorkoutId = workout.Id;
            await _connection!.InsertAsync(exerciseDetails);
        }

        return workout.Id;
    }

    public async Task ClearAllDataAsync()
    {
        await InitAsync();
        await _connection!.DeleteAllAsync<WorkoutExercise>();
        await _connection!.DeleteAllAsync<Workout>();
        await _connection!.DeleteAllAsync<Exercise>();
        await _connection!.DeleteAllAsync<WorkoutSet>();
        await _connection!.DeleteAllAsync<WorkoutSession>();
    }

    public async Task<int> CreateWorkoutSessionAsync(int workoutId)
    {
        await InitAsync();
        var session = new WorkoutSession
        {
            WorkoutId = workoutId,
            StartTime = DateTime.UtcNow
        };
        await _connection!.InsertAsync(session);
        return session.Id;
    }

    public async Task FinishWorkoutSessionAsync(int sessionId)
    {
        await InitAsync();
        var session = await _connection!.Table<WorkoutSession>().Where(s => s.Id == sessionId).FirstOrDefaultAsync();
        if (session != null)
        {
            session.EndTime = DateTime.UtcNow;
            await _connection!.UpdateAsync(session);
        }
    }

    public async Task SaveSetAsync(WorkoutSet set)
    {
        await InitAsync();
        await _connection!.InsertAsync(set);
    }

    public async Task<List<WorkoutSet>> GetSetsForWorkoutExerciseAsync(int workoutExerciseId, int sessionId)
    {
        await InitAsync();
        return await _connection!.Table<WorkoutSet>()
                                 .Where(s => s.WorkoutExerciseId == workoutExerciseId && s.WorkoutSessionId == sessionId)
                                 .ToListAsync();
    }

    public async Task<List<WorkoutSet>> GetLastSessionSetsForExerciseAsync(int workoutExerciseId, int currentSessionId)
    {
        await InitAsync();
        
        var lastSet = await _connection!.Table<WorkoutSet>()
                                        .Where(s => s.WorkoutExerciseId == workoutExerciseId && s.WorkoutSessionId < currentSessionId)
                                        .OrderByDescending(s => s.WorkoutSessionId)
                                        .FirstOrDefaultAsync();

        if (lastSet == null)
            return new List<WorkoutSet>();

        return await _connection!.Table<WorkoutSet>()
                                 .Where(s => s.WorkoutExerciseId == workoutExerciseId && s.WorkoutSessionId == lastSet.WorkoutSessionId)
                                 .OrderBy(s => s.SetNumber)
                                 .ToListAsync();
    }

    public async Task DeleteSetAsync(int setId)
    {
        await InitAsync();
        await _connection!.DeleteAsync<WorkoutSet>(setId);
    }

    public async Task<List<WorkoutExercise>> GetExercisesForWorkoutAsync(int workoutId)
    {
        await InitAsync();
        return await _connection!.Table<WorkoutExercise>()
                                 .Where(we => we.WorkoutId == workoutId)
                                 .ToListAsync();
    }

    public async Task DeleteWorkoutAsync(int workoutId)
    {
        await InitAsync();
        await _connection!.DeleteAsync<Workout>(workoutId);
        await _connection!.ExecuteAsync("DELETE FROM WorkoutExercise WHERE WorkoutId = ?", workoutId);
    }

    public async Task DeleteExerciseAsync(int exerciseId)
    {
        await InitAsync();
        await _connection!.DeleteAsync<Exercise>(exerciseId);
    }

    public async Task UpdateExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        await _connection!.UpdateAsync(exercise);
    }

    public async Task UpdateFullWorkoutAsync(Workout workout, List<WorkoutExercise> exercises)
    {
        await InitAsync();
        await _connection!.UpdateAsync(workout);

        await _connection!.ExecuteAsync("DELETE FROM WorkoutExercise WHERE WorkoutId = ?", workout.Id);

        foreach (var exerciseDetails in exercises)
        {
            exerciseDetails.WorkoutId = workout.Id;
            exerciseDetails.Id = 0; 
            await _connection!.InsertAsync(exerciseDetails);
        }
    }

    public async Task<List<ExerciseHistoryEntry>> GetExerciseHistoryAsync(int exerciseId)
    {
        await InitAsync();
        
        var workoutExercises = await _connection!.Table<WorkoutExercise>()
                                                 .Where(we => we.ExerciseId == exerciseId)
                                                 .ToListAsync();
        
        var history = new List<ExerciseHistoryEntry>();
        var workouts = await _connection!.Table<Workout>().ToListAsync(); 
        
        foreach (var we in workoutExercises)
        {
            var sets = await _connection!.Table<WorkoutSet>()
                                        .Where(s => s.WorkoutExerciseId == we.Id)
                                        .ToListAsync();
                                        
            var groupedBySession = sets.GroupBy(s => s.WorkoutSessionId);
            
            foreach (var group in groupedBySession)
            {
                int sessionId = group.Key;
                var session = await _connection!.Table<WorkoutSession>().Where(s => s.Id == sessionId).FirstOrDefaultAsync();
                if (session == null) continue;
                
                var workout = workouts.FirstOrDefault(w => w.Id == session.WorkoutId);
                
                history.Add(new ExerciseHistoryEntry
                {
                    Date = session.StartTime,
                    WorkoutName = workout?.Name ?? "Unknown Workout",
                    Sets = group.OrderBy(s => s.SetNumber).ToList()
                });
            }
        }
        
        return history.OrderByDescending(h => h.Date).ToList();
    }

    public async Task<List<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId)
    {
        await InitAsync();
        
        var sessions = await _connection!.Table<WorkoutSession>()
                                         .Where(s => s.WorkoutId == workoutId)
                                         .OrderByDescending(s => s.StartTime)
                                         .ToListAsync();
                                         
        var workoutExercises = await _connection!.Table<WorkoutExercise>()
                                                .Where(we => we.WorkoutId == workoutId)
                                                .ToListAsync();
                                                
        var history = new List<WorkoutHistorySession>();
        
        foreach (var session in sessions)
        {
            int sessionId = session.Id;
            var sets = await _connection!.Table<WorkoutSet>()
                                        .Where(s => s.WorkoutSessionId == sessionId)
                                        .ToListAsync();
                                        
            if (!sets.Any()) continue;
            
            var historySession = new WorkoutHistorySession
            {
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TotalVolume = sets.Sum(s => s.Weight * s.Reps)
            };
            
            var groupedSets = sets.GroupBy(s => s.WorkoutExerciseId);
            foreach (var group in groupedSets)
            {
                var we = workoutExercises.FirstOrDefault(w => w.Id == group.Key);
                historySession.Exercises.Add(new WorkoutHistoryExercise
                {
                    ExerciseName = we?.ExerciseName ?? "Unknown",
                    Sets = group.OrderBy(s => s.SetNumber).ToList()
                });
            }
            
            history.Add(historySession);
        }
        
        return history;
    }
}
