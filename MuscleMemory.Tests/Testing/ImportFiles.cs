using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Services;
using SQLite;

namespace MuscleMemory.Tests.Testing;

public static class ImportFiles
{
    private const string NotADatabase = "This is a plain text file, not a backup.";
    private const string UnrelatedTableSql = "CREATE TABLE Notes (Id integer primary key, Text varchar)";
    private const string ExtraColumnSql = "ALTER TABLE Exercise ADD COLUMN Notes varchar";
    private const string WorkoutSetWithoutRepsSql =
        "CREATE TABLE WorkoutSet (Id integer primary key autoincrement, SessionExerciseId integer, Weight float, SetNumber integer)";
    private const int PageSize = 4096;
    private const byte Garbage = 0xFF;
    private const string DropTableFormat = "DROP TABLE \"{0}\"";
    private const string CreateTableFormat = "CREATE TABLE \"{0}\" ({1})";
    private const string QuotedColumnFormat = "\"{0}\"";
    private const string ColumnSeparator = ", ";

    private static readonly string[] SchemaBeforeRepRange =
    [
        """
        CREATE TABLE "ActiveWorkoutState" (
        "Id" integer primary key not null ,
        "SessionId" integer ,
        "StartTimeUtc" bigint ,
        "CurrentExerciseIndex" integer ,
        "IsResting" integer ,
        "BreakEndTimeUtc" bigint ,
        "RestDurationSeconds" integer )
        """,
        """
        CREATE TABLE "Exercise" (
        "Id" integer primary key autoincrement not null ,
        "Name" varchar(100) ,
        "TargetMuscleGroup" integer ,
        "Equipment" integer )
        """,
        """
        CREATE TABLE "SessionExercise" (
        "Id" integer primary key autoincrement not null ,
        "WorkoutSessionId" integer ,
        "ExerciseId" integer ,
        "ExerciseName" varchar ,
        "Order" integer ,
        "PlannedSets" integer ,
        "PlannedReps" integer ,
        "BreakTimeInSeconds" integer ,
        "TargetRPE" integer )
        """,
        """
        CREATE TABLE "Workout" (
        "Id" integer primary key autoincrement not null ,
        "Name" varchar )
        """,
        """
        CREATE TABLE "WorkoutExercise" (
        "Id" integer primary key autoincrement not null ,
        "WorkoutId" integer ,
        "ExerciseId" integer ,
        "Order" integer ,
        "Sets" integer ,
        "Reps" integer ,
        "BreakTimeInSeconds" integer ,
        "ExerciseName" varchar ,
        "TargetRPE" integer )
        """,
        """
        CREATE TABLE "WorkoutSession" (
        "Id" integer primary key autoincrement not null ,
        "WorkoutId" integer ,
        "WorkoutName" varchar ,
        "StartTimeUtc" bigint ,
        "EndTimeUtc" bigint )
        """,
        """
        CREATE TABLE "WorkoutSet" (
        "Id" integer primary key autoincrement not null ,
        "SessionExerciseId" integer ,
        "Weight" float ,
        "Reps" integer ,
        "SetNumber" integer )
        """,
        "CREATE INDEX \"SessionExercise_ExerciseId\" on \"SessionExercise\"(\"ExerciseId\")",
        "CREATE INDEX \"SessionExercise_WorkoutSessionId\" on \"SessionExercise\"(\"WorkoutSessionId\")",
        "CREATE INDEX \"WorkoutExercise_ExerciseId\" on \"WorkoutExercise\"(\"ExerciseId\")",
        "CREATE INDEX \"WorkoutExercise_WorkoutId\" on \"WorkoutExercise\"(\"WorkoutId\")",
        "CREATE INDEX \"WorkoutSession_WorkoutId\" on \"WorkoutSession\"(\"WorkoutId\")",
        "CREATE INDEX \"WorkoutSet_SessionExerciseId\" on \"WorkoutSet\"(\"SessionExerciseId\")"
    ];

