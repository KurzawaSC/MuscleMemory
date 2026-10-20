using MuscleMemory.Progression;

namespace MuscleMemory.ViewModels;

public sealed record PersonalRecordsItem(IReadOnlyList<PersonalRecordItem> Records)
{
    public const int Columns = 2;

    public static PersonalRecordsItem Create(ExerciseRecords records) => Arranged(Items(records));

    public static PersonalRecordsItem Placeholder(IEnumerable<RecordKind> kinds) => Arranged(kinds.Select(PersonalRecordItem.Placeholder));

    private static PersonalRecordsItem Arranged(IEnumerable<PersonalRecordItem> items) =>
        new([.. items.Select((item, index) => item with { Row = index / Columns, Column = index % Columns })]);

    private static List<PersonalRecordItem> Items(ExerciseRecords records)
    {
        List<PersonalRecordItem> items = [];

        if (records.HeaviestWeight is { } heaviest)
        {
            items.Add(PersonalRecordItem.ForHeaviestWeight(heaviest));
        }

        if (records.BestOneRepMax is { } oneRepMax)
        {
            items.Add(PersonalRecordItem.ForBestOneRepMax(oneRepMax));
        }

        if (records.BestSessionVolume is { } sessionVolume)
        {
            items.Add(PersonalRecordItem.ForBestSessionVolume(sessionVolume));
        }

        if (records.MostReps is { } mostReps)
        {
            items.Add(PersonalRecordItem.ForMostReps(mostReps));
        }

        return items;
    }
}
