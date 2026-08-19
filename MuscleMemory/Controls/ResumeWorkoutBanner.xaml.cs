using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ResumeWorkoutBanner : ContentView
{
    public static readonly BindableProperty ActiveWorkoutProperty =
        BindableProperty.Create(nameof(ActiveWorkout), typeof(ActiveWorkoutViewModel), typeof(ResumeWorkoutBanner));

    public ResumeWorkoutBanner() => InitializeComponent();

    public ActiveWorkoutViewModel? ActiveWorkout
    {
        get => (ActiveWorkoutViewModel?)GetValue(ActiveWorkoutProperty);
        set => SetValue(ActiveWorkoutProperty, value);
    }
}
