using MuscleMemory.Data.Repositories;
using MuscleMemory.Services;
using NSubstitute;

namespace MuscleMemory.Tests.Testing;

public sealed class TestApp : IAsyncDisposable
{
    public static readonly DateTime StartUtc = new(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);

    public TestApp()
        : this(Substitute.For<IDataChangeNotifier>())
    {
    }

    public TestApp(IDataChangeNotifier dataChanges)
    {
        DataChanges = dataChanges;
        Exercises = new ExerciseRepository(Database.Context);
        Workouts = new WorkoutRepository(Database.Context);
        Sessions = new WorkoutSessionRepository(Database.Context, Clock);
        SessionExercises = new SessionExerciseRepository(Database.Context);
        Sets = new WorkoutSetRepository(Database.Context);
        ActiveState = new ActiveWorkoutStateRepository(Database.Context);

        Catalog = new ExerciseCatalogService(Database.Context, Exercises, Workouts, DataChanges);
        Templates = new WorkoutTemplateService(Workouts);
        ActiveSessions = new ActiveWorkoutSessionService(Workouts, Sessions, SessionExercises, Sets, ActiveState);
        History = new HistoryQueryService(Sessions, SessionExercises, Sets);
        Records = new PersonalRecordService(Sets);
        Summary = new WorkoutSummaryService(Sets);
        Maintenance = new DatabaseMaintenanceService(
            Database.Context, Exercises, Workouts, Sessions, SessionExercises, Sets, ActiveState, DataChanges, Database.FileSystem);

        Data = new TestDataBuilder(this);
    }

    public TestDatabase Database { get; } = new();

    public TestClock Clock { get; } = new(StartUtc);

    public IDataChangeNotifier DataChanges { get; }

    public ExerciseRepository Exercises { get; }

    public WorkoutRepository Workouts { get; }

    public WorkoutSessionRepository Sessions { get; }

    public SessionExerciseRepository SessionExercises { get; }

    public WorkoutSetRepository Sets { get; }

    public ActiveWorkoutStateRepository ActiveState { get; }

    public ExerciseCatalogService Catalog { get; }

    public WorkoutTemplateService Templates { get; }

    public ActiveWorkoutSessionService ActiveSessions { get; }

    public HistoryQueryService History { get; }

    public PersonalRecordService Records { get; }

    public WorkoutSummaryService Summary { get; }

    public DatabaseMaintenanceService Maintenance { get; }

    public TestDataBuilder Data { get; }

    public ValueTask DisposeAsync() => Database.DisposeAsync();
}
