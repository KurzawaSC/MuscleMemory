using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class PersonalRecordsTests
{
    private const int Bench = 1;
    private const int Dips = 2;
    private const int Bodyweight = 0;

    private static readonly DateTime FirstDayUtc = new(2026, 2, 2, 7, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Find_NoSets_HasNoRecords()
    {
        var records = PersonalRecords.Find([], []);

        Assert.Equal(new ExerciseRecords(null, null, null, null), records);
        Assert.False(records.HasAny);
    }

    [Fact]
    public void Find_SeveralWeights_TakesTheHeaviest()
    {
        var heaviest = Set(2, 102.5, 3, day: 2);

        var records = PersonalRecords.Find([Set(1, 100, 5, day: 1), heaviest, Set(3, 95, 8, day: 3)], []);

        Assert.Same(heaviest, records.HeaviestWeight);
    }

    [Fact]
    public void Find_HeaviestWeightTie_PrefersMoreReps()
    {
        var moreReps = Set(2, 100, 5, day: 2);

        var records = PersonalRecords.Find([Set(1, 100, 3, day: 1), moreReps], []);

        Assert.Same(moreReps, records.HeaviestWeight);
    }

    [Fact]
    public void Find_HeaviestWeightAndRepsTie_PrefersTheEarliestDate()
    {
        var earliest = Set(5, 100, 5, day: 1);

        var records = PersonalRecords.Find([Set(4, 100, 5, day: 3), earliest, Set(6, 100, 5, day: 2)], []);

        Assert.Same(earliest, records.HeaviestWeight);
    }

    [Fact]
    public void Find_TieWithinOneSession_PrefersTheSetLoggedFirst()
    {
        var first = Set(7, 100, 5, day: 1);

        var records = PersonalRecords.Find([Set(8, 100, 5, day: 1), first], []);

        Assert.Same(first, records.HeaviestWeight);
        Assert.Same(first, records.BestOneRepMax?.Set);
    }

    [Fact]
    public void Find_SeveralRepRanges_TakesTheBestEstimatedOneRepMax()
    {
        var best = Set(3, 80, 10, day: 3);

        var records = PersonalRecords.Find([Set(1, 100, 1, day: 1), Set(2, 90, 5, day: 2), best], []);

        Assert.Equal(new OneRepMaxRecord(106.66666666666667, best), records.BestOneRepMax);
        Assert.Equal(100, records.HeaviestWeight?.Weight);
    }

    [Fact]
    public void Find_EqualEstimates_PrefersTheEarliestDate()
    {
        var earliest = Set(2, 96, 5, day: 1);

        var records = PersonalRecords.Find([Set(1, 105, 2, day: 2), earliest], []);

        Assert.Equal(new OneRepMaxRecord(112, earliest), records.BestOneRepMax);
    }

    [Fact]
    public void Find_OnlyMoreThanTwelveReps_HasNoEstimateButKeepsTheOtherRecords()
    {
        var set = Set(1, 50, 15, day: 1);

        var records = PersonalRecords.Find([set], [Volume(1, 750, day: 1)]);

        Assert.Null(records.BestOneRepMax);
        Assert.Same(set, records.HeaviestWeight);
        Assert.Equal(750, records.BestSessionVolume?.Volume);
        Assert.Null(records.MostReps);
    }

    [Fact]
    public void Find_SeveralSessions_TakesTheLargestSessionVolume()
    {
        var best = Volume(2, 2400, day: 2);

        var records = PersonalRecords.Find([Set(1, 100, 5, day: 1)], [Volume(1, 2000, day: 1), best, Volume(3, 1800, day: 3)]);

        Assert.Same(best, records.BestSessionVolume);
    }

    [Fact]
    public void Find_EqualSessionVolumes_PrefersTheEarliestDate()
    {
        var earliest = Volume(4, 2400, day: 1);

        var records = PersonalRecords.Find([Set(1, 100, 5, day: 1)], [Volume(5, 2400, day: 2), earliest]);

        Assert.Same(earliest, records.BestSessionVolume);
    }

    [Fact]
    public void Find_BodyweightOnly_HasOnlyMostReps()
    {
        var most = Set(2, Bodyweight, 15, day: 2, exercise: Dips);

        var records = PersonalRecords.Find(
            [Set(1, Bodyweight, 12, day: 1, exercise: Dips), most, Set(3, Bodyweight, 15, day: 3, exercise: Dips)],
            [Volume(1, 0, day: 1), Volume(2, 0, day: 2)]);

        Assert.Equal(new ExerciseRecords(null, null, null, most), records);
        Assert.True(records.HasAny);
    }

    [Fact]
    public void Find_BodyweightAndWeightedSets_HasNoMostReps()
    {
        var weighted = Set(2, 10, 8, day: 2, exercise: Dips);

        var records = PersonalRecords.Find([Set(1, Bodyweight, 20, day: 1, exercise: Dips), weighted], [Volume(2, 80, day: 2)]);

        Assert.Null(records.MostReps);
        Assert.Same(weighted, records.HeaviestWeight);
        Assert.Equal(new OneRepMaxRecord(12.666666666666666, weighted), records.BestOneRepMax);
    }

    [Fact]
    public void Find_SetWithoutReps_IsNotEligible()
    {
        var eligible = Set(2, 80, 5, day: 2);

        var records = PersonalRecords.Find([Set(1, 200, 0, day: 1), eligible], []);

        Assert.Same(eligible, records.HeaviestWeight);
        Assert.Same(eligible, records.BestOneRepMax?.Set);
    }

    [Fact]
    public void FindNewRecords_NoHistory_MarksNothing()
    {
        var marked = PersonalRecords.FindNewRecords([], [Set(10, 60, 5, day: 9), Set(11, 80, 5, day: 9), Set(12, 100, 5, day: 9)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_HeavierThanHistory_MarksTheSet()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 100, 5, day: 1)], [Set(10, 102.5, 3, day: 9)]);

        Assert.Equal([10], marked.Order());
    }

    [Fact]
    public void FindNewRecords_EqualToHistory_IsNeverARecord()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 100, 5, day: 1)], [Set(10, 100, 5, day: 9)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_EqualEstimateFromAnotherRepCount_IsNotARecord()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 99, 1, day: 1)], [Set(10, 90, 3, day: 9)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_LighterButHigherEstimate_MarksTheSet()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 100, 5, day: 1)], [Set(10, 95, 8, day: 9)]);

        Assert.Equal([10], marked.Order());
    }

    [Fact]
    public void FindNewRecords_MoreThanTwelveReps_IsNoEstimateRecord()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 100, 5, day: 1)], [Set(10, 95, 20, day: 9)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_SeveralSetsInOneSession_CompareWithTheRunningBest()
    {
        var marked = PersonalRecords.FindNewRecords(
            [Set(1, 100, 5, day: 1)],
            [Set(10, 102.5, 5, day: 9), Set(11, 102.5, 5, day: 9), Set(12, 105, 3, day: 9), Set(13, 100, 5, day: 9), Set(14, 100, 8, day: 9)]);

        Assert.Equal([10, 12, 14], marked.Order());
    }

    [Fact]
    public void FindNewRecords_SetsOutOfOrder_FollowTheLoggingOrder()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 100, 5, day: 1)], [Set(21, 105, 5, day: 9), Set(20, 110, 5, day: 9)]);

        Assert.Equal([20], marked.Order());
    }

    [Fact]
    public void FindNewRecords_BodyweightHistory_MarksMoreReps()
    {
        var marked = PersonalRecords.FindNewRecords(
            [Set(1, Bodyweight, 12, day: 1, exercise: Dips)],
            [Set(10, Bodyweight, 13, day: 9, exercise: Dips), Set(11, Bodyweight, 13, day: 9, exercise: Dips), Set(12, Bodyweight, 12, day: 9, exercise: Dips)]);

        Assert.Equal([10], marked.Order());
    }

    [Fact]
    public void FindNewRecords_BodyweightHistory_DoesNotMarkTheFirstWeightedSet()
    {
        var marked = PersonalRecords.FindNewRecords(
            [Set(1, Bodyweight, 12, day: 1, exercise: Dips)],
            [Set(10, 10, 8, day: 9, exercise: Dips)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_WeightedHistory_DoesNotCountBodyweightReps()
    {
        var marked = PersonalRecords.FindNewRecords(
            [Set(1, 10, 8, day: 1, exercise: Dips)],
            [Set(10, Bodyweight, 30, day: 9, exercise: Dips)]);

        Assert.Empty(marked);
    }

    [Fact]
    public void FindNewRecords_SeveralExercises_CompareEachWithItsOwnHistory()
    {
        var marked = PersonalRecords.FindNewRecords(
            [Set(1, 100, 5, day: 1), Set(2, Bodyweight, 10, day: 1, exercise: Dips)],
            [Set(10, 90, 5, day: 9), Set(11, Bodyweight, 11, day: 9, exercise: Dips), Set(12, 50, 5, day: 9, exercise: 3)]);

        Assert.Equal([11], marked.Order());
    }

    [Fact]
    public void FindNewRecords_SetsWithoutReps_AreIgnored()
    {
        var marked = PersonalRecords.FindNewRecords([Set(1, 300, 0, day: 1), Set(2, 100, 5, day: 1)], [Set(10, 200, 0, day: 9), Set(11, 102.5, 5, day: 9)]);

        Assert.Equal([11], marked.Order());
    }

    private static PerformedSet Set(int id, double weight, int reps, int day, int exercise = Bench) => new()
    {
        SetId = id,
        SessionId = day,
        ExerciseId = exercise,
        DateUtc = FirstDayUtc.AddDays(day),
        Weight = weight,
        Reps = reps
    };

    private static SessionVolume Volume(int sessionId, double volume, int day) => new()
    {
        SessionId = sessionId,
        DateUtc = FirstDayUtc.AddDays(day),
        Volume = volume
    };
}
