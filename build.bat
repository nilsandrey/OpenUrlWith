@echo off
echo Building OpenWith Tool...
dotnet publish OpenWithTool.csproj -c Release -r win-x64 --self-contained -o ".\publish"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build completed successfully!
    echo Output directory: .\publish
    echo.
    echo To install:
    echo 1. Copy files to C:\Program Files\OpenWithTool\
    echo 2. Run install-browser.ps1 as Administrator
    echo 3. Set as default browser in Windows Settings
) else (
    echo Build failed!
)
pause