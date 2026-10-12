using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class WorkoutSummaryViewModelTests : IAsyncLifetime
{
    private const string BenchPress = "Bench Press";
    private const string OverheadPress = "Overhead Press";

    private static readonly DateTime LocalStart = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Local);
    private static readonly TimeSpan Duration = new(0, 47, 5);

    private readonly TestViewModels _ui = new();
    private Workout _push = null!;

    private WorkoutSummaryViewModel Summary => _ui.ActiveWorkout.Summary;

    public async ValueTask InitializeAsync()
    {
        var bench = await _ui.App.Data.ExerciseAsync(BenchPress);
        var press = await _ui.App.Data.ExerciseAsync(OverheadPress);
        _push = await _ui.App.Data.WorkoutAsync("Push Day", bench, press);
        _ui.Clock.SetUtcNow(LocalStart.ToUniversalTime());
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public async Task Show_FinishedWorkout_FillsEveryTileFromTheLoggedSets()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await FinishAsync(Duration, (100, 10), (102.5, 8), (102.5, 8), (60, 10));

        Assert.True(Summary.IsVisible);
        Assert.Equal("47:05", Summary.TotalTimeText);
        Assert.Equal(3240, Summary.TotalVolume);
        Assert.Equal((4, "sets"), (Summary.TotalSets, Summary.TotalSetsCaption));
        Assert.Equal("Mar 05, 2026 · 09:15", Summary.DateText);
        Assert.Equal([(BenchPress, "2,640 kg"), (OverheadPress, "600 kg")], Summary.Exercises.Select(item => (item.ExerciseName, item.VolumeText)));
        Assert.Equal(["100 kg × 10", "102.5 kg × 8", "102.5 kg × 8"], Summary.Exercises[0].Sets.Select(bar => bar.Text));
        Assert.Equal([true, false, false], Summary.Exercises[0].Sets.Select(bar => bar.IsBest));
    }

    [Fact]
    public async Task Show_SingleSet_UsesTheSingularCaptionAndMarksNoBest()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await FinishAsync(Duration, (80, 5));

        Assert.Equal((1, "set"), (Summary.TotalSets, Summary.TotalSetsCaption));
        var exercise = Assert.Single(Summary.Exercises);
        Assert.Equal(BenchPress, exercise.ExerciseName);
        var bar = Assert.Single(exercise.Sets);
        Assert.Equal((false, 1.0), (bar.IsBest, bar.FillBounds.Width));
    }

    [Fact]
    public async Task Show_OverAnHour_WidensTheTime()
    {
        await FinishAsync(new TimeSpan(1, 2, 3), (80, 5));

        Assert.Equal("1:02:03", Summary.TotalTimeText);
    }

    [Theory]
    [InlineData(1, "seria")]
    [InlineData(3, "serie")]
    [InlineData(5, "serii")]
    public async Task Show_Polish_UsesPolishPluralsAndFormats(int setCount, string caption)
    {
        using var scope = new CultureScope(Cultures.Polish);

        await FinishAsync(Duration, [.. Enumerable.Repeat((102.5, 8), setCount)]);

        Assert.Equal((setCount, caption), (Summary.TotalSets, Summary.TotalSetsCaption));
        Assert.Equal("5 mar 2026 · 09:15", Summary.DateText);
        Assert.Equal("102,5 kg × 8", Summary.Exercises[0].Sets[0].Text);
    }

    [Fact]
    public async Task Show_PolishThousands_GroupsWithANoBreakSpace()
    {
        using var scope = new CultureScope(Cultures.Polish);

        await FinishAsync(Duration, (100, 10), (102.5, 8), (102.5, 8));

        Assert.Equal($"2{Cultures.PolishGroupSeparator}640 kg", Summary.Exercises[0].VolumeText);
    }

    [Fact]
    public async Task RefreshText_LanguageChangedWhileShown_ReformatsTheTexts()
    {
        using (new CultureScope(Cultures.EnglishUs))
        {
            await FinishAsync(Duration, (102.5, 8), (102.5, 8));
        }

        using var polish = new CultureScope(Cultures.Polish);
        var changed = PropertyChanges.Record(Summary);

        Summary.RefreshText();

        Assert.Equal("5 mar 2026 · 09:15", Summary.DateText);
        Assert.Equal("serie", Summary.TotalSetsCaption);
        Assert.Equal("102,5 kg × 8", Summary.Exercises[0].Sets[0].Text);
        Assert.Contains(nameof(WorkoutSummaryViewModel.TotalSetsCaption), changed);
    }

    [Fact]
    public async Task Clear_LeavingTheSummary_ResetsEveryTile()
    {
        await FinishAsync(Duration, (80, 5));

        await _ui.ActiveWorkout.ExitWorkoutCommand.ExecuteAsync(null);

        Assert.False(Summary.IsVisible);
        Assert.Equal(("00:00", 0.0, 0, string.Empty), (Summary.TotalTimeText, Summary.TotalVolume, Summary.TotalSets, Summary.DateText));
        Assert.Empty(Summary.Exercises);
    }

    [Fact]
    public void Summary_NeverShown_IsEmpty()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.False(Summary.IsVisible);
        Assert.Equal(("00:00", 0, "sets"), (Summary.TotalTimeText, Summary.TotalSets, Summary.TotalSetsCaption));
        Assert.Empty(Summary.Exercises);
    }

    private async Task FinishAsync(TimeSpan duration, params (double Weight, int Reps)[] sets)
    {
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = _push });
        await _ui.SettleAsync();
        foreach (var (weight, reps) in sets)
        {
            _ui.ActiveWorkout.SetInput.Fill(weight, reps);
            await _ui.ActiveWorkout.SaveSetCommand.ExecuteAsync(null);
        }

        _ui.Clock.Advance(duration);
        _ui.AnswerConfirmations(true);
        await _ui.ActiveWorkout.FinishWorkoutCommand.ExecuteAsync(null);
    }
}
