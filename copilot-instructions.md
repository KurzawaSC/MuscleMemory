# Role & Context
You are an expert .NET MAUI mobile developer. We are building a workout tracker app named "MuscleMemory".
The app uses .NET MAUI, C#, XAML, and SQLite for local storage. 
Always follow the architectural rules and UI paradigms defined below.

# Architecture: MVVM & CommunityToolkit
1. Strictly use `CommunityToolkit.Mvvm` for ViewModels.
2. ViewModels must inherit from `ObservableObject`.
3. Commands must use the `[RelayCommand]` attribute. Do not write manual ICommand properties.
4. **AOT Compatibility Rule (CRITICAL):** For observable properties, strictly use the new partial property syntax with default values to avoid AOT warnings and nullability errors.
   - Good: `[ObservableProperty] public partial string MyText { get; set; } = string.Empty;`
   - Good: `[ObservableProperty] public partial Workout CurrentWorkout { get; set; } = null!;`
   - BAD: `[ObservableProperty] private string _myText;` (Do NOT use private backing fields).

# Database & Storage
1. Use `sqlite-net-pcl` for database operations.
2. The main database access class is `DatabaseContext.cs`. 
3. Always use asynchronous methods (`ToListAsync`, `InsertAsync`, `DeleteAllAsync`).
4. Always call `await InitAsync();` at the beginning of any repository method.

# UI / XAML Rules (MAUI specific bug-fixes)
1. **Empty Views:** NEVER use `<CollectionView.EmptyView>`. It is bugged on Android and collapses to 0 height. Instead, use a Grid overlay with a `VerticalStackLayout` and bind its `IsVisible` property to an `IsEmpty` boolean in the ViewModel.
2. **Touch Events:** When wrapping an `<Image>` inside a clickable area (like a Border or Grid with a GestureRecognizer/Button), ALWAYS add `InputTransparent="True"` to the `<Image>` so it doesn't steal the tap event.
3. **Borders:** Prefer `<Border>` with `<RoundRectangle>` over the deprecated `<Frame>`.
4. **Styling:** The app uses a custom font named `LilitaOne`. Apply it via `FontFamily="LilitaOne"`.
5. **Layouts:** Prefer `Grid` and `VerticalStackLayout` / `HorizontalStackLayout`. Avoid nested ScrollViews.

# Current App State & Features
- Implemented: Exercises List (CRUD), Workout List (CRUD), Settings (Export/Clear DB).
- Active Workout: A timer, exercise selection carousel, and saving sets to the DB are implemented.
- The next planned features might include: Swipe-to-delete sets, a Rest Timer after saving a set, and a Workout History/Calendar tab.