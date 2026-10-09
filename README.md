# MuscleMemory

MuscleMemory is an offline strength-training tracker for Android, built with .NET MAUI. You keep a
library of exercises, combine them into workout plans, and log every set of a live session against
that plan. All data stays in a SQLite database on the device. Android is the only target platform.

## Features

The app has three tabs: **Exercises**, **Workouts** and **Settings**. Item actions open from the
"⋯" button on a row, from a long-press on the row, or, for logged sets, from a tap. Each one opens
an action sheet. Every sheet is a bottom sheet, and hardware BACK closes an open sheet before it
leaves the page.

### Exercises

- Each exercise has a name, a muscle group (10 options) and an equipment type (8 options). The name
  is required and must be unique; the check ignores case and extra spaces.
- Search by name, or filter with chips for the muscle groups that are in use.
- Actions: edit, view history, delete. Deleting an exercise also removes it from every workout that
  uses it, and the confirmation says how many workouts that is.

### Workouts

- A workout is a named list of exercises. Each entry has its own sets (1–20), target reps (1–100),
  rest time and target RPE (1–10). Rest is a 60, 90 or 120 s preset, or a custom value from 0 to
  600 s in 15 s steps.
- Exercises are added from a picker sheet with search, muscle-group filters and a shortcut to create
  a new exercise. Tap an entry or its **Edit** button to change it or remove it. Entries cannot be
  reordered.
- Leaving the editor with unsaved changes asks for confirmation.
- Actions: edit, view history, delete. Deleting a workout keeps its session history.

### Live session

- **Start** copies the plan into the session and shows one exercise at a time. You can step back
  and forth between exercises.
- Weight and reps are prefilled from the last session of that exercise, and that session's sets are
  shown as "Last: …". The weight stepper moves in 2.5 kg steps, and typed weights accept either `.`
  or `,` as the decimal separator.
- **Save set** logs a set and starts the rest timer for that exercise. **Undo** removes the last
  set. Tapping a logged set opens a sheet to edit or delete it.
- The rest timer offers **+30 s** and **Skip rest**. When it runs out, the phone vibrates and plays
  a sound.
- Once an exercise's planned sets are logged, the session moves on to the next exercise.
- **Finish** asks for confirmation and then shows a summary: duration, total volume, set count, and
  each exercise's sets as bars. A session with no logged sets is discarded instead of saved.
- Only one session can be active. While it is, the other tabs show a resume banner and hide their
  "+" buttons. Session state is written to the database on every change, so the session and a
  running rest countdown survive the app being killed.

### History

- **Workout history** lists the sessions of a workout behind date chips. A past session stays
  editable: add, edit or delete sets, remove an exercise, or add an exercise to it.
- **Exercise history** lists every session that included an exercise, newest first, with its sets
  and volume.
- Each session keeps a snapshot of the plan as it was at the start, so later edits to the workout or
  its exercises do not change what was logged.

### Settings

- Theme: System, Light or Dark.
- Export data, through the system share sheet.
- Erase all data, behind a confirmation panel.

## Privacy and data

- Everything is stored locally, in the SQLite file `MuscleMemory.db3` in the app's private data
  directory. There is no account and no network code.
- The manifest declares one permission, `VIBRATE`, and no `INTERNET`. A Release APK merges in only
  AndroidX's app-private `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. Debug builds also get
  `INTERNET`, which the .NET Android debugger needs. `android:allowBackup` is `false`.
- **Export data** writes a copy of the database (`VACUUM INTO`) to the app's cache as
  `MuscleMemory-export.db3`, then hands it to the system share sheet. The file leaves the device
  only if you pick a target. The copy is deleted the next time the app starts.
- **Erase all data** deletes every row from every table in one transaction, including an active
  session, and returns each tab to its root page. It then deletes the export copy as well.

## Tech stack

| Area | Choice |
| --- | --- |
| Runtime | .NET 10, `net10.0-android`; minimum Android API 21, target API 36 |
| UI | .NET MAUI (`Microsoft.Maui.Controls` 10.0.60), XAML compiled by the source generator (`MauiXamlInflator=SourceGen`), compiled bindings |
| MVVM | `CommunityToolkit.Mvvm` 8.4.2: `[ObservableProperty]` partial properties and `[RelayCommand]` |
| Toolkit | `CommunityToolkit.Maui` 14.1.1, used only for `EventToCommandBehavior`, `IconTintColorBehavior` and three converters (`InvertedBoolConverter`, `VariableMultiValueConverter`, `BoolToObjectConverter`) |
| Database | `sqlite-net-e` 1.11.285 on `SourceGear.sqlite3` 3.53.4, with `SQLitePCLRaw.core`, `SQLitePCLRaw.config.e_sqlite3` and `SQLitePCLRaw.provider.e_sqlite3` 3.0.5 |
| Audio | `Plugin.Maui.Audio` 4.0.0, for the rest-timer sound |
| Logging | `Microsoft.Extensions.Logging.Debug` 10.0.0, Debug builds only |
| Font | Lilita One, bundled |

