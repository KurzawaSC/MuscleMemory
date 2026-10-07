using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MuscleMemory.Controls;

public class ScrollToTopOnInsertBehavior : Behavior<CollectionView>
{
    private const int FirstIndex = 0;

    private CollectionView? _list;
    private INotifyCollectionChanged? _items;
    private bool _isAtTop = true;
    private bool _isScrollPending;

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
        _list = bindable;
        bindable.Scrolled += OnScrolled;
        bindable.PropertyChanged += OnListPropertyChanged;
        ObserveItems(bindable.ItemsSource);
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        ObserveItems(null);
        bindable.PropertyChanged -= OnListPropertyChanged;
        bindable.Scrolled -= OnScrolled;
        _list = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ItemsView.ItemsSourceProperty.PropertyName)
        {
            ObserveItems(_list?.ItemsSource);
        }
    }

    private void ObserveItems(IEnumerable? itemsSource)
    {
        if (_items is not null)
        {
            _items.CollectionChanged -= OnItemsChanged;
        }

        _items = itemsSource as INotifyCollectionChanged;

        if (_items is not null)
        {
            _items.CollectionChanged += OnItemsChanged;
        }
    }

    private void OnScrolled(object? sender, ItemsViewScrolledEventArgs e) =>
        _isAtTop = e.FirstVisibleItemIndex == FirstIndex;

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e is { Action: NotifyCollectionChangedAction.Add, NewStartingIndex: FirstIndex } && _isAtTop && !_isScrollPending)
        {
            _isScrollPending = true;
            _list?.Dispatcher.Dispatch(ScrollToTop);
        }
    }

    private void ScrollToTop()
    {
        _isScrollPending = false;

        if (_list is { ItemsSource: ICollection { Count: > 0 } } list)
        {
            list.ScrollTo(FirstIndex, position: ScrollToPosition.Start, animate: false);
        }
    }
}
