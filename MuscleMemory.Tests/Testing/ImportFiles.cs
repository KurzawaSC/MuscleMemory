using MuscleMemory.Data;
using MuscleMemory.Models;
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
