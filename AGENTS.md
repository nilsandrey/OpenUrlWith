# Agent Instructions

## Project Shape

- This is a Windows-only WinUI 3 desktop app targeting `net10.0-windows10.0.26100.0`; keep UI work in `Views/*.xaml`, behavior/state in `ViewModels`, and platform/browser operations in `Services`.
- Dependency injection is configured in `App.xaml.cs`. Register new services there and inject interfaces into view models instead of newing service implementations in UI code.
- Runtime settings and browser cache are per-user files under `%APPDATA%\OpenWithTool`; `appsettings.json` only provides bundled defaults.
- See [README.md](README.md) for installation, default-browser registration, usage, and supported-browser details. See [Resources/icon-readme.md](Resources/icon-readme.md) before changing icon-related project settings.

## Release And Versioning

- Always update `ReleaseNotes.md` for user-visible changes, bug fixes, behavior changes, documentation/process changes, and version bumps.
- Follow semantic versioning when changing the project version in `OpenWithTool.csproj`: increment MAJOR for breaking changes, MINOR for backward-compatible features, and PATCH for backward-compatible bug fixes or maintenance-only updates.
- When a change includes both a feature and a fix, use the highest applicable semantic-version increment and document both in the release notes.
- Keep release note entries concise, user-facing, and grouped under a dated version heading.

## Build And Run

- Restore/build: `dotnet restore` then `dotnet build OpenWithTool.csproj -p:Platform=x64`.
- Build and run through the packaged WinUI workflow: `.\BuildAndRun.ps1 OpenWithTool.csproj`.
- Run a built app with a URL argument: `winapp run .\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64 --args "https://www.example.com"`.
- Publish a self-contained release and optionally build an MSIX: `.\build.ps1 -Package`; omit `-Package` for publish output only.
- There is currently no test project in the repo. For behavior changes, at minimum run `dotnet build OpenWithTool.csproj`; add targeted tests only if you introduce test infrastructure intentionally.

## Implementation Conventions

- Follow the existing CommunityToolkit.Mvvm style: `ObservableObject`, `[ObservableProperty]` partial properties, `[RelayCommand]`, async initialization methods, and WinUI `x:Bind`. Keep code-behind limited to window wiring and UI events.
- Preserve async browser detection and launching paths so the UI stays responsive. Avoid blocking calls on the UI thread when touching registry, file system, or browser profile data.
- Keep browser/profile additions in `BrowserDetectionService` and launch argument construction in `BrowserLauncherService`; do not spread browser-specific command-line rules into view models.
- Use `Newtonsoft.Json` consistently for the existing settings/cache models unless doing a broader, deliberate serialization migration.

## Windows And Registry Caveats

- Browser detection depends on Windows install paths, user profile folders, and registry state; validate registry-sensitive changes on Windows.
- The desktop manifest runs `asInvoker`. The MSIX declares `runFullTrust` and `allowElevation`; registration/unregistration writes under `HKLM`/`HKCR` through short elevated command processes, while normal link selection stays at medium integrity.
- `--register`, `--unregister`, `--hide-icons`, and `--show-icons` are browser-registration command hooks handled during startup; keep them non-interactive unless changing Windows registration behavior.
- When changing cache or settings behavior, consider existing user files in `%APPDATA%\OpenWithTool\settings.json` and `%APPDATA%\OpenWithTool\browser_cache.json`.