## Architecture

```text
Views (XAML pages)  →  ViewModels  →  Services  →  Repositories  →  DatabaseContext  →  SQLite
       ↑                    ↑
   Controls          composed child view models
```

- **Views** are pages whose code-behind only calls `InitializeComponent` and takes its view model
  from DI. Navigation is triggered by view-model commands.
- **ViewModels** use CommunityToolkit.Mvvm and depend only on service and repository interfaces.
  They never touch the database connection or the Shell directly.
- **Services** hold the logic that is neither presentation nor storage: history projections, the
  summary, the session lifecycle, dialogs, navigation, theme, audio, haptics and the timer.
- **Repositories** (`Data/Repositories`) have one interface per aggregate. Multi-statement writes run
  in a transaction, and multi-row reads are batched rather than issued in a loop.
- **Models** are plain SQLite tables plus the enums they store. `DatabaseContext` owns one lazily
  opened `SQLiteAsyncConnection` and creates the tables. There is no migration code.
- **Controls** are reusable XAML views: buttons, bottom sheets, dialogs, steppers, set rows and
  empty states.
- **Platforms/Android** holds all Android-specific code: the activity, window-level overlays for
  sheets and dialogs, long-press, the status-bar and tab-bar painting, entry mappings and
  accessibility state. Shared code reaches it through an interface or a static class. The project
  has no `#if ANDROID`.

### Services

Every service and repository is a singleton registered in `MauiProgram.cs`:

| Registration | Responsibility |
| --- | --- |
| `IAudioManager` (`AudioManager.Current`) | Plugin.Maui.Audio player factory |
| `DatabaseContext` | Lazily opened SQLite connection and table creation |
| `IExerciseRepository` | Exercise rows |
| `IWorkoutRepository` | Workouts and their planned exercises |
| `IWorkoutSessionRepository` | Sessions: create with a plan snapshot, finish or discard |
| `ISessionExerciseRepository` | Per-session exercise snapshots |
| `IWorkoutSetRepository` | Logged sets; owns set numbering |
| `IActiveWorkoutStateRepository` | The single active-session row, written through a serial queue |
| `IHistoryQueryService` | Builds workout and exercise history from batched reads |
| `IDatabaseMaintenanceService` | Export snapshot and its deletion; erase-all in one transaction |
| `IExerciseCatalogService` | Exercise create, edit and delete, and the data-changed notification for them |
| `IStatusBarService` | Paints the status bar after each navigation and picks light or dark icons |
| `IThemeService` | Saves the theme preference and applies it to `UserAppTheme` |
| `IAudioCueService` | Plays the rest-end sound |
| `IWorkoutTimerService` | One-second tick and elapsed or countdown formatting |
| `IDialogService` | Styled confirmation and message dialogs |
| `IErrorHandler` | Runs async commands, logs failures and shows a fixed message |
| `IWorkoutSummaryService` | Builds the end-of-session summary and total volume |
| `IActiveWorkoutSessionService` | Stateless session operations: start, resume lookup, sets, finish |
| `INavigationStackService` | Pops all tabs to root; asks a tab's unsaved-changes guards |
| `INavigationService` | Shell navigation with an in-flight counter |
| `IDataChangeNotifier` | Tells tab-root lists that data changed elsewhere |
| `IHapticFeedback` (`HapticFeedback.Default`) | MAUI haptics |
| `IHapticService` | Click and rest-finished haptics |

The tab-root pages, their view models and `ActiveWorkoutPage` / `ActiveWorkoutViewModel` are
singletons. The routed pages (`AddEditWorkoutPage`, `ExerciseHistoryPage`, `WorkoutHistoryPage`)
and the sheet view models (exercise form, filter, picker and configuration) are transient.

### Key mechanisms

- **Error handler.** Every async command runs through `IErrorHandler.RunAsync`. It catches the
  exception, logs it to logcat through `AppLog`, and shows a fixed message, never the exception
  text. Work that nothing awaits goes through `ReportFailures`.
- **Navigation service.** `INavigationService.GoToAsync` wraps Shell navigation and counts
  in-flight navigations. While one is running, `InFlightNavigationBackCallback` in `MainActivity`
  swallows BACK, so a fast double press cannot pop twice or close the app mid-transition.
