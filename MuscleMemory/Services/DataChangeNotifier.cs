namespace MuscleMemory.Services;

public sealed class DataChangeNotifier : IDataChangeNotifier
{
    public event EventHandler<DataArea>? Changed;

    public void Notify(DataArea areas) =>
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(this, areas));
}
