using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ListLoadStateTests
{
    private readonly ListLoadState _state = new();

    [Fact]
    public void New_BeforeFirstLoad_ReportsNeitherEmptyNorItems()
    {
        Assert.False(_state.IsEmpty);
        Assert.False(_state.HasItems);
    }

    [Fact]
    public void Complete_NoItems_ReportsEmpty()
    {
        _state.Complete(0);

        Assert.True(_state.IsEmpty);
        Assert.False(_state.HasItems);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public void Complete_SomeItems_ReportsItems(int itemCount)
    {
        _state.Complete(itemCount);

        Assert.False(_state.IsEmpty);
        Assert.True(_state.HasItems);
    }

    [Fact]
    public void Complete_ItemsThenNone_SwitchesToEmpty()
    {
        _state.Complete(3);

        _state.Complete(0);

        Assert.True(_state.IsEmpty);
        Assert.False(_state.HasItems);
    }

    [Fact]
    public void Complete_StateChanges_RaisesOnlyChangedProperties()
    {
        List<string?> changed = [];
        _state.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _state.Complete(2);

        Assert.Equal([nameof(ListLoadState.HasItems)], changed);
        changed.Clear();

        _state.Complete(0);

        Assert.Equal([nameof(ListLoadState.IsEmpty), nameof(ListLoadState.HasItems)], changed);
    }
}