- **Data-changed notifier.** A service that writes data shown on another tab raises
  `IDataChangeNotifier.Notify` after the write succeeds. The singleton tab-root lists reload on the
  main thread and discard a load that a newer one has overtaken.
- **Serialized state writes.** `ActiveWorkoutStateRepository` puts every save of the active-session
  row on a `SequentialTaskQueue`. The row is written only while its session is still open, so a late
  save cannot bring back a finished session.
- **Active-workout resume.** On startup `AppShell` calls `ActiveWorkoutViewModel.LoadStateAsync`.
  It finds the open session through `IActiveWorkoutSessionService.FindResumableAsync` and restores
  the exercise index and rest countdown. All times are stored in UTC and recomputed from
  timestamps, never from a tick counter. A state row whose session has already ended is cleared.
- **Composed child view models.** Logic shared between pages lives in child view models that the
  parent creates and exposes as properties. `SetActionsViewModel` (the set sheet and editor) is used
  by both the live session and workout history. `RestTimerViewModel`, `SetInputViewModel` and
  `WorkoutSummaryViewModel` belong to the live session. Parent-specific writes go through small host
  interfaces (`ISetActionsHost`, `IRestTimerHost`).

## Project layout

```text
.
├── MuscleMemory.slnx
├── README.md
└── MuscleMemory/
    ├── MuscleMemory.csproj
    ├── MauiProgram.cs           DI registrations and app configuration
    ├── App.xaml(.cs)            Application resources; restores the saved theme; reuses one Window
    ├── AppShell.xaml(.cs)       Tab bar and routes; starts session restore
    ├── Constants/               UI text, routes, domain limits, timings, spacing, database names
    ├── Controls/                Reusable XAML controls and BackNavigationPage
    ├── Converters/              Enum display name, equality and initials converters
    ├── Data/
    │   ├── DatabaseContext.cs
    │   └── Repositories/        Repository interfaces and implementations
    ├── Diagnostics/             AppLog (logcat)
    ├── Extensions/              Helpers for collections, commands, enums, colours, time and sets
    ├── Markup/                  {markup:Gutter} XAML extension
    ├── Models/                  SQLite tables and persisted enums
    ├── Platforms/Android/       MainActivity, MainApplication, manifest and Android-only code
    │   └── Resources/           Page transition animations and colours
    ├── Resources/
    │   ├── AppIcon/  Fonts/  Images/  Raw/  Splash/
    │   └── Styles/              Colors.xaml (theme roles), Styles.xaml
    ├── Services/                Service interfaces, implementations and result records
    ├── Threading/               SequentialTaskQueue
    ├── ViewModels/              Page, sheet and child view models, and list item records
    └── Views/                   Pages
```

## Build and run

Prerequisites:

- .NET 10 SDK
- The MAUI Android workload: `dotnet workload install maui-android`
- Android SDK with platform 36, and an emulator or a device with USB debugging enabled

Build:

```bash
dotnet build MuscleMemory/MuscleMemory.csproj -f net10.0-android
```

Deploy and launch on a running emulator or a connected device:

```bash
dotnet build MuscleMemory/MuscleMemory.csproj -f net10.0-android -t:Run
```

Debug builds use fast deployment, which keeps the app's assemblies outside the APK. Installing the
APK alone with `adb install` does not update the code, so deploy with `-t:Install` or `-t:Run`.

Release build:

```bash
dotnet publish MuscleMemory/MuscleMemory.csproj -f net10.0-android -c Release
```

Release produces an Android App Bundle (`.aab`). Add `-p:AndroidPackageFormat=apk` for an APK.
Release signing is configured separately and is not part of this repository.

## Known limitations

- **Crash on a fast relaunch that does not come from the launcher.** Pressing BACK on a tab root and
  immediately starting the activity with an explicit intent (for example
  `adb shell am start -n …/crc6419b996a3bf9febbe.MainActivity`) can crash in `OnCreate`. The error is
  an `ObjectDisposedException` for `ShellToolbarTracker`, thrown from MAUI's
  `ShellFlyoutRenderer.Disconnect`. It reproduced on the second of five attempts on a Release build.
- **Jank on long chip strips.** Swiping the session-date chips of a workout with a long history
  drops frames: 15 % janky frames and a 90th-percentile frame of 40 ms on an emulator Release build,
  against 18 ms for vertical scrolling on the same page. The exercise filter chips are not
  affected.
- **Locale changes need a restart.** Changing the app language in Android settings while the app
  is running does not change number formatting, such as the decimal separator or digit grouping,
  until the process restarts.
- **No reordering.** Exercises in a workout cannot be reordered.
