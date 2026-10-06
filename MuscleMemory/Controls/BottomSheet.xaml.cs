using MuscleMemory.Constants;
using MuscleMemory.Diagnostics;

namespace MuscleMemory.Controls;

[ContentProperty(nameof(SheetContent))]
public partial class BottomSheet : ContentView
{
    private const string AnimationName = nameof(BottomSheet);
    private const string InsetAnimationName = nameof(BottomSheet) + nameof(WindowLayer.BottomInset);
    private const double DismissThreshold = 0.4;
    private const double TopClearance = 24;

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(BottomSheet), false, BindingMode.TwoWay,
            propertyChanged: (bindable, _, isOpen) => ((BottomSheet)bindable).OnIsOpenChanged((bool)isOpen));

    public static readonly BindableProperty SheetContentProperty =
        BindableProperty.Create(nameof(SheetContent), typeof(View), typeof(BottomSheet));

    public static readonly BindableProperty FooterProperty =
        BindableProperty.Create(nameof(Footer), typeof(View), typeof(BottomSheet),
            propertyChanged: (bindable, _, footer) => ((BottomSheet)bindable).OnFooterChanged(footer));

    private readonly Thickness _sheetPadding;
    private double _panStartTranslation;
    private double _bottomInset;
    private IDisposable? _insetRegistration;

    public BottomSheet()
    {
        InitializeComponent();
        DetachLayer();
        _sheetPadding = Sheet.Padding;
        Sheet.SizeChanged += (_, _) => LimitBodyHeight();
        FooterHost.SizeChanged += (_, _) => LimitBodyHeight();
        BodyContent.SizeChanged += (_, _) => OnBodyLayoutChanged();
        Body.SizeChanged += (_, _) => OnBodyLayoutChanged();
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

    public View? Footer
    {
        get => (View?)GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    private void OnFooterChanged(object? footer)
    {
        FooterHost.IsVisible = footer is not null;
        LimitBodyHeight();
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
        AttachLayer();

        if (WindowLayer.TryShow(Layer))
        {
            SetBottomInset(WindowLayer.BottomInset);
            _insetRegistration = WindowLayer.ObserveBottomInset(Layer, OnBottomInsetChanged);
            return;
        }

        SetBottomInset(0);
        Content = Layer;
    }

    private void HideLayer()
    {
        _insetRegistration?.Dispose();
        _insetRegistration = null;
        this.AbortAnimation(InsetAnimationName);
        IsVisible = false;
        WindowLayer.Hide(Layer);
        DetachLayer();
    }

    private void AttachLayer()
    {
        Content = null;

        if (Layer.Parent is null)
        {
            AddLogicalChild(Layer);
        }
    }

    private void DetachLayer()
    {
        Content = null;
        RemoveLogicalChild(Layer);
    }

    private void SetBottomInset(double inset)
    {
        _bottomInset = inset;
        ApplyBottomInset(inset);
        LimitBodyHeight();
    }

    private void OnBottomInsetChanged(double inset)
    {
        if (inset == _bottomInset)
        {
            return;
        }

        _bottomInset = inset;
        LimitBodyHeight();

        new Animation(ApplyBottomInset, Sheet.Padding.Bottom - _sheetPadding.Bottom, inset)
            .Commit(this, InsetAnimationName, length: UiTiming.KeyboardInsetMilliseconds, easing: Easing.CubicOut);
    }

    private void ApplyBottomInset(double inset) => Sheet.Padding = _sheetPadding with { Bottom = _sheetPadding.Bottom + inset };

    private void LimitBodyHeight()
    {
        if (Window is not { Height: > 0 } window)
        {
            return;
        }

        var sheetMaximum = window.Height - WindowLayer.TopInset - TopClearance;
        Body.MaximumHeightRequest = Math.Max(0, sheetMaximum - SheetChromeHeight());
        UpdateBodyScrolling();
    }

    private void OnBodyLayoutChanged()
    {
        UpdateBodyScrolling();
        ScrollFocusedFieldIntoView();
    }

    private void UpdateBodyScrolling()
    {
        if (Body.Width <= 0)
        {
            return;
        }

        var contentHeight = BodyContent.Measure(Body.Width, double.PositiveInfinity).Height;
        Body.Orientation = contentHeight > Body.MaximumHeightRequest ? ScrollOrientation.Vertical : ScrollOrientation.Neither;
    }

    private double SheetChromeHeight()
    {
        var footerHeight = FooterHost.IsVisible
            ? SheetLayout.Spacing + FooterHost.Margin.VerticalThickness + Math.Max(0, FooterHost.Height)
            : 0;

        return _sheetPadding.VerticalThickness + _bottomInset + GrabHandle.HeightRequest + SheetLayout.Spacing + footerHeight;
    }

    private void ScrollFocusedFieldIntoView()
    {
        if (Body.GetVisualTreeDescendants().OfType<VisualElement>().FirstOrDefault(element => element.IsFocused) is { } focusedElement)
        {
            AppLog.LogFailures(ScrollIntoViewAsync(OwningField(focusedElement)));
        }
    }

    private async Task ScrollIntoViewAsync(Element field) =>
        await Body.ScrollToAsync(field, ScrollToPosition.MakeVisible, true);

    private static VisualElement OwningField(VisualElement focusedElement)
    {
        for (var parent = focusedElement.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is ContentView field)
            {
                return field;
            }
        }

        return focusedElement;
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
