using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class EmptyState : ContentView
{
    private const double DefaultSpacing = 16;

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(View), typeof(EmptyState),
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasIcon)));

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyState), string.Empty,
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasMessage)));

    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(EmptyState), string.Empty,
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasDetail)));

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyState), string.Empty,
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasAction)));

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState));

    public static readonly BindableProperty ShowsActionProperty =
        BindableProperty.Create(nameof(ShowsAction), typeof(bool), typeof(EmptyState), true,
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasAction)));

    public static readonly BindableProperty ActionStyleProperty =
        BindableProperty.Create(nameof(ActionStyle), typeof(Style), typeof(EmptyState));

    public static readonly BindableProperty SpacingProperty =
        BindableProperty.Create(nameof(Spacing), typeof(double), typeof(EmptyState), DefaultSpacing);

    public EmptyState() => InitializeComponent();

    public View? Icon
    {
        get => (View?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public bool ShowsAction
    {
        get => (bool)GetValue(ShowsActionProperty);
        set => SetValue(ShowsActionProperty, value);
    }

    public Style? ActionStyle
    {
        get => (Style?)GetValue(ActionStyleProperty);
        set => SetValue(ActionStyleProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool HasIcon => Icon is not null;

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    public bool HasDetail => !string.IsNullOrEmpty(Detail);

    public bool HasAction => ShowsAction && !string.IsNullOrEmpty(ActionText);
}
