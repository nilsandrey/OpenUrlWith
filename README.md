# OpenWith Tool

OpenWith Tool is a Windows 10/11 desktop application for choosing which installed browser and browser profile opens a web link. It is built with WinUI 3 and the Windows App SDK.

## Features

- Detects Chrome, Firefox, Edge, Opera, Brave, Vivaldi, and registered Windows browsers.
- Detects Chromium and Firefox profiles and shows the installed browser icon.
- Remembers the last browser and profile, with an optional auto-selection countdown.
- Saves exact URL, domain, or path rules for links that should open automatically.
- Provides a focused profile picker for a preferred browser.
- Stores settings and browser cache per user under `%APPDATA%\OpenWithTool`.

## Requirements

- Windows 10 version 1809 or later, or Windows 11.
- .NET 10 SDK for development.
- Developer Mode, the WinApp CLI, and WinUI templates for local deployment.

## Development

Restore and build the x64 app:

```powershell
dotnet restore
dotnet build OpenWithTool.csproj -p:Platform=x64
```

Build and launch with WinUI debug output:

```powershell
.\BuildAndRun.ps1 OpenWithTool.csproj
```

To launch with a URL after building:

```powershell
winapp run .\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64 --args "https://www.example.com"
```

## Package And Install

Build a Release MSIX signed with a generated development certificate:

```powershell
.\build.ps1 -Package
```

Trust the generated certificate once from an elevated terminal, then install the package:

```powershell
winapp cert install .\OpenWithTool_cert.pfx
Add-AppxPackage .\OpenWithTool_1.3.0.0_x64.msix
```

Development certificates are for local sideloading only. Use an organization or Store certificate and a trusted timestamp for distribution.

## Browser Registration

1. Open **Settings** in OpenWith Tool.
2. Select **Register** and approve the Windows elevation prompt.
3. Open **Default apps** from the same settings page.
4. Choose OpenWith Tool for the HTTP and HTTPS associations.

Registration writes machine-wide browser and protocol entries. Normal link selection continues to run without administrator privileges.

## Usage

When OpenWith Tool receives a link, select a browser and optional profile, then choose **Open**. Moving the pointer or using the keyboard stops the auto-selection countdown. Enable **Remember for this site** to store an exact URL, domain, or path rule.

Use **Sites** to review, add, or remove remembered rules. Use the pin command on a browser row to make its profile picker the default main view.

## Supported Browsers

- Google Chrome
- Mozilla Firefox
- Microsoft Edge
- Opera
- Brave Browser
- Vivaldi
- Other browsers registered with Windows

## Project Layout

- `Views/`: WinUI windows and UI event wiring.
- `ViewModels/`: CommunityToolkit.Mvvm state and commands.
- `Services/`: configuration, browser detection and launch, icons, registration, and window interop.
- `Models/`: settings, cache, browser, profile, and remembered-site models.
