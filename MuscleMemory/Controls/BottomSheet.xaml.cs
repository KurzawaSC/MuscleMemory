using MuscleMemory.Constants;

namespace MuscleMemory.Controls;

[ContentProperty(nameof(SheetContent))]
public partial class BottomSheet : ContentView
{
    private const string AnimationName = nameof(BottomSheet);
    private const double DismissThreshold = 0.4;

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(BottomSheet), false, BindingMode.TwoWay,
            propertyChanged: (bindable, _, isOpen) => ((BottomSheet)bindable).OnIsOpenChanged((bool)isOpen));

    public static readonly BindableProperty SheetContentProperty =
        BindableProperty.Create(nameof(SheetContent), typeof(View), typeof(BottomSheet));

    private readonly Thickness _sheetPadding;
    private double _panStartTranslation;

    public BottomSheet()
    {
        InitializeComponent();
        _sheetPadding = Sheet.Padding;
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public View? SheetContent
    {
        get => (View?)GetValue(SheetContentProperty);
        set => SetValue(SheetContentProperty, value);
    }

    private void OnIsOpenChanged(bool isOpen)
    {
        if (isOpen)
        {
            Open();
        }
        else
        {
            AnimateTo(ClosedTranslation(), UiTiming.SheetCloseMilliseconds, Easing.CubicIn, hideWhenFinished: true);
        }
    }

    private void Open()
    {
        this.AbortAnimation(AnimationName);
        Sheet.TranslationY = Window?.Height ?? 0;
        ShowLayer();

        if (Sheet.Height > 0)
        {
            AnimateOpen();
            return;
        }

        Sheet.SizeChanged += OnFirstSheetLayout;
    }

    private void ShowLayer()
    {
        IsVisible = true;

        if (ReferenceEquals(Content, Layer))
        {
            Content = null;
            Layer.Parent = this;
        }

        if (WindowLayer.TryShow(Layer))
        {
            Sheet.Padding = _sheetPadding with { Bottom = _sheetPadding.Bottom + WindowLayer.BottomInset };
            return;
        }

        Content = Layer;
    }

    private void HideLayer()
    {
        IsVisible = false;
        WindowLayer.Hide(Layer);
    }

    private void OnFirstSheetLayout(object? sender, EventArgs e)
    {
        Sheet.SizeChanged -= OnFirstSheetLayout;
        Sheet.TranslationY = Sheet.Height;
        AnimateOpen();
    }

    private void AnimateOpen()
    {
        if (IsOpen)
        {
            AnimateTo(0, UiTiming.SheetOpenMilliseconds, Easing.CubicOut, hideWhenFinished: false);
        }
    }

    private void AnimateTo(double translation, uint length, Easing easing, bool hideWhenFinished)
    {
        this.AbortAnimation(AnimationName);

        var animation = new Animation
        {
            { 0, 1, new Animation(value => Sheet.TranslationY = value, Sheet.TranslationY, translation) },
            { 0, 1, new Animation(value => Scrim.Opacity = value, Scrim.Opacity, hideWhenFinished ? 0 : 1) }
        };

        animation.Commit(this, AnimationName, length: length, easing: easing,
            finished: (_, cancelled) =>
            {
                if (!cancelled && hideWhenFinished)
                {
                    HideLayer();
                }
            });
    }

    private double ClosedTranslation() => Sheet.Height > 0 ? Sheet.Height : Window?.Height ?? 0;

    private void OnScrimTapped(object? sender, TappedEventArgs e) => IsOpen = false;

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                this.AbortAnimation(AnimationName);
                _panStartTranslation = Sheet.TranslationY;
                break;
            case GestureStatus.Running:
                Sheet.TranslationY = Math.Max(0, _panStartTranslation + e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                SettleAfterDrag();
                break;
        }
    }

    private void SettleAfterDrag()
    {
        if (Sheet.TranslationY > Sheet.Height * DismissThreshold)
        {
            IsOpen = false;
            return;
        }

        AnimateTo(0, UiTiming.SheetOpenMilliseconds, Easing.CubicOut, hideWhenFinished: false);
    }
}
