namespace MuscleMemory.Services;

public interface IDataChangeNotifier
{
    event EventHandler<DataArea>? Changed;

    void Notify(DataArea areas);
}
