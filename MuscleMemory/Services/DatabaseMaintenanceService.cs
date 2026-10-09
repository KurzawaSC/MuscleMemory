using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Diagnostics;
using MuscleMemory.Models;
using SQLite;

namespace MuscleMemory.Services;

public sealed class DatabaseMaintenanceService(
    DatabaseContext context,
    IExerciseRepository exerciseRepository,
    IWorkoutRepository workoutRepository,
    IWorkoutSessionRepository sessionRepository,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository,
    IActiveWorkoutStateRepository activeWorkoutStateRepository,
    IDataChangeNotifier dataChanges) : IDatabaseMaintenanceService
{
    private const string VacuumIntoSql = "VACUUM INTO ?";
    private const string ImportSchema = "import";
    private const string AttachImportSql = "ATTACH DATABASE ? AS " + ImportSchema;
    private const string DetachImportSql = "DETACH DATABASE " + ImportSchema;
    private const string CopyTableSqlFormat = "INSERT INTO main.\"{0}\" ({1}) SELECT {1} FROM " + ImportSchema + ".\"{0}\"";
    private const string QuotedColumnFormat = "\"{0}\"";
    private const string ColumnSeparator = ", ";
    private const string QuickCheckSql = "PRAGMA quick_check";
    private const string QuickCheckPassed = "ok";
    private const string CountExercisesSql = "SELECT COUNT(*) FROM Exercise";
    private const string CountWorkoutsSql = "SELECT COUNT(*) FROM Workout";
    private const string CountFinishedSessionsSql = "SELECT COUNT(*) FROM WorkoutSession WHERE EndTimeUtc IS NOT NULL";

    private static readonly EnumerationOptions CacheSearch = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        MatchType = MatchType.Simple
    };

    private static ReadOnlySpan<byte> SqliteHeader => "SQLite format 3\0"u8;

    private static string ExportSnapshotPath => Path.Combine(FileSystem.CacheDirectory, DatabaseNames.ExportFileName);

    private static string ImportCopyPath => Path.Combine(FileSystem.CacheDirectory, DatabaseNames.ImportFileName);

    private static IEnumerable<Type> ImportedTables => DatabaseContext.Tables.Where(table => table != typeof(ActiveWorkoutState));

    public async Task<string?> CreateExportSnapshotAsync()
    {
        if (!File.Exists(context.DatabasePath))
        {
            return null;
        }

        var snapshotPath = ExportSnapshotPath;
        File.Delete(snapshotPath);

        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(VacuumIntoSql, snapshotPath);
        return snapshotPath;
    }

    public Task<ImportPreview> PrepareImportAsync(Stream source)
    {
        var importPath = ImportCopyPath;
        return Task.Run(async () =>
        {
            await using (var copy = File.Create(importPath))
            {
                await source.CopyToAsync(copy);
            }

            return Inspect(importPath);
        });
    }

    public async Task ImportAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(AttachImportSql, ImportCopyPath);
        try
        {
            await connection.RunInTransactionAsync(transaction =>
            {
                ClearAll(transaction);
                CopyImportedTables(transaction);
            });
        }
        finally
        {
            await connection.ExecuteAsync(DetachImportSql);
        }
        dataChanges.Notify(DataArea.All);
    }

    public Task DeleteImportCopiesAsync(string pickedFilePath)
    {
        var importPath = ImportCopyPath;
        var cacheDirectory = Path.GetFullPath(FileSystem.CacheDirectory);
        return Task.Run(() =>
        {
            File.Delete(importPath);
            DeletePickerCopy(pickedFilePath, cacheDirectory);
        });
    }

    public Task DeleteTemporaryFilesAsync()
    {
        var snapshotPath = ExportSnapshotPath;
        var importPath = ImportCopyPath;
        return Task.Run(() =>
        {
            File.Delete(snapshotPath);
            File.Delete(importPath);
        });
    }

    public async Task ClearAllDataAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(ClearAll);
        dataChanges.Notify(DataArea.All);
    }

    private static void DeletePickerCopy(string pickedFilePath, string cacheDirectory)
    {
        if (FindInCache(pickedFilePath, cacheDirectory) is not { } pickerCopy)
        {
            return;
        }

        File.Delete(pickerCopy);
        if (Path.GetDirectoryName(pickerCopy) is { } folder
            && folder != cacheDirectory
            && !Directory.EnumerateFileSystemEntries(folder).Any())
        {
            Directory.Delete(folder);
        }
    }

    private static string? FindInCache(string pickedFilePath, string cacheDirectory) =>
        Directory.EnumerateFiles(cacheDirectory, Path.GetFileName(pickedFilePath), CacheSearch)
                 .FirstOrDefault(candidate => pickedFilePath.EndsWith(
                     Path.DirectorySeparatorChar + Path.GetRelativePath(cacheDirectory, candidate),
                     StringComparison.Ordinal));

    private void ClearAll(SQLiteConnection transaction)
    {
        workoutRepository.Clear(transaction);
        exerciseRepository.Clear(transaction);
        setRepository.Clear(transaction);
        sessionExerciseRepository.Clear(transaction);
        sessionRepository.Clear(transaction);
        activeWorkoutStateRepository.Clear(transaction);
    }

    private static void CopyImportedTables(SQLiteConnection transaction)
    {
        foreach (var mapping in ImportedTables.Select(table => transaction.GetMapping(table)))
        {
            var columns = string.Join(ColumnSeparator, mapping.Columns.Select(column => string.Format(QuotedColumnFormat, column.Name)));
            transaction.Execute(string.Format(CopyTableSqlFormat, mapping.TableName, columns));
        }
    }

    private static ImportPreview Inspect(string importPath)
    {
        if (!HasSqliteHeader(importPath))
        {
            return ImportPreview.Rejected(ImportFileStatus.NotBackup);
        }

        try
        {
            using var connection = DatabaseContext.OpenReadOnly(importPath);
            return InspectDatabase(connection);
        }
        catch (SQLiteException exception)
        {
            AppLog.Error(exception, nameof(Inspect));
            return ImportPreview.Rejected(ImportFileStatus.Damaged);
        }
    }

    private static bool HasSqliteHeader(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> header = stackalloc byte[SqliteHeader.Length];
        return file.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
               && header.SequenceEqual(SqliteHeader);
    }

    private static ImportPreview InspectDatabase(SQLiteConnection connection)
    {
        if (connection.ExecuteScalar<string>(QuickCheckSql) != QuickCheckPassed)
        {
            return ImportPreview.Rejected(ImportFileStatus.Damaged);
        }

        var schemaStatus = CheckSchema(connection);
        return schemaStatus == ImportFileStatus.Valid
            ? new ImportPreview(schemaStatus, ReadCounts(connection))
            : ImportPreview.Rejected(schemaStatus);
    }

    private static ImportFileStatus CheckSchema(SQLiteConnection connection)
    {
        List<(TableMapping Mapping, HashSet<string> FileColumns)> tables =
        [
            .. DatabaseContext.Tables.Select(table => connection.GetMapping(table))
                                     .Select(mapping => (mapping, ReadFileColumns(connection, mapping.TableName)))
        ];

        if (tables.All(table => table.FileColumns.Count == 0))
        {
            return ImportFileStatus.NotBackup;
        }

        return tables.All(table => table.Mapping.Columns.All(column => table.FileColumns.Contains(column.Name)))
            ? ImportFileStatus.Valid
            : ImportFileStatus.Incompatible;
    }

    private static HashSet<string> ReadFileColumns(SQLiteConnection connection, string tableName) =>
        connection.GetTableInfo(tableName)
                  .Select(column => column.Name)
                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static ImportCounts ReadCounts(SQLiteConnection connection) => new(
        connection.ExecuteScalar<int>(CountExercisesSql),
        connection.ExecuteScalar<int>(CountWorkoutsSql),
        connection.ExecuteScalar<int>(CountFinishedSessionsSql));
}
