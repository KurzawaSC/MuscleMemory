using MuscleMemory.Services;
using NSubstitute;

namespace MuscleMemory.Tests.Services;

public sealed class DataChangeNotifierTests
{
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly DataChangeNotifier _notifier;
    private readonly List<DataArea> _raised = [];

    public DataChangeNotifierTests()
    {
        _notifier = new DataChangeNotifier(_dispatcher);
        _notifier.Changed += (_, areas) => _raised.Add(areas);
    }

    [Fact]
    public void Notify_OnTheMainThread_RaisesChangedAtOnce()
    {
        _dispatcher.IsDispatchRequired.Returns(false);

        _notifier.Notify(DataArea.Exercises);

        Assert.Equal([DataArea.Exercises], _raised);
        _dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public void Notify_OffTheMainThread_RaisesChangedOnlyWhenDispatched()
    {
        Action? dispatched = null;
        _dispatcher.IsDispatchRequired.Returns(true);
        _dispatcher.Dispatch(Arg.Do<Action>(action => dispatched = action)).Returns(true);

        _notifier.Notify(DataArea.All);

        Assert.Empty(_raised);
        Assert.NotNull(dispatched);
        dispatched();
        Assert.Equal([DataArea.All], _raised);
    }
}
