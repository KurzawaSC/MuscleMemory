using System.Collections.ObjectModel;

namespace MuscleMemory.Extensions;

public static class ObservableCollectionExtensions
{
    public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items) where T : class
    {
        List<T> target = [.. items];
        RemoveAbsent(collection, new HashSet<T>(target, ReferenceEqualityComparer.Instance));

        var present = new HashSet<T>(collection, ReferenceEqualityComparer.Instance);
        for (var index = 0; index < target.Count; index++)
        {
            PlaceAt(collection, target[index], index, present);
        }

        TrimTo(collection, target.Count);
    }

    private static void RemoveAbsent<T>(ObservableCollection<T> collection, HashSet<T> wanted) where T : class
    {
        if (!collection.Any(wanted.Contains))
        {
            collection.Clear();
            return;
        }

        for (var index = collection.Count - 1; index >= 0; index--)
        {
            if (!wanted.Contains(collection[index]))
            {
                collection.RemoveAt(index);
            }
        }
    }

    private static void PlaceAt<T>(ObservableCollection<T> collection, T item, int index, HashSet<T> present) where T : class
    {
        if (index < collection.Count && ReferenceEquals(collection[index], item))
        {
            return;
        }

        var currentIndex = present.Contains(item) ? IndexOfReference(collection, item, index + 1) : -1;
        if (currentIndex >= 0)
        {
            collection.Move(currentIndex, index);
        }
        else
        {
            collection.Insert(index, item);
        }
    }

    private static int IndexOfReference<T>(ObservableCollection<T> collection, T item, int start) where T : class
    {
        for (var index = start; index < collection.Count; index++)
        {
            if (ReferenceEquals(collection[index], item))
            {
                return index;
            }
        }

        return -1;
    }

    private static void TrimTo<T>(ObservableCollection<T> collection, int count)
    {
        while (collection.Count > count)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }
}
