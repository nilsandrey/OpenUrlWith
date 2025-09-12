# Build OpenWith Tool for Production

Write-Host "Building OpenWith Tool..." -ForegroundColor Green

# Clean previous builds
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}

# Build the application
dotnet publish OpenWithTool.csproj -c Release -r win-x64 --self-contained -o "publish"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: .\publish" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Installation Instructions:" -ForegroundColor Yellow
    Write-Host "1. Copy files to C:\Program Files\OpenWithTool\" -ForegroundColor White
    Write-Host "2. Run Resources\install-browser.ps1 as Administrator" -ForegroundColor White
    Write-Host "3. Set as default browser in Windows Settings > Apps > Default apps" -ForegroundColor White
    Write-Host ""
    Write-Host "Quick install commands (run as Administrator):" -ForegroundColor Yellow
    Write-Host "New-Item -ItemType Directory -Path 'C:\Program Files\OpenWithTool' -Force" -ForegroundColor Gray
    Write-Host "Copy-Item -Path 'publish\*' -Destination 'C:\Program Files\OpenWithTool\' -Recurse -Force" -ForegroundColor Gray
    Write-Host "& 'Resources\install-browser.ps1'" -ForegroundColor Gray
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}

Read-Host "Press Enter to continue"