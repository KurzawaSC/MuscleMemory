using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class ExerciseRecordsTests
{
    private static readonly PerformedSet Set = new() { SetId = 1, SessionId = 1, Weight = 100, Reps = 5 };
    private static readonly SessionVolume Volume = new() { SessionId = 1, Volume = 500 };

    [Fact]
    public void Kinds_EveryRecord_ListsThemInDisplayOrder()
    {
        var records = new ExerciseRecords(Set, new OneRepMaxRecord(116, Set), Volume, Set);

        Assert.Equal([RecordKind.HeaviestWeight, RecordKind.BestOneRepMax, RecordKind.BestSessionVolume, RecordKind.MostReps], records.Kinds);
    }

    [Fact]
    public void Kinds_SomeRecords_ListsOnlyThose()
    {
        Assert.Equal([RecordKind.HeaviestWeight, RecordKind.BestSessionVolume], new ExerciseRecords(Set, null, Volume, null).Kinds);
    }

    [Fact]
    public void Kinds_NoRecords_IsEmpty()
    {
        Assert.Empty(new ExerciseRecords(null, null, null, null).Kinds);
    }
}
