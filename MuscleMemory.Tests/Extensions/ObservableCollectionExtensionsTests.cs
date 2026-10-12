using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MuscleMemory.Extensions;

namespace MuscleMemory.Tests.Extensions;

public sealed class ObservableCollectionExtensionsTests
{
    private readonly Item _first = new("first");
    private readonly Item _second = new("second");
    private readonly Item _third = new("third");
    private readonly Item _fourth = new("fourth");

    [Fact]
    public void ReplaceAll_EmptyCollection_InsertsItemsInOrder()
    {
        ObservableCollection<Item> collection = [];
        var actions = RecordActions(collection);

        collection.ReplaceAll([_first, _second]);

        AssertSameItems([_first, _second], collection);
        Assert.Equal(2, actions.Count(action => action == NotifyCollectionChangedAction.Add));
        Assert.DoesNotContain(NotifyCollectionChangedAction.Remove, actions);
        Assert.DoesNotContain(NotifyCollectionChangedAction.Move, actions);
    }

    [Fact]
    public void ReplaceAll_SameItemsInSameOrder_RaisesNoChange()
    {
        ObservableCollection<Item> collection = [_first, _second, _third];
        var actions = RecordActions(collection);

        collection.ReplaceAll([_first, _second, _third]);

        AssertSameItems([_first, _second, _third], collection);
        Assert.Empty(actions);
    }

    [Fact]
    public void ReplaceAll_ReorderedItems_MovesWithoutRemovingOrInserting()
    {
        ObservableCollection<Item> collection = [_first, _second, _third];
        var actions = RecordActions(collection);

        collection.ReplaceAll([_third, _first, _second]);

        AssertSameItems([_third, _first, _second], collection);
        Assert.All(actions, action => Assert.Equal(NotifyCollectionChangedAction.Move, action));
    }

    [Fact]
    public void ReplaceAll_NoItemKept_ResetsBeforeInserting()
    {
        ObservableCollection<Item> collection = [_first, _second];
        var actions = RecordActions(collection);

        collection.ReplaceAll([_third, _fourth]);

        AssertSameItems([_third, _fourth], collection);
        Assert.Equal(
            [NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Add],
            actions);
    }

    [Fact]
    public void ReplaceAll_PartialOverlap_RemovesAbsentAndInsertsNewItems()
    {
        ObservableCollection<Item> collection = [_first, _second, _third];
        var actions = RecordActions(collection);

        collection.ReplaceAll([_second, _fourth]);

        AssertSameItems([_second, _fourth], collection);
        Assert.DoesNotContain(NotifyCollectionChangedAction.Reset, actions);
        Assert.Equal(2, actions.Count(action => action == NotifyCollectionChangedAction.Remove));
        Assert.Single(actions, action => action == NotifyCollectionChangedAction.Add);
    }

    [Fact]
    public void ReplaceAll_EmptyTarget_ClearsCollection()
    {
        ObservableCollection<Item> collection = [_first, _second];

        collection.ReplaceAll([]);

        Assert.Empty(collection);
    }

    [Fact]
    public void ReplaceAll_FewerItems_TrimsTail()
    {
        ObservableCollection<Item> collection = [_first, _second, _third];

        collection.ReplaceAll([_first]);

        AssertSameItems([_first], collection);
    }

    [Fact]
    public void ReplaceAll_EqualButDistinctInstance_ReplacesByReference()
    {
        var copy = _first with { };
        ObservableCollection<Item> collection = [_first];

        collection.ReplaceAll([copy]);

        Assert.Same(copy, Assert.Single(collection));
    }

    private static List<NotifyCollectionChangedAction> RecordActions(ObservableCollection<Item> collection)
    {
        List<NotifyCollectionChangedAction> actions = [];
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);
        return actions;
    }

    private static void AssertSameItems(IReadOnlyList<Item> expected, IReadOnlyList<Item> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        Assert.All(expected.Zip(actual), pair => Assert.Same(pair.First, pair.Second));
    }

    private sealed record Item(string Name);
}
