# App Icon

The application icon is stored as:

- `app.ico` in the project root: the multi-resolution Windows application icon
- `Resources/app-icon.png`: the transparent high-resolution source used in the README

`OpenWithTool.csproj` references `app.ico` through its `ApplicationIcon`
property. The ICO contains 16, 24, 32, 48, 64, 128, and 256 pixel variants.
