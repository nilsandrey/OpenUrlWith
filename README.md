# OpenWith Tool

A Windows desktop application that allows you to select which browser to open web links with. When set as the default browser, it presents a dialog with a list of installed browsers and their profiles, allowing you to choose how to open each link.

## Features

- **Dynamic Browser Detection**: Automatically detects installed browsers (Chrome, Firefox, Edge, Opera, Brave, Vivaldi, and others)
- **Profile Support**: Supports browser profiles for Chrome-based browsers and Firefox
- **Smart Caching**: Caches browser list for quick access (refreshes daily or manually)
- **Auto-Selection**: Automatically selects the last used browser after a configurable timeout (default: 3 seconds)
- **User Interaction Detection**: Stops auto-selection when user interacts with the dialog
- **Persistent Settings**: Remembers your last selection for quick access

## Installation

1. **Build the Application**:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. **Copy to Program Files** (as Administrator):
   ```powershell
   Copy-Item -Recurse "bin\Release\net8.0-windows\win-x64\publish\*" "C:\Program Files\OpenWithTool\"
   ```

3. **Register as Browser** (as Administrator):
   ```powershell
   # Using PowerShell script
   .\Resources\install-browser.ps1
   
   # Or using registry file
   # Right-click Resources\register-browser.reg and select "Merge"
   ```

4. **Set as Default Browser**:
   - Open Windows Settings
   - Go to Apps > Default apps
   - Click on "Web browser"
   - Select "OpenWith Tool - Browser Selector"

## Usage

Once installed and set as the default browser:

1. Click any web link
2. The OpenWith Tool dialog appears
3. Select your preferred browser and profile
4. The link opens in the selected browser
5. Your choice is remembered for next time

### Auto-Selection

- The tool will automatically select your last choice after 3 seconds (configurable)
- Move the mouse or press any key to stop the auto-selection timer
- Use the Settings button to configure the timeout duration

### Manual Controls

- **Refresh**: Reload the browser list
- **Settings**: Configure preferences (timeout, etc.)
- **Cancel**: Close without opening the link
- **Launch**: Open the link in the selected browser

## Configuration

Settings are stored in: `%APPDATA%\OpenWithTool\settings.json`

Available settings:
- `AutoSelectTimeoutSeconds`: Auto-selection timeout (default: 3)
- `CacheDurationHours`: Browser cache duration (default: 24)
- `EnableAutoSelect`: Enable/disable auto-selection (default: true)
- `LastSelectedBrowser`: Last selected browser name
- `LastSelectedProfile`: Last selected profile name

## Uninstallation

1. **Change Default Browser** (optional):
   - Set another browser as default in Windows Settings

2. **Unregister from System** (as Administrator):
   ```powershell
   # Using PowerShell script
   .\Resources\install-browser.ps1 -Uninstall
   
   # Or using registry file
   # Right-click Resources\unregister-browser.reg and select "Merge"
   ```

3. **Remove Files**:
   ```powershell
   Remove-Item -Recurse "C:\Program Files\OpenWithTool"
   Remove-Item -Recurse "$env:APPDATA\OpenWithTool"
   ```

## Supported Browsers

The tool automatically detects:
- **Google Chrome** (with profiles)
- **Mozilla Firefox** (with profiles)
- **Microsoft Edge** (with profiles)
- **Opera**
- **Brave Browser** (with profiles)
- **Vivaldi** (with profiles)
- Any other browser registered in Windows

## Development

### Requirements
- .NET 8.0 or later
- Windows 10/11

### Building
```bash
git clone <repository-url>
cd openwith-tool
dotnet restore
dotnet build
```

### Running in Development
```bash
dotnet run -- "https://www.example.com"
```

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]