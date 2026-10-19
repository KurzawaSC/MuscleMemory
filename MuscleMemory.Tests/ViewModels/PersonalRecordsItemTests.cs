using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class PersonalRecordsItemTests
{
    private static readonly DateTime DateUtc = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_EveryRecord_ListsThemInAFixedOrder()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var set = Set(100, 5);

        var item = PersonalRecordsItem.Create(new ExerciseRecords(set, new OneRepMaxRecord(116.7, set), Volume(1500), Set(0, 20)));

        Assert.Equal(["100 kg × 5", "116.7 kg", "1,500 kg", "20"], item.Records.Select(record => record.ValueText));
    }

    [Fact]
    public void Create_SeveralRecords_FillTwoColumnsRowByRow()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var set = Set(100, 5);

        var item = PersonalRecordsItem.Create(new ExerciseRecords(set, new OneRepMaxRecord(116.7, set), Volume(1500), null));

        Assert.Equal([(0, 0), (0, 1), (1, 0)], item.Records.Select(record => (record.Row, record.Column)));
    }

    [Fact]
    public void Create_SomeRecords_LeavesTheOthersOut()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordsItem.Create(new ExerciseRecords(Set(50, 15), null, Volume(750), null));

        Assert.Equal(["50 kg × 15", "750 kg"], item.Records.Select(record => record.ValueText));
    }

    [Fact]
    public void Create_NoRecords_IsEmpty()
    {
        Assert.Empty(PersonalRecordsItem.Create(new ExerciseRecords(null, null, null, null)).Records);
    }

    private static PerformedSet Set(double weight, int reps) => new() { SetId = 1, SessionId = 1, ExerciseId = 1, DateUtc = DateUtc, Weight = weight, Reps = reps };

    private static SessionVolume Volume(double volume) => new() { SessionId = 1, DateUtc = DateUtc, Volume = volume };
}