    private static readonly string[] RowsBeforeRepRange =
    [
        "INSERT INTO Exercise (Name, TargetMuscleGroup, Equipment) VALUES ('Bench Press', 1, 1), ('Pull-up', 2, 4)",
        "INSERT INTO Workout (Name) VALUES ('Push Day')",
        "INSERT INTO WorkoutExercise (WorkoutId, ExerciseId, \"Order\", Sets, Reps, BreakTimeInSeconds, ExerciseName, TargetRPE) VALUES (1, 1, 0, 3, 8, 90, 'Bench Press', 8), (1, 2, 1, 4, 6, 120, 'Pull-up', 9)",
        "INSERT INTO WorkoutSession (WorkoutId, WorkoutName, StartTimeUtc, EndTimeUtc) VALUES (1, 'Push Day', 639030000000000000, 639030027000000000), (1, 'Push Day', 639040000000000000, NULL)",
        "INSERT INTO SessionExercise (WorkoutSessionId, ExerciseId, ExerciseName, \"Order\", PlannedSets, PlannedReps, BreakTimeInSeconds, TargetRPE) VALUES (1, 1, 'Bench Press', 0, 3, 8, 90, 8), (1, 2, 'Pull-up', 1, 4, 6, 120, 9), (2, 1, 'Bench Press', 0, 3, 8, 90, 8)",
        "INSERT INTO WorkoutSet (SessionExerciseId, Weight, Reps, SetNumber) VALUES (1, 80, 8, 1), (1, 82.5, 7, 2), (2, 0, 10, 1), (3, 80, 8, 1)",
        "INSERT INTO ActiveWorkoutState (Id, SessionId, StartTimeUtc, CurrentExerciseIndex, IsResting, BreakEndTimeUtc, RestDurationSeconds) VALUES (1, 2, 639040000000000000, 0, 0, NULL, 0)"
    ];

    public static ImportCounts CountsBeforeRepRange { get; } = new(Exercises: 2, Workouts: 1, Sessions: 1);

    public static void PlainText(string path) => File.WriteAllText(path, NotADatabase);

    public static void Empty(string path) => File.WriteAllBytes(path, []);

    public static void UnrelatedDatabase(string path)
    {
        using var connection = Create(path);
        connection.Execute(UnrelatedTableSql);
    }

    public static void WithoutTable(string path, Type missing)
    {
        using var connection = Create(path);
        connection.CreateTables(CreateFlags.None, [.. DatabaseContext.Tables.Where(table => table != missing)]);
    }

    public static void WithoutRepsColumn(string path)
    {
        using var connection = Create(path);
        connection.CreateTables(CreateFlags.None, [.. DatabaseContext.Tables.Where(table => table != typeof(WorkoutSet))]);
        connection.Execute(WorkoutSetWithoutRepsSql);
    }

    public static void FromBeforeRepRange(string path)
    {
        using var connection = Create(path);
        foreach (var statement in SchemaBeforeRepRange.Concat(RowsBeforeRepRange))
        {
            connection.Execute(statement);
        }
    }

    public static void WithoutColumn(string path, Type table, string column)
    {
        using var connection = Create(path);
        connection.CreateTables(CreateFlags.None, [.. DatabaseContext.Tables]);
        var mapping = connection.GetMapping(table);
        var keptColumns = mapping.Columns.Where(kept => kept.Name != column).Select(kept => string.Format(QuotedColumnFormat, kept.Name));
        connection.Execute(string.Format(DropTableFormat, mapping.TableName));
        connection.Execute(string.Format(CreateTableFormat, mapping.TableName, string.Join(ColumnSeparator, keptColumns)));
    }

    public static void WithExtraTableAndColumn(string path)
    {
        using var connection = Create(path);
        connection.CreateTables(CreateFlags.None, [.. DatabaseContext.Tables]);
        connection.Execute(UnrelatedTableSql);
        connection.Execute(ExtraColumnSql);
    }

    public static void Corrupt(string path)
    {
        using var file = File.OpenWrite(path);
        file.Position = PageSize;
        file.Write(Enumerable.Repeat(Garbage, PageSize).ToArray());
    }

    private static SQLiteConnection Create(string path)
    {
        SQLitePCL.Batteries_V2.Init();
        return new SQLiteConnection(path);
    }
}
