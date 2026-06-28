# Agent Instructions

## Project Shape

- This is a Windows-only WPF desktop app targeting `net8.0-windows`; keep UI work in `Views/*.xaml`, behavior/state in `ViewModels`, and platform/browser operations in `Services`.
- Dependency injection is configured in `App.xaml.cs`. Register new services there and inject interfaces into view models instead of newing service implementations in UI code.
- Runtime settings and browser cache are per-user files under `%APPDATA%\OpenWithTool`; `appsettings.json` only provides bundled defaults.
- See [README.md](README.md) for installation, default-browser registration, usage, and supported-browser details. See [Resources/icon-readme.md](Resources/icon-readme.md) before changing icon-related project settings.

## Release And Versioning

- Always update `ReleaseNotes.md` for user-visible changes, bug fixes, behavior changes, documentation/process changes, and version bumps.
- Follow semantic versioning when changing the project version in `OpenWithTool.csproj`: increment MAJOR for breaking changes, MINOR for backward-compatible features, and PATCH for backward-compatible bug fixes or maintenance-only updates.
- When a change includes both a feature and a fix, use the highest applicable semantic-version increment and document both in the release notes.
- Keep release note entries concise, user-facing, and grouped under a dated version heading.

## Build And Run

- Restore/build: `dotnet restore` then `dotnet build OpenWithTool.csproj`.
- Run locally with a URL argument: `dotnet run --project OpenWithTool.csproj -- "https://www.example.com"`.
- Publish release build: `dotnet publish OpenWithTool.csproj -c Release -r win-x64 --self-contained -o publish`.
- `build.ps1` and `build.bat` both wait for interactive input at the end; prefer direct `dotnet` commands in automated agent runs.
- There is currently no test project in the repo. For behavior changes, at minimum run `dotnet build OpenWithTool.csproj`; add targeted tests only if you introduce test infrastructure intentionally.

## Implementation Conventions

- Follow the existing MVVM style: `INotifyPropertyChanged`, `ICommand`/`RelayCommand`, async initialization methods, and WPF data binding. Keep code-behind limited to window wiring and UI events.
- Preserve async browser detection and launching paths so the UI stays responsive. Avoid blocking calls on the UI thread when touching registry, file system, or browser profile data.
- Keep browser/profile additions in `BrowserDetectionService` and launch argument construction in `BrowserLauncherService`; do not spread browser-specific command-line rules into view models.
- Use `Newtonsoft.Json` consistently for the existing settings/cache models unless doing a broader, deliberate serialization migration.

## Windows And Registry Caveats

- Browser detection depends on Windows install paths, user profile folders, and registry state; validate registry-sensitive changes on Windows.
- The app manifest runs `asInvoker`. Registration/unregistration writes under `HKLM`/`HKCR` and requires an elevated process, but normal link selection should keep working without admin rights.
- `--register`, `--hide-icons`, and `--show-icons` are browser-registration command hooks handled during startup; keep them non-interactive unless changing Windows registration behavior.
- When changing cache or settings behavior, consider existing user files in `%APPDATA%\OpenWithTool\settings.json` and `%APPDATA%\OpenWithTool\browser_cache.json`.
