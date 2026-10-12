namespace MuscleMemory.Services;

public sealed class DataChangeNotifier(IDispatcher dispatcher) : IDataChangeNotifier
{
    public event EventHandler<DataArea>? Changed;

    public void Notify(DataArea areas)
    {
        if (dispatcher.IsDispatchRequired)
        {
            dispatcher.Dispatch(() => Raise(areas));
            return;
        }

        Raise(areas);
    }

    private void Raise(DataArea areas) => Changed?.Invoke(this, areas);
}
