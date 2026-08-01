# App Icon

The WinUI package icon assets live in `Assets/`. `Assets/AppIcon.ico` is used by the executable, title bars, and `AppWindow`; the PNG variants are referenced by `Package.appxmanifest` for package and Start menu presentation.

The checked-in files are the WinUI template defaults. When replacing them:

1. Keep every filename referenced by `OpenWithTool.csproj` and `Package.appxmanifest`.
2. Provide the matching scale and target-size variants, including transparent unplated icons.
3. Preserve the ICO file with at least 32, 48, and 256 pixel sizes.
4. Build an MSIX and verify the title bar, taskbar, Start menu, and Default apps presentation.
