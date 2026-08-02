# App Icon

The WinUI package icon assets live in `Assets/`. `Assets/AppIcon.ico` is used by the executable, title bars, and `AppWindow`; the PNG variants are referenced by `Package.appxmanifest` for package and Start menu presentation.

The checked-in identity set is derived from `Assets/LogoMaster.png`, a 1024x1024 transparent master. The package PNGs use asset-specific safe-area padding, and `AppIcon.ico` contains 16, 20, 24, 32, 40, 48, 64, 128, and 256 pixel frames.

When replacing or regenerating them:

1. Keep every filename referenced by `OpenWithTool.csproj` and `Package.appxmanifest`.
2. Provide the matching scale and target-size variants, including transparent unplated icons.
3. Preserve the ICO file with at least 32, 48, and 256 pixel sizes.
4. Build an MSIX and verify the title bar, taskbar, Start menu, and Default apps presentation.
