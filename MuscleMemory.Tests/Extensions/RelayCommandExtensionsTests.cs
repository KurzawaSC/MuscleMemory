using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Extensions;
using NSubstitute;

namespace MuscleMemory.Tests.Extensions;

public sealed class RelayCommandExtensionsTests
{
    private const string WatchedProperty = "IsBusy";
    private const string OtherProperty = "Title";

    private readonly IRelayCommand _command = Substitute.For<IRelayCommand>();
    private readonly INotifyPropertyChanged _source = Substitute.For<INotifyPropertyChanged>();

    [Fact]
    public void NotifyCanExecuteChangedWhen_WatchedPropertyChanges_NotifiesCommand()
    {
        _command.NotifyCanExecuteChangedWhen(_source, WatchedProperty);

        RaisePropertyChanged(WatchedProperty);

        _command.Received(1).NotifyCanExecuteChanged();
    }

    [Fact]
    public void NotifyCanExecuteChangedWhen_OtherPropertyChanges_DoesNotNotifyCommand()
    {
        _command.NotifyCanExecuteChangedWhen(_source, WatchedProperty);

        RaisePropertyChanged(OtherProperty);

        _command.DidNotReceive().NotifyCanExecuteChanged();
    }

    [Fact]
    public void NotifyCanExecuteChangedWhen_AllPropertiesChange_DoesNotNotifyCommand()
    {
        _command.NotifyCanExecuteChangedWhen(_source, WatchedProperty);

        RaisePropertyChanged(null);

        _command.DidNotReceive().NotifyCanExecuteChanged();
    }

    [Fact]
    public void NotifyCanExecuteChangedWhen_RepeatedChanges_NotifiesEachTime()
    {
        _command.NotifyCanExecuteChangedWhen(_source, WatchedProperty);

        RaisePropertyChanged(WatchedProperty);
        RaisePropertyChanged(WatchedProperty);

        _command.Received(2).NotifyCanExecuteChanged();
    }

    private void RaisePropertyChanged(string? propertyName) =>
        _source.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(_source, new PropertyChangedEventArgs(propertyName));
}
