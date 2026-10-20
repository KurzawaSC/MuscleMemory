using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class ProgressionSuggestionServiceTests : IAsyncLifetime
{
    private const int Min = 8;
    private const int Max = 12;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;
    private Workout _upper = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _push = await _app.Data.PlannedWorkoutAsync("Push Day", TestDataBuilder.Plan(_bench, Min, Max), TestDataBuilder.Plan(_press));
        _upper = await _app.Data.PlannedWorkoutAsync("Upper Day", TestDataBuilder.Plan(_press), TestDataBuilder.Plan(_bench, Min, Max));
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private async Task<SessionExercise> CurrentBenchAsync(Workout workout)
    {
        var started = await _app.Data.StartAsync(workout);
        return started.Exercises.Single(exercise => exercise.ExerciseId == _bench.Id);
    }

    private static void AssertSuggestion(SuggestionOutcome outcome, double weight, int[] targets, ProgressionSuggestion? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal((outcome, weight), (actual.Outcome, actual.Weight));
        Assert.Equal(targets, actual.TargetReps);
    }

    [Fact]
    public async Task SuggestAsync_NoRangeInThePlan_SuggestsNothing()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(1, 40, 12), new LoggedSet(1, 40, 12), new LoggedSet(1, 40, 12));
        var started = await _app.Data.StartAsync(_push);

        Assert.Null(await _app.Suggestions.SuggestAsync(started.Exercises[1]));
    }

    [Fact]
    public async Task SuggestAsync_NoFinishedHistory_SuggestsNothing()
    {
        Assert.Null(await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_LastSessionAtTheTopOfTheRange_Increases()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, Max), new LoggedSet(0, 80, Max), new LoggedSet(0, 80, Max));

        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min, Min, Min], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_LastTwoSessionsBelowTheRangeAcrossPlans_Decreases()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 7), new LoggedSet(0, 80, 6));
        await _app.Data.CompletedSessionAsync(_upper, TestApp.StartUtc + OneDay, new LoggedSet(1, 80, 7), new LoggedSet(1, 80, 7), new LoggedSet(1, 80, 5));

        AssertSuggestion(SuggestionOutcome.Decrease, 77.5, [Min, Min, Min], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_OlderSessionBelowTheRange_IsNotThePreviousSession()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 6));
        await _app.Data.CompletedSessionAsync(_upper, TestApp.StartUtc + OneDay, new LoggedSet(1, 80, 9));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + (2 * OneDay), new LoggedSet(0, 80, 7), new LoggedSet(0, 80, 7), new LoggedSet(0, 80, 6));

        AssertSuggestion(SuggestionOutcome.Hold, 80, [8, 8, 7], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_SessionWithoutTheExerciseInBetween_IsSkipped()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 7));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(1, 40, 10));
        await _app.Data.CompletedSessionAsync(_upper, TestApp.StartUtc + (2 * OneDay), new LoggedSet(0, 40, 10), new LoggedSet(1, 80, 6));

        AssertSuggestion(SuggestionOutcome.Decrease, 77.5, [Min, Min, Min], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_RangeChangedSinceTheEarlierSessions_AppliesTheCurrentSessionRange()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 9), new LoggedSet(0, 80, 9));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 80, 9), new LoggedSet(0, 80, 8));
        await _app.Workouts.UpdateWithExercisesAsync(_push, [TestDataBuilder.Plan(_bench, 10, 15), TestDataBuilder.Plan(_press)]);

        AssertSuggestion(SuggestionOutcome.Decrease, 77.5, [10, 10, 10], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(_push)));
    }

    [Fact]
    public async Task SuggestAsync_PlannedSetCount_ComesFromTheCurrentSession()
    {
        var single = await _app.Data.PlannedWorkoutAsync("Bench Single", TestDataBuilder.Plan(_bench, Min, Max, sets: 1));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, Max));

        AssertSuggestion(SuggestionOutcome.Increase, 82.5, [Min], await _app.Suggestions.SuggestAsync(await CurrentBenchAsync(single)));
    }

    [Fact]
    public async Task SuggestAsync_SuggestionDuringTheWorkout_IgnoresSetsOfTheOpenSession()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 9), new LoggedSet(0, 80, 9), new LoggedSet(0, 80, 9));
        var current = await CurrentBenchAsync(_push);
        await _app.Data.LogSetAsync(current, 100, Max);

        AssertSuggestion(SuggestionOutcome.Hold, 80, [10, 10, 10], await _app.Suggestions.SuggestAsync(current));
    }
}
