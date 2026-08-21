# MuscleMemory 🏋️

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![.NET MAUI](https://img.shields.io/badge/UI-.NET%20MAUI-512BD4?style=for-the-badge&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android)
![Version](https://img.shields.io/badge/Version-2.0.0-FF4040?style=for-the-badge)

**MuscleMemory** is an offline-first strength-training tracker for Android. You build an exercise
library, arrange exercises into reusable workout templates, then run a live session that logs every
set as you perform it. Nothing leaves the device: all data lives in a local SQLite file that you can
export or erase yourself.

---

## Features

### Exercise library

Create exercises with a name, a target muscle group (chest, back, shoulders, biceps, triceps, legs,
calves, abs, full body, cardio, other) and an equipment type (body weight, barbell, dumbbell,
machine, cable, kettlebell, band, none, other). Edit and delete with a swipe.

### Workout templates

A workout is a named, ordered list of exercises. Each entry carries its own plan: number of sets,
target reps, rest time in seconds, and a target RPE from 1 to 10. Exercises are picked from the
library and configured in a popup; the editor warns before discarding unsaved changes.

### Live workout session

Starting a workout opens a session screen that walks through the plan one exercise at a time:

- **Set logging** — enter weight and reps for the current set; sets can be edited, deleted, or the
  last one undone.
- **Rest timer** — after each logged set a countdown runs for the exercise's configured rest time
  and plays an audio cue when it ends. Rest can be skipped.
- **Last-session comparison** — the current exercise shows what you lifted for it the last time it
  was performed, so progression is visible in the moment.
- **Exercise navigation** — step back and forth through the exercises of the session; the header
  shows both exercise and set progress.
- **Total workout timer** — elapsed time is always reconstructed from the session start timestamp,
  so it stays correct across app restarts.
- **Completion summary** — finishing a session shows every logged exercise with its sets and the
  total volume lifted.

### Crash-proof active session

Active-session state (session id, start time, current exercise index, rest state and rest end time)
is written to the database on every change. If the OS kills the process mid-workout, the app
restores the session on next launch. While a workout is running, a resume banner appears on the
other tabs and the global "+" buttons are hidden.

### History

- **Workout history** — every session logged against a template, with date, duration, total volume,
  and the exercises and sets performed.
- **Exercise history** — every session in which a given exercise was performed, newest first.
- Past sessions are **editable**: sets can be added, edited or deleted, exercises can be removed,
  and an exercise can be appended to an already-finished session.

### Settings

Theme selection (System / Light / Dark), exporting the raw SQLite database through the Android
share sheet, and erasing all data behind a confirmation prompt.

---

## Tech stack

| Concern | Choice |
| --- | --- |
| Framework | .NET 10 MAUI, `net10.0-android` |
| Language | C# with nullable reference types and implicit usings enabled |
| MVVM | `CommunityToolkit.Mvvm` 8.4.2 — `[ObservableProperty]` partial properties, `[RelayCommand]` |
| UI toolkit | `CommunityToolkit.Maui` 14.1.1 — popups and converters |
| Database | `sqlite-net-e` 1.10.0-beta2 over `SourceGear.sqlite3`, with the `SQLitePCLRaw` dynamic cdecl provider |
| Audio | `Plugin.Maui.Audio` 4.0.0, for the rest-timer cue |
| XAML | Source-generated (`MauiXamlInflator=SourceGen`), compiled bindings throughout (`x:DataType`) |
| Typography | LilitaOne, bundled as an app font |

Android is the target platform: the shipping target framework is `net10.0-android`, with a minimum
supported API level of 21. The iOS, Mac Catalyst and Windows target frameworks are left over from
the MAUI template and are not maintained — the status-bar integration, for instance, is
Android-only.

---

## Requirements

- .NET 10 SDK with the `maui-android` workload installed
- Android SDK, plus an emulator or a physical device with developer mode enabled

## Build and run

```bash
git clone https://github.com/KurzawaSC/MuscleMemory.git
cd MuscleMemory
dotnet restore MuscleMemory/MuscleMemory.csproj
```

Build:

```bash
dotnet build MuscleMemory/MuscleMemory.csproj -f net10.0-android
```

Deploy and launch on a connected device or a running emulator:

```bash
dotnet build MuscleMemory/MuscleMemory.csproj -f net10.0-android -t:Run
```

> Debug Android builds use fast deployment, which keeps the managed assemblies outside the APK.
> Installing an APK with `adb install` therefore does **not** update code changes — always deploy
> with `-t:Install` or `-t:Run`.

---

## Architecture

Layering is strict and one-directional:

```text
Views (XAML)  →  ViewModels  →  Services  →  Repositories  →  DatabaseContext  →  SQLite
```

ViewModels never touch the database; they depend on interfaces resolved through constructor
injection. Every page, view model, service and repository is registered in `MauiProgram.cs`.
Code-behind files contain nothing but `InitializeComponent()` and dependency assignment, and
navigation is always triggered from a command, never from a view.

### Repositories behind interfaces

Data access is split by aggregate, each behind an interface so view models and services compose
against abstractions: `IExerciseRepository`, `IWorkoutRepository`, `IWorkoutSessionRepository`,
`ISessionExerciseRepository`, `IWorkoutSetRepository` and `IActiveWorkoutStateRepository`.

`DatabaseContext` owns a single lazily-opened `SQLiteAsyncConnection` and creates the tables on
first use — there is no migration code. Multi-row reads are batched (`GetByIdsAsync`,
`GetForSessionsAsync`, `GetForSessionExercisesAsync`) rather than issued inside a loop, and writes
that must be atomic — saving a workout with its exercises, snapshotting a session — run inside a
transaction.

### Service layer

Services hold the logic that is neither presentation nor persistence and would otherwise be
duplicated across view models:

| Service | Responsibility |
| --- | --- |
| `IWorkoutHistoryQueryService` | Assembles session, exercise and set rows into history projections |
| `IWorkoutSummaryService` | Builds the end-of-workout summary and total volume |
| `IWorkoutTimerService` | The one-second tick, plus elapsed and countdown formatting |
| `IAudioCueService` | Plays the rest-over sound |
| `ISetEditService` | The weight and reps prompts, and the delete confirmation |
| `IThemeService` | Reads and persists the theme preference, applies it to `UserAppTheme` |
| `IStatusBarService` | Keeps the Android status bar in step with the nav-bar colour |
| `INavigationStackService` | Removes a page from every tab's navigation stack |
| `IDatabaseMaintenanceService` | Exposes the database path and clears all data |

Results are returned as immutable records — `WorkoutSummary`, `WorkoutHistorySession`,
`WorkoutHistoryExercise`, `ExerciseHistoryEntry`, `SetValues` — rather than as mutable entities.

### The session-snapshot model

Logged history is deliberately independent of the templates that produced it. When a workout starts,
`SessionExerciseRepository.CreateSnapshotAsync` copies the template's exercises into
`SessionExercise` rows owned by that session, carrying the exercise name and the planned sets, reps,
rest time and target RPE as they were at that moment. Logged sets (`WorkoutSet`) attach to the
snapshot row, never to the template row.

The consequence is that editing a workout template, reordering it, or deleting it outright cannot
alter or destroy what was already recorded. A session that used "Incline Bench Press 4×8" still
reads that way a year later, even if the template has since changed to 3×12 or no longer exists.

Timestamps follow the same discipline: everything is stored and computed in UTC (`StartTimeUtc`,
`EndTimeUtc`, `BreakEndTimeUtc`) and converted to local time only for display.

### Semantic colour roles

No page contains a raw hex literal. `Resources/Styles/Colors.xaml` declares each colour as a named
role with a `…Light` / `…Dark` pair, and pages reference them through `AppThemeBinding`:

`PageBackground`, `SurfaceBackground`, `SubtleSurfaceBackground`, `TableHeaderBackground`,
`NavBarBackground`, `TabBarBackground`, `TabBarUnselected`, `PrimaryText`, `SecondaryText`,
`ChipText`, `StrokeColor`, `SubtleStroke`, `AccentRed`, `OnAccentText`, `ShadowColor`.

The red top navigation bar and the light bottom tab bar are separate roles, so they theme
independently. The theme resolves declaratively from `UserAppTheme` at startup, so a saved
preference renders correctly on a cold start rather than only after the setting is toggled.

### Extracted controls

Repeated UI is factored into bindable `ContentView` controls under `Controls/` instead of being
copied between pages:

| Control | Purpose |
| --- | --- |
| `TappableButton` | The button primitive — a `Border` with a tap gesture; styled as `PrimaryButton`, `OutlineButton`, `DangerOutlineButton`, `TextButton` or `NeutralTextButton` |
| `FloatingActionButton` | The global "+" action, hidden while a workout is active |
| `EmptyState` | Icon, message and optional detail for empty lists — a Grid overlay, not `CollectionView.EmptyView`, which collapses to zero height on Android |
| `SwipeAction` | The coloured edit/delete affordance inside a `SwipeView` |
| `Chip` | Small metadata pill — muscle group, equipment, target RPE |
| `SetTableHeader` | The shared weight/reps/set column header |
| `PopupActionBar` | The cancel/confirm pair used by every popup |
| `SettingsActionRow` | Icon-and-label row on the settings page |
| `ResumeWorkoutBanner` | The "workout in progress" banner shown on the non-workout tabs |
| `BackNavigationPage` | A `ContentPage` base that routes the Android back button to a command |

### Navigation

Three tabs — **List** (exercise library), **Workout** (templates and sessions) and **Settings** —
plus four routed pages: `AddEditWorkoutPage`, `ActiveWorkoutPage`, `ExerciseHistoryPage` and
`WorkoutHistoryPage`. Parameters travel as `IQueryAttributable` dictionaries and are never
interpolated into a route URI, so a workout named `Push & Pull` navigates correctly.

`ActiveWorkoutViewModel` is registered as a **singleton** — the active workout belongs to the app,
not to a page lifecycle, which is what lets a session survive tab switches, navigation and process
death. Because `Appearing` and `Disappearing` do not fire on Shell tab switches, any state that
depends on which page is showing is derived from `Shell.Navigated` instead.

---

## Project layout

```text
MuscleMemory/
├── Constants/            # ColorRoles, DatabaseNames, DomainDefaults, NavigationRoutes,
│                         # PreferenceKeys, QueryKeys, UiText, UiTiming
├── Controls/             # Reusable bindable ContentViews, plus BackNavigationPage
├── Data/
│   ├── DatabaseContext.cs        # Lazy SQLiteAsyncConnection and table creation
│   └── Repositories/             # One repository and interface per aggregate
├── Extensions/           # ColorExtensions (relative luminance), ObservableCollectionExtensions
├── Models/               # Exercise, Workout, WorkoutExercise, WorkoutSession, SessionExercise,
│                         # WorkoutSet, ActiveWorkoutState, ExerciseConfiguration,
│                         # CompletedExerciseSummary, MuscleGroup, EquipmentType, ThemePreference
├── Platforms/            # Android head (MainActivity, MainApplication, manifest) and template heads
├── Properties/           # launchSettings.json
├── Resources/
│   ├── AppIcon/          # logo.png
│   ├── Fonts/            # LilitaOne-Regular.ttf
│   ├── Images/           # SVG icons for tabs, empty states and settings rows
│   ├── Raw/              # BreakEnd.mp3, the rest-timer cue
│   ├── Splash/           # splash.png
│   └── Styles/           # Colors.xaml (semantic roles), Styles.xaml
├── Services/             # Service interfaces, implementations and result records
├── ViewModels/           # One per page or popup; ActiveWorkoutViewModel is a singleton
├── Views/                # Pages and popups
├── App.xaml(.cs)         # Application root; restores the saved theme
├── AppShell.xaml(.cs)    # TabBar (List / Workout / Settings) and route registration
└── MauiProgram.cs        # DI registration and app configuration
```

---

## Data and privacy

Everything is stored in `MuscleMemory.db3` inside the app's private data directory. There is no
account, no network call and no telemetry. **Export data** shares that file as-is; **Erase data**
empties every table, including any session in progress.
