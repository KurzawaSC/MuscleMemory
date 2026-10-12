using System.Globalization;
using MuscleMemory.Data;

namespace MuscleMemory.Tests.Testing;

public static class DatabaseRows
{
    private const string Separator = "|";
    private const string SelectOrderedFormat = "SELECT * FROM \"{0}\" ORDER BY \"{1}\"";
    private const string QuickCheckSql = "PRAGMA quick_check";

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Read(string path, IEnumerable<Type> tables)
    {
        using var connection = DatabaseContext.OpenReadOnly(path);
        return tables.Select(table => connection.GetMapping(table)).ToDictionary(
            mapping => mapping.TableName,
            mapping => (IReadOnlyList<string>)
            [
                .. connection.Query(mapping, string.Format(SelectOrderedFormat, mapping.TableName, mapping.PK.Name))
                             .Select(row => string.Join(Separator, mapping.Columns.Select(column =>
                                 Convert.ToString(column.GetValue(row), CultureInfo.InvariantCulture))))
            ]);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadAll(string path) => Read(path, DatabaseContext.Tables);

    public static string QuickCheck(string path)
    {
        using var connection = DatabaseContext.OpenReadOnly(path);
        return connection.ExecuteScalar<string>(QuickCheckSql);
    }
}
