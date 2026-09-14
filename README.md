# ToDoOfSorts

[![CI](https://github.com/WebGrga/todo-of-sorts/actions/workflows/ci.yml/badge.svg)](https://github.com/WebGrga/todo-of-sorts/actions/workflows/ci.yml)

ToDoOfSorts is a native, offline-first productivity app for Android and iPhone. Instead of treating task completion as a checkbox, it turns the moment into a tactile action: press **Stamp It** and leave a permanent DONE impression in the day's record.

The project combines product design, cross-platform mobile engineering, local persistence, notification scheduling, accessibility work, and performance measurement in one complete application.

## Highlights

- Daily commitments and bonus tasks with explicit day-cleared rules
- Add, edit, start, complete, undo, skip, move, and historical correction flows
- Daily and selected-weekday recurrence with linked history preservation
- Native scheduled reminders with Start, Move, and Skip actions
- Quiet hours, permission handling, and optional evening review
- Weekly totals, category completion rates, runs, XP, and per-task history
- Offline SQLite storage with validated JSON backup/restore and safe CSV export
- Tactile, energetic, and calm feedback profiles with reduced-motion controls
- Responsive task sheets and large-text handling for accessibility

The app contains no account system, advertising, remote analytics, or cloud task storage. Each installation owns its local data.

## Architecture

```text
src/
  ToDoOfSorts.Core/            domain model, recurrence, rewards, statistics
  ToDoOfSorts.Infrastructure/  SQLite persistence and backup/restore
  ToDoOfSorts.App/             .NET MAUI UI and native platform services
tests/
  ToDoOfSorts.Tests/           37 domain and storage tests
  ToDoOfSorts.Performance/     reproducible large-history benchmark
```

The core project is platform-independent. Android and iOS implementations provide notification scheduling and native feedback behind the shared application layer.

## Build

Requirements:

- .NET 10 SDK
- .NET MAUI workload
- Android SDK for Android builds
- macOS and Xcode for signed iOS builds

Restore and test the solution:

```bash
dotnet restore ToDoOfSorts.slnx
dotnet test tests/ToDoOfSorts.Tests/ToDoOfSorts.Tests.csproj
```

Build Android:

```bash
dotnet build src/ToDoOfSorts.App/ToDoOfSorts.App.csproj -f net10.0-android
```

On macOS, build an iOS simulator target with:

```bash
bash scripts/build-ios.sh simulator
```

The PowerShell Android helper accepts explicit SDK/JDK paths when automatic discovery is unavailable:

```powershell
./scripts/Build-Android.ps1 -AndroidSdkDirectory 'C:\path\to\android-sdk' -JavaSdkDirectory 'C:\path\to\jdk'
```

## Verification

The current beta has 37 automated domain/storage tests. The repository also includes a detailed [engineering audit](APP_AUDIT.md) covering corrected edge cases, accessibility checks, notification behavior, backup validation, and measured performance improvements.

The Android flow was exercised on an API 36 emulator. Managed iOS compilation succeeded on Windows, but final Apple linking, signing, and physical-device behavior still require a Mac and iPhone. These limits are kept explicit rather than presented as verified behavior.

## Data and privacy

Task data stays in the local SQLite database. Exported JSON backups and CSV files can contain private information and should not be committed. Build products, local SDKs, signing material, and distribution credentials are excluded from source control.

## Author

Created by [Roko Grga](https://github.com/WebGrga).

## License

MIT. The bundled fonts retain their own license files under `src/ToDoOfSorts.App/Resources/Fonts/`.
