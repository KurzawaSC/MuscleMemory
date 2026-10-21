using MuscleMemory.Constants;
using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class DoubleProgressionTests
{
    private const int Min = 8;
    private const int Max = 12;
    private const int PlannedSets = 3;
    private const int LastSessionId = 2;
    private const int PreviousSessionId = 1;
    private const double Step = DomainDefaults.WeightStepInKg;

    private static readonly RepRange Range = RepRange.From(Min, Max)!;

    private static IReadOnlyList<PerformedSet> Last(params (double Weight, int Reps)[] sets) => Session(LastSessionId, sets);

    private static IReadOnlyList<PerformedSet> Previous(params (double Weight, int Reps)[] sets) => Session(PreviousSessionId, sets);

    private static IReadOnlyList<PerformedSet> Session(int sessionId, (double Weight, int Reps)[] sets) =>
        [.. sets.Select((set, index) => new PerformedSet { SetId = (sessionId * 100) + index, SessionId = sessionId, Weight = set.Weight, Reps = set.Reps })];

    private static ProgressionSuggestion? Suggest(IReadOnlyList<PerformedSet> last, IReadOnlyList<PerformedSet>? previous = null, RepRange? range = null, int plannedSets = PlannedSets) =>
        DoubleProgression.Suggest(range ?? Range, plannedSets, last, previous ?? [], Step);

    private static void AssertSuggestion(SuggestionOutcome outcome, double weight, int[] targets, ProgressionSuggestion? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal((outcome, weight), (actual.Outcome, actual.Weight));
        Assert.Equal(targets, actual.TargetReps);
    }

    [Fact]
    public void Suggest_NoRange_SuggestsNothing()
    {
        Assert.Null(DoubleProgression.Suggest(null, PlannedSets, Last((80, 12), (80, 12), (80, 12)), [], Step));
    }

    [Fact]
    public void Suggest_NoFinishedHistory_SuggestsNothing()
    {
        Assert.Null(Suggest(Last()));
    }

    [Fact]
    public void Suggest_EveryWorkingSetAtTheTopOfTheRange_IncreasesByOneStepToTheBottomOfTheRange()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min, Min, Min], Suggest(Last((80, Max), (80, Max), (80, Max))));
    }

    [Fact]
    public void Suggest_EveryWorkingSetAboveTheTopOfTheRange_Increases()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min, Min, Min], Suggest(Last((80, 15), (80, 13), (80, 20))));
    }

    [Fact]
    public void Suggest_MoreWorkingSetsThanPlannedAllAtTheTop_IncreasesForThePlannedSets()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min, Min, Min], Suggest(Last((80, Max), (80, Max), (80, Max), (80, Max))));
    }

    [Fact]
    public void Suggest_OneSetOneRepShortOfTheTop_HoldsWithOneMoreRepCappedAtTheTop()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [Max, Max, Max], Suggest(Last((80, Max), (80, Max - 1), (80, Max))));
    }

    [Fact]
    public void Suggest_FewerWorkingSetsThanPlannedAllAtTheTop_HoldsAndFillsTheMissingSetsWithTheBottom()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [Max, Max, Min], Suggest(Last((80, Max), (80, Max))));
    }

    [Fact]
    public void Suggest_InsideTheRange_HoldsWithOneMoreRepPerWorkingSetInOrder()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [9, 9, 8], Suggest(Last((80, 8), (80, 8), (80, 7))));
    }

    [Fact]
    public void Suggest_SetsLoggedOutOfOrder_TargetsFollowTheLoggingOrder()
    {
        List<PerformedSet> last =
        [
            new() { SetId = 30, SessionId = LastSessionId, Weight = 80, Reps = 7 },
            new() { SetId = 10, SessionId = LastSessionId, Weight = 80, Reps = 10 },
            new() { SetId = 20, SessionId = LastSessionId, Weight = 80, Reps = 9 }
        ];

        AssertSuggestion(SuggestionOutcome.Hold, 80, [11, 10, 8], Suggest(last));
    }

    [Fact]
    public void Suggest_MoreWorkingSetsThanPlannedNotAllAtTheTop_HoldsWithATargetForEveryWorkingSet()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [11, 11, 10, 9], Suggest(Last((80, 10), (80, 10), (80, 9), (80, 8))));
    }

    [Fact]
    public void Suggest_LighterBackOffSets_IgnoresThemAsNonWorking()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min, Min, Min], Suggest(Last((80, Max), (80, Max), (80, Max), (60, 6), (60, 5))));
    }

    [Fact]
    public void Suggest_LighterBackOffSetsInsideTheRange_DoNotCountTowardsThePlannedSets()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [Max, Max, Min], Suggest(Last((80, Max), (80, Max), (60, Max))));
    }

    [Fact]
    public void Suggest_BelowTheRangeTwiceAtTheSameWeight_DecreasesByOneStepToTheBottomOfTheRange()
    {
        AssertSuggestion(
            SuggestionOutcome.Decrease,
            77.5,
            [Min, Min, Min],
            Suggest(Last((80, 7), (80, 6), (80, 5)), Previous((80, 7), (80, 7), (80, 6))));
    }

    [Fact]
    public void Suggest_BelowTheRangeWithLighterBackOffSets_StillDecreases()
    {
        AssertSuggestion(
            SuggestionOutcome.Decrease,
            77.5,
            [Min, Min, Min],
            Suggest(Last((80, 7), (80, 7), (60, Max)), Previous((80, 7), (60, Max))));
    }

    [Fact]
    public void Suggest_BelowTheRangeOnlyOnce_Holds()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [8, 8, 7], Suggest(Last((80, 7), (80, 7), (80, 6)), Previous((80, 8), (80, 7), (80, 7))));
    }

    [Fact]
    public void Suggest_BelowTheRangeWithoutAPreviousSession_Holds()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [8, 8, 7], Suggest(Last((80, 7), (80, 7), (80, 6))));
    }

    [Fact]
    public void Suggest_OneLastSetAtTheBottomOfTheRange_Holds()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [8, 8, 9], Suggest(Last((80, 7), (80, 7), (80, Min)), Previous((80, 7), (80, 7), (80, 6))));
    }

    [Fact]
    public void Suggest_PreviousSessionAtADifferentWeight_DoesNotDecrease()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 80, [8, 7, 7], Suggest(Last((80, 7), (80, 6), (80, 6)), Previous((77.5, 7), (77.5, 7), (77.5, 6))));
    }

    [Theory]
    [InlineData(2.5)]
    [InlineData(1.25)]
    public void Suggest_DecreaseWouldReachZeroOrLess_Holds(double weight)
    {
        AssertSuggestion(SuggestionOutcome.Hold, weight, [7, 7, 6], Suggest(Last((weight, 6), (weight, 6), (weight, 5)), Previous((weight, 6), (weight, 6), (weight, 5))));
    }

    [Fact]
    public void Suggest_DecreaseLeavingTheSmallestLoad_Decreases()
    {
        AssertSuggestion(SuggestionOutcome.Decrease, 0.25, [Min, Min, Min], Suggest(Last((2.75, 6), (2.75, 6), (2.75, 5)), Previous((2.75, 6), (2.75, 6))));
    }

    [Fact]
    public void Suggest_RangeChangedSinceThePreviousSession_JudgesBothSessionsByTheCurrentRange()
    {
        var raised = RepRange.From(10, 15);

        AssertSuggestion(
            SuggestionOutcome.Decrease,
            77.5,
            [10, 10, 10],
            Suggest(Last((80, 9), (80, 9), (80, 8)), Previous((80, 9), (80, 9), (80, 9)), raised));
    }

    [Fact]
    public void Suggest_RangeLoweredBelowWhatWasDone_IncreasesByTheCurrentRange()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [5, 5, 5], Suggest(Last((80, 9), (80, 9), (80, 8)), range: RepRange.From(5, 8)));
    }

    [Fact]
    public void Suggest_Bodyweight_HoldsAtZeroWithOneMoreRepPerSetAndNoCap()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 0, [16, 14, 13], Suggest(Last((0, 15), (0, 13), (0, 12))));
    }

    [Fact]
    public void Suggest_BodyweightWithFewerSetsThanPlanned_FillsTheMissingSetsWithTheBottom()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 0, [6, Min, Min], Suggest(Last((0, 5))));
    }

    [Fact]
    public void Suggest_BodyweightBelowTheRangeTwice_HoldsInsteadOfDecreasing()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 0, [5, 5, 4], Suggest(Last((0, 4), (0, 4), (0, 3)), Previous((0, 4), (0, 4), (0, 3))));
    }

    [Fact]
    public void Suggest_BodyweightAtTheTopOfTheRange_HoldsInsteadOfIncreasing()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 0, [13, 13, 13], Suggest(Last((0, Max), (0, Max), (0, Max))));
    }

    [Fact]
    public void Suggest_WeightedSetsAmongBodyweightOnes_TreatsTheWeightedOnesAsWorking()
    {
        AssertSuggestion(SuggestionOutcome.Hold, 10, [9, 8, 8], Suggest(Last((0, 15), (10, 8), (0, 14))));
    }

    [Theory]
    [InlineData(81.25, 83.75)]
    [InlineData(62.75, 65.25)]
    [InlineData(0.25, 2.75)]
    public void Suggest_FractionalWorkingWeight_IncreasesExactlyByOneStep(double weight, double expected)
    {
        AssertSuggestion(SuggestionOutcome.Increase, expected, [Min, Min, Min], Suggest(Last((weight, Max), (weight, Max), (weight, Max))));
    }

    [Fact]
    public void Suggest_FractionalWorkingWeight_DecreasesExactlyByOneStep()
    {
        AssertSuggestion(SuggestionOutcome.Decrease, 78.75, [Min, Min, Min], Suggest(Last((81.25, 7), (81.25, 7)), Previous((81.25, 6))));
    }

    [Fact]
    public void Suggest_SinglePlannedSet_TargetsOneSet()
    {
        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min], Suggest(Last((80, Max)), plannedSets: 1));
    }

    [Fact]
    public void WorkingSets_MixedWeights_KeepsOnlyTheHeaviestInLoggingOrder()
    {
        var working = DoubleProgression.WorkingSets(Last((60, 10), (80, 8), (70, 9), (80, 7)));

        Assert.Equal([(80.0, 8), (80.0, 7)], working.Select(set => (set.Weight, set.Reps)));
    }

    [Fact]
    public void WorkingSets_NoSets_IsEmpty()
    {
        Assert.Empty(DoubleProgression.WorkingSets([]));
    }
}
