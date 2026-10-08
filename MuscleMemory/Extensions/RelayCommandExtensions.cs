using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MuscleMemory.Extensions;

public static class RelayCommandExtensions
{
    public static void NotifyCanExecuteChangedWhen(this IRelayCommand command, INotifyPropertyChanged source, string propertyName) =>
        source.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == propertyName)
            {
                command.NotifyCanExecuteChanged();
            }
        };
}
