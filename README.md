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
- An entry can also have an optional **rep range** ("Reps from" and "Reps to", 1 ≤ from ≤ to ≤ 30).
  Both fields are filled or both are left empty; with a range, the live session suggests the next
  weight and reps (see [Progression suggestions](#progression-suggestions)).
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
- For an entry with a rep range, a suggestion line such as "Increase: 82.5 kg × 8" or
  "Hold: 80 kg × 9 / 9 / 8" appears under the target, and the input starts with the suggested weight
  and the first set's target reps instead of the last session's values. Once you edit the input or
  log a set, the usual prefill takes over again.
- **Save set** logs a set and starts the rest timer for that exercise. **Undo** removes the last
  set. Tapping a logged set opens a sheet to edit or delete it.
- A logged set that beats the exercise's personal record carries a **PR** badge (see
  [Personal records](#personal-records)). Editing, deleting or undoing a set recomputes the badges.
- The rest timer offers **+30 s** and **Skip rest**. When it runs out, the phone vibrates and plays
  a sound.
- Once an exercise's planned sets are logged, the session moves on to the next exercise.
- **Finish** asks for confirmation and then shows a summary: duration, total volume, set count, and
  each exercise's sets as bars. When the session set new records, a tile counts them and the
  exercises that set them carry the **PR** badge. A session with no logged sets is discarded
  instead of saved.
- Only one session can be active. While it is, the other tabs show a resume banner and hide their
  "+" buttons. Session state is written to the database on every change, so the session and a
  running rest countdown survive the app being killed.

### History

- **Workout history** lists the sessions of a workout behind date chips. A past session stays
  editable: add, edit or delete sets, remove an exercise, or add an exercise to it.
- **Exercise history** opens with the exercise's personal records, one tile per record with the
  date it was set, then a progress chart (see [Progress chart](#progress-chart)), and then lists
  every session that included the exercise, newest first, with its sets and volume. The sessions
  appear first; the record tiles and the chart are laid out empty in the same place and filled in
  when they have been computed, so nothing on the page moves when they arrive.
- Each session keeps a snapshot of the plan as it was at the start, so later edits to the workout or
  its exercises do not change what was logged.

### Personal records

Records are computed from the logged sets whenever they are shown and are never stored, so editing
or deleting a set can never leave a stale record behind.

- **Estimated one-rep max (e1RM)** uses the Epley formula: e1RM = w · (1 + r / 30) for
  2 ≤ r ≤ 12 repetitions at weight w, and e1RM = w for r = 1. It is undefined for r > 12, for
  w ≤ 0 and for r ≤ 0. It is computed as w · (30 + r) / 30, the same value written so that equal
  estimates compare exactly equal, and it is rounded only for display, to one decimal place.
- **Eligible sets** are logged sets with at least one repetition, from finished sessions. Sets of a
  session still in progress never count towards a record.
- **Heaviest weight**: the greatest w over sets with w > 0, shown as "w × r". Ties go to more
  repetitions, then to the earlier session, then to the set logged first.
- **Best e1RM**: the greatest estimate. Ties go to the earlier session, then to the set logged
  first.
- **Best session volume**: the greatest Σ (w · r) over the exercise's sets within one session.
  Ties go to the earlier session.
- **Most reps**: the greatest r in one set. It is shown only for an exercise whose sets are all
  bodyweight sets, which the app stores with a weight of 0; such an exercise has no other record.
- **New records during a workout**: a set is a new record when it strictly exceeds the running
  best of heaviest weight, e1RM, or, for a bodyweight exercise, most reps. The running best starts
  from finished sessions and is raised by each earlier set of the current session in the order the
  sets were logged. A value equal to the running best is never a record, and a record type with no
  value in the finished history marks nothing, so an exercise done for the first time shows no
  badges.

### Progress chart

The chart on Exercise history plots one point per finished session, at the session's local start
date, using the same eligible sets and the same e1RM as the records.

- **Series.** *e1RM*: the greatest e1RM among the session's sets; a session with no defined e1RM is
  left out. *Volume*: Σ (w · r) over the session's sets; a session with volume 0 is left out.
  *Reps*: the greatest r in the session, offered only for an exercise whose sets are all bodyweight
  sets, which has no other series.
- **Range**: the last 3, 6 or 12 months, counted back from today's local date (a session on the
  first day of the range counts), or All. All is the default.
- **Trend**: an ordinary least-squares line of the value against time in days over the points in
  the range, with slope b = Σ (tᵢ − t̄)(yᵢ − ȳ) / Σ (tᵢ − t̄)². It is shown as the rate per 30 days
  in the series' unit, rounded to one decimal place, for example "+1.2 kg / month"; a rate that
  rounds to zero is shown as "0". It needs at least 3 points spanning at least 14 days; otherwise
  the card says there are not enough sessions for a trend.
- With fewer than 2 points in the range the card shows a message instead of the chart, and an
  exercise with no eligible sets has no chart at all.
- Tapping or dragging sideways across the chart selects the nearest session by date and shows its
  value and date above the chart; tapping where no session is near clears the selection. A vertical
  swipe that starts on the chart scrolls the page and selects nothing.
- For screen readers the chart is a single element that reads the series, the range, the first and
  last values with their dates, the best value and the trend.

### Progression suggestions

For a plan entry with a rep range [m, M] and n planned sets, the live session suggests the next
weight and reps by double progression: add reps inside the range, then add weight and start again
at the bottom. The suggestion is computed when the exercise is shown and never stored.

The input is the exercise's eligible sets (as for records) from the most recent finished session
that contains logged sets of it — the **last session**, from any workout — and from the finished
session before that, the **previous session**. The working weight W is the heaviest weight in the
last session, and the **working sets** are its sets at W, in the order they were logged; lighter
back-off sets are ignored. Δ is the weight step, 2.5 kg.

| # | Condition (checked in this order) | Suggestion |
|---|---|---|
| 1 | No rep range, or no finished session with the exercise | none |
| 2 | W = 0 (bodyweight) | Hold at 0, target r + 1 for each working set (no cap) |
| 3 | At least n working sets, and every one has r ≥ M | Increase to W + Δ, m reps × n sets |
| 4 | Every working set has r < m, the previous session's working weight is also W with every working set r < m, and W − Δ > 0 | Decrease to W − Δ, m reps × n sets |
| 5 | Anything else | Hold at W, target min(r + 1, M) for each working set in order |

In both Hold rows, when fewer than n working sets were done, the missing sets get a target of m.
The range is always the current one, also when judging the previous session. History and summary
screens show no suggestion. For screen readers the line is one sentence, for example "Suggestion:
keep 80 kg, sets of 9, 9 and 8 reps."

### Settings

- Theme: System, Light or Dark.
- Language: System default, or any language the app contains, listed by its native name. The choice
  applies at once: every tab returns to its root, lists reload, and a running session keeps going
  with its text re-formatted. System default follows the Android language when the app has it and
  falls back to English otherwise. Numbers and dates use the system's regional format when it
  matches the chosen language, and that language's default region otherwise. The app ships in
  English and Polish.
- Export data, through the system share sheet.
- Import data from a file made by Export data. It replaces all current data after a confirmation
  that shows the file's exercise, workout and session counts, and it is blocked while a workout is
  active.
- Erase all data, behind a confirmation panel.

## Privacy and data

- Everything is stored locally, in the SQLite file `MuscleMemory.db3` in the app's private data
  directory. There is no account and no network code. The schema only ever grows (new nullable
  columns or tables), and an existing database gains new columns on startup with its rows intact.
- The manifest declares one permission, `VIBRATE`, and no `INTERNET`. A Release APK merges in only
  AndroidX's app-private `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. Debug builds also get
  `INTERNET`, which the .NET Android debugger needs. `android:allowBackup` is `false`.
- **Export data** writes a copy of the database (`VACUUM INTO`) to the app's cache as
  `MuscleMemory-export.db3`, then hands it to the system share sheet. The file leaves the device
  only if you pick a target. The copy is deleted the next time the app starts.
- **Import data** reads the file you choose in the system file picker, so it needs no permission.
  The file is copied to the app's cache as `MuscleMemory-import.db3` and checked before anything
  changes: the SQLite header, `PRAGMA quick_check`, and every table and column the app uses. A
  column added after a release (today the rep range) may be missing, so backups from older versions
  still import, with those values left empty. A file that is not a backup, is damaged, or lacks any
  other column is rejected with a message. After you confirm, all current data is replaced in one transaction; an active-session
  row in the file is not imported. The temporary copy, and the copy the file picker makes in the
  cache, are deleted when the import ends, and any leftover temporary copy is deleted the next time
  the app starts.
- **Erase all data** deletes every row from every table in one transaction, including an active
  session, and returns each tab to its root page. It then deletes the export copy as well.

## Tech stack

| Area | Choice |
| --- | --- |
| Runtime | .NET 10, `net10.0-android`; minimum Android API 21, target API 36 |
| UI | .NET MAUI (`Microsoft.Maui.Controls` 10.0.110), XAML compiled by the source generator (`MauiXamlInflator=SourceGen`), compiled bindings |
| MVVM | `CommunityToolkit.Mvvm` 8.4.2: `[ObservableProperty]` partial properties and `[RelayCommand]` |
| Toolkit | `CommunityToolkit.Maui` 15.0.1, used only for `EventToCommandBehavior`, `IconTintColorBehavior` and three converters (`InvertedBoolConverter`, `VariableMultiValueConverter`, `BoolToObjectConverter`) |
| Database | `sqlite-net-e` 1.11.285 on `SourceGear.sqlite3` 3.53.4, with `SQLitePCLRaw.core`, `SQLitePCLRaw.config.e_sqlite3` and `SQLitePCLRaw.provider.e_sqlite3` 3.0.5 |
| Audio | `Plugin.Maui.Audio` 4.0.0, for the rest-timer sound |
| Logging | `Microsoft.Extensions.Logging.Debug` 10.0.12, Debug builds only |
| Font | Barlow Semi Condensed ExtraBold, bundled (SIL Open Font License 1.1) |

## Architecture

```text
Views (XAML pages)  →  ViewModels  →  Services  →  Repositories  →  DatabaseContext  →  SQLite
       ↑                    ↑
   Controls          composed child view models
```

- **Views** are pages whose code-behind only calls `InitializeComponent` and takes its view model
  from DI. Navigation is triggered by view-model commands.
- **ViewModels** use CommunityToolkit.Mvvm and depend only on service interfaces. They never touch
  a repository, the database connection or the Shell directly; the architecture tests enforce the
  layering.
- **Services** hold the logic that is neither presentation nor storage: history projections, the
  summary, personal records, the session lifecycle, dialogs, navigation, theme, audio, haptics and
  the timer.
- **Progression** holds pure domain rules with no dependencies: the Epley estimate and the record
  definitions. Repositories return its value records, and services apply its rules.
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
| `IPersonalRecordService` | An exercise's personal records, and the sets of a session that set new ones |
| `IProgressionSuggestionService` | The double-progression suggestion for a session exercise with a rep range, from its last two sessions |
| `IDatabaseMaintenanceService` | Export snapshot; import with validation, replacing all data in one transaction; deletion of temporary copies; erase-all in one transaction |
| `IExerciseCatalogService` | The exercise list, exercise create, edit and delete with the data-changed notification for them, and how many workouts use an exercise |
| `IWorkoutTemplateService` | Workout plans: list, load, save and delete |
| `IStatusBarService` | Paints the status bar after each navigation and picks light or dark icons |
| `IThemeService` | Saves the theme preference and applies it to `UserAppTheme` |
| `ILocalizationService` | Finds the available languages, saves the language choice, applies it to the default cultures and raises `LanguageChanged` |
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
- **Localization.** Every user-visible string is a key in `Resources/Strings/AppResources.resx`.
  MSBuild generates the typed `AppResources` accessor on every build, so C# keys are checked by the
  compiler. XAML reads text through `{loc:Text Key}`, a binding to the `LocalizedText` indexer that
  updates when the language changes; view models re-format the text they build on
  `ILocalizationService.LanguageChanged`. The language is applied as the process default culture,
  so it holds on every thread and across async continuations.
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
├── .github/workflows/tests.yml  CI: build, format check and unit tests
├── MuscleMemory.Tests/          Unit tests (see Testing)
└── MuscleMemory/
    ├── MuscleMemory.csproj
    ├── MauiProgram.cs           DI registrations and app configuration
    ├── App.xaml(.cs)            Application resources; restores the saved language and theme; reuses one Window
    ├── AppShell.xaml(.cs)       Tab bar and routes; starts session restore
    ├── Constants/               Routes, domain limits, timings, spacing, display formats, database names
    ├── Controls/                Reusable XAML controls and BackNavigationPage
    ├── Converters/              Enum display name, equality and initials converters
    ├── Data/
    │   ├── DatabaseContext.cs
    │   └── Repositories/        Repository interfaces and implementations
    ├── Diagnostics/             AppLog (logcat)
    ├── Extensions/              Helpers for collections, commands, enums, colours, time and sets
    ├── Localization/            {loc:Text} extension, LocalizedText, plural rules
    ├── Markup/                  {markup:Gutter} XAML extension
    ├── Models/                  SQLite tables and persisted enums
    ├── Platforms/Android/       MainActivity, MainApplication, manifest and Android-only code
    │   └── Resources/           Page transition animations and colours
    ├── Progression/             Pure domain rules: the Epley e1RM, personal records, progress series and double progression
    ├── Resources/
    │   ├── AppIcon/  Fonts/  Images/  Raw/  Splash/
    │   ├── Strings/             AppResources.resx (English) and one AppResources.<code>.resx per language
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

## Testing

[![Tests](https://github.com/KurzawaSC/MuscleMemory/actions/workflows/tests.yml/badge.svg?branch=master)](https://github.com/KurzawaSC/MuscleMemory/actions/workflows/tests.yml)

The unit tests are the `MuscleMemory.Tests` project, xUnit v3 with NSubstitute, running on
`net10.0`. The app project has a second target framework, `net10.0`, that exists only for them: it
compiles the shared code without the Android-specific parts, and nothing ships from it.

Running them needs the .NET 10 SDK and the `maui-android` workload. The `net10.0` target itself
needs no workload, but restoring the app project also evaluates its Android target. No Android SDK
or device is needed. From the repository root:

```bash
dotnet test MuscleMemory.Tests/MuscleMemory.Tests.csproj
```

With coverage, which writes a Cobertura `coverage.cobertura.xml` under
`MuscleMemory.Tests/TestResults/`:

```bash
dotnet test MuscleMemory.Tests/MuscleMemory.Tests.csproj --collect:"XPlat Code Coverage"
```

The test folders mirror the app's:

| Folder | Contents |
| --- | --- |
| `Architecture/` | Layering and dependency-injection rules, checked by reflection over the app assembly |
| `Constants/`, `Converters/`, `Extensions/`, `Threading/` | Formatting, conversion and helper logic |
| `Data/` | Every repository, against a real SQLite file |
| `Localization/` | Plural rules and formatting, and the consistency of the resource files |
| `Progression/` | The e1RM formula, record selection, new-record detection and the double-progression decision table, with exact expected values |
| `Services/` | The services, over a real temporary database |
| `ViewModels/` | The view models, over the real services |
| `Testing/` | Shared helpers: temporary database, fake clock, culture scopes, data builders |

What the unit tests cover:

- **Logic:** number, date and duration formatting under fixed cultures, plural rules, parsing and
  validation.
- **Data:** each test gets its own SQLite file in a temporary folder, filled through the app's own
  services. Transactions are tested by forcing a statement to fail and checking that nothing was
  half-written. Set numbering, the session snapshot, import validation and the startup cleanup are
  covered.
- **View models:** what a page shows and does: bound properties, change notifications, command
  availability, navigation targets, dialogs and the rows written. Only dialogs, navigation, haptics,
  audio, the status bar, the dispatcher and the share, file-picker and app-info APIs are
  substituted, and the clock is a fake one, so no test waits or depends on the machine's culture or
  time zone.
- **Architecture:** view models do not depend on the data layer, repositories are used only by
  services and the data layer (and registered in `MauiProgram`), and every constructor parameter of
  every registered type is itself registered.
- **Localization:** see [Adding a language](#adding-a-language).

What is verified on a device or emulator instead, because a unit test cannot see it:

- XAML layout, bindings and rendering in both themes, and text fit at large font scales.
- Shell navigation, hardware BACK and page lifecycle.
- The Android code under `Platforms/Android`: sheets and dialogs on the window layer, long-press,
  status-bar and tab-bar painting, decimal input and TalkBack state.
- Audio, vibration, the file picker and the share sheet.
- Recovery of a session after the process is killed, and performance.

Continuous integration runs on every push and pull request to `master`, on Ubuntu with the pinned
.NET SDK and the `maui-android` workload. It builds the app's `net10.0` target and the tests with
warnings as errors, checks the formatting of both projects with `dotnet format --verify-no-changes`,
runs the tests with coverage and uploads the TRX results and the coverage report as the
`test-results` artifact. It does not build the Android app.

## Adding a language

1. Copy `MuscleMemory/Resources/Strings/AppResources.resx` to `AppResources.<code>.resx` in the same
   folder, where `<code>` is the language (`de`, `pl`) or a regional variant (`pt-PT`), and
   translate every value. At run time a missing key would fall back to English, but the
   localization tests fail on it.
2. Keep every `{0}`-style placeholder. Quotation marks and the order of words around a placeholder
   live in the string, so each language can use its own (`„{0}”` in Polish, for example). Date
   patterns such as `ShortDateFormat` are .NET custom format strings and are formatted with the
   regional culture. Write a line break inside a value as `&#xA;`, as `EmptyWorkoutEditor` does,
   never as a literal new line.
3. Counted words are plurals. A key ending in `_Other` has siblings per CLDR cardinal category:
   `_Zero`, `_One`, `_Two`, `_Few`, `_Many`. Provide `_Other` and exactly the categories the
   plural rules select for your language, for example `SetCount_One`, `SetCount_Few`,
   `SetCount_Many` and `SetCount_Other` for Polish.
4. Build. The build records the new culture and the language appears in Settings under its native
   name. No code or list changes.
5. Run the tests. The localization tests in `MuscleMemory.Tests/Localization/AppResourcesTests.cs`
   check every resource file against `AppResources.resx`, so they catch a missing or extra key, an
   empty value, a placeholder that differs from the English one (`{0}`, `{1:N0}`), a plural
   category missing or one the language never selects, and a XAML `{loc:Text}` key that does not
   exist. They also fail on a key that nothing uses.

Built-in plural rules cover every EU official language (Bulgarian, Croatian, Czech, Danish, Dutch,
English, Estonian, Finnish, French, German, Greek, Hungarian, Irish, Italian, Latvian, Lithuanian,
Maltese, Polish, Portuguese, Romanian, Slovak, Slovenian, Spanish, Swedish) plus Ukrainian and
Russian. Any other language uses one/other. The tests accept a language only if `PluralRules` has a
rule for it or it is listed in the tests as a one/other language, so a language outside the EU
needs its CLDR rules checked and either a rule or that entry.

Table column headers sit in fixed-width columns, so they have their own short keys: give
`LabelRepsColumn` the abbreviation your language uses ("Powt." in Polish) when the full word does
not fit. Text may wrap between words, but never inside one.

The bundled Barlow Semi Condensed font covers Latin-1, Latin Extended-A and every letter of the
EU's Latin-script languages, including Romanian ș and ț. It has no Greek or Cyrillic, so Greek,
Bulgarian, Ukrainian and Russian would show their letters in Android's fallback font.

## Known limitations

- **Crash on a fast relaunch that does not come from the launcher.** Pressing BACK on a tab root and
  immediately starting the activity with an explicit intent (for example
  `adb shell am start -n …/crc6419b996a3bf9febbe.MainActivity`) can crash in `OnCreate`. The error is
  an `ObjectDisposedException` for `ShellToolbarTracker`, thrown from MAUI's
  `ShellFlyoutRenderer.Disconnect`. It still happens on MAUI 10.0.110: 4 of 10 attempts crashed on a
  Release build, the same rate as on 10.0.60.
- **Jank on long chip strips.** Swiping the session-date chips of a workout with a long history
  drops frames: 15 % janky frames and a 90th-percentile frame of 40 ms on an emulator Release build,
  against 18 ms for vertical scrolling on the same page. The exercise filter chips are not
  affected.
- **Android language changes need a restart.** The app reads the Android language once, at
  startup. With the language set to System default, changing the language in Android settings
  while the app is running changes neither its text nor its number and date formatting until the
  process restarts. A language picked in the app's own Settings applies immediately.
- **No reordering.** Exercises in a workout cannot be reordered.

## Font license

The app bundles Barlow Semi Condensed ExtraBold (Copyright 2017 The Barlow Project Authors,
https://github.com/jpt/barlow), unmodified from the google/fonts repository. It is licensed under
the SIL Open Font License 1.1, whose full text ships next to the font in
`MuscleMemory/Resources/Fonts/OFL.txt`.
