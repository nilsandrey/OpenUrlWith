# OpenWith Tool Installation Script
# Run as Administrator

param(
    [switch]$Uninstall,
    [string]$InstallPath = "C:\Program Files\OpenWithTool"
)

function Test-Administrator {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Register-OpenWithTool {
    param([string]$ExePath)
    
    Write-Host "Registering OpenWith Tool as browser..." -ForegroundColor Green
    
    # Register as browser
    $browserKey = "HKLM:\SOFTWARE\Clients\StartMenuInternet\OpenWithTool"
    New-Item -Path $browserKey -Force | Out-Null
    Set-ItemProperty -Path $browserKey -Name "(Default)" -Value "OpenWith Tool - Browser Selector"
    
    # Default icon
    $iconKey = "$browserKey\DefaultIcon"
    New-Item -Path $iconKey -Force | Out-Null
    Set-ItemProperty -Path $iconKey -Name "(Default)" -Value "`"$ExePath`",0"
    
    # Install info
    $installKey = "$browserKey\InstallInfo"
    New-Item -Path $installKey -Force | Out-Null
    Set-ItemProperty -Path $installKey -Name "ReinstallCommand" -Value "`"$ExePath`" --register"
    Set-ItemProperty -Path $installKey -Name "HideIconsCommand" -Value "`"$ExePath`" --hide-icons"
    Set-ItemProperty -Path $installKey -Name "ShowIconsCommand" -Value "`"$ExePath`" --show-icons"
    Set-ItemProperty -Path $installKey -Name "IconsVisible" -Value 1 -Type DWord
    
    # Shell command
    $shellKey = "$browserKey\shell\open\command"
    New-Item -Path $shellKey -Force | Out-Null
    Set-ItemProperty -Path $shellKey -Name "(Default)" -Value "`"$ExePath`" `"%1`""
    
    # Capabilities
    $capKey = "$browserKey\Capabilities"
    New-Item -Path $capKey -Force | Out-Null
    Set-ItemProperty -Path $capKey -Name "ApplicationName" -Value "OpenWith Tool - Browser Selector"
    Set-ItemProperty -Path $capKey -Name "ApplicationDescription" -Value "Browser selection tool for opening web links"
    Set-ItemProperty -Path $capKey -Name "ApplicationIcon" -Value "$ExePath,0"
    
    # File associations
    $fileKey = "$capKey\FileAssociations"
    New-Item -Path $fileKey -Force | Out-Null
    Set-ItemProperty -Path $fileKey -Name ".htm" -Value "OpenWithToolHTML"
    Set-ItemProperty -Path $fileKey -Name ".html" -Value "OpenWithToolHTML"
    
    # URL associations
    $urlKey = "$capKey\URLAssociations"
    New-Item -Path $urlKey -Force | Out-Null
    Set-ItemProperty -Path $urlKey -Name "http" -Value "OpenWithToolURL"
    Set-ItemProperty -Path $urlKey -Name "https" -Value "OpenWithToolURL"
    
    # Register in RegisteredApplications
    $regKey = "HKLM:\SOFTWARE\RegisteredApplications"
    Set-ItemProperty -Path $regKey -Name "OpenWithTool" -Value "SOFTWARE\Clients\StartMenuInternet\OpenWithTool\Capabilities"
    
    # Register URL protocols
    $urlProtocolKey = "HKCR:\OpenWithToolURL"
    New-Item -Path $urlProtocolKey -Force | Out-Null
    Set-ItemProperty -Path $urlProtocolKey -Name "(Default)" -Value "URL:http Protocol"
    Set-ItemProperty -Path $urlProtocolKey -Name "URL Protocol" -Value ""
    
    $urlIconKey = "$urlProtocolKey\DefaultIcon"
    New-Item -Path $urlIconKey -Force | Out-Null
    Set-ItemProperty -Path $urlIconKey -Name "(Default)" -Value "`"$ExePath`",0"
    
    $urlCommandKey = "$urlProtocolKey\shell\open\command"
    New-Item -Path $urlCommandKey -Force | Out-Null
    Set-ItemProperty -Path $urlCommandKey -Name "(Default)" -Value "`"$ExePath`" `"%1`""
    
    # Register HTML file type
    $htmlKey = "HKCR:\OpenWithToolHTML"
    New-Item -Path $htmlKey -Force | Out-Null
    Set-ItemProperty -Path $htmlKey -Name "(Default)" -Value "HTML Document"
    
    $htmlIconKey = "$htmlKey\DefaultIcon"
    New-Item -Path $htmlIconKey -Force | Out-Null
    Set-ItemProperty -Path $htmlIconKey -Name "(Default)" -Value "`"$ExePath`",0"
    
    $htmlCommandKey = "$htmlKey\shell\open\command"
    New-Item -Path $htmlCommandKey -Force | Out-Null
    Set-ItemProperty -Path $htmlCommandKey -Name "(Default)" -Value "`"$ExePath`" `"%1`""
    
    Write-Host "Registration completed successfully!" -ForegroundColor Green
}

function Unregister-OpenWithTool {
    Write-Host "Unregistering OpenWith Tool..." -ForegroundColor Yellow
    
    # Remove browser registration
    Remove-Item -Path "HKLM:\SOFTWARE\Clients\StartMenuInternet\OpenWithTool" -Recurse -Force -ErrorAction SilentlyContinue
    
    # Remove from RegisteredApplications
    Remove-ItemProperty -Path "HKLM:\SOFTWARE\RegisteredApplications" -Name "OpenWithTool" -ErrorAction SilentlyContinue
    
    # Remove URL protocols
    Remove-Item -Path "HKCR:\OpenWithToolURL" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "HKCR:\OpenWithToolHTML" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "Unregistration completed!" -ForegroundColor Green
}

# Main script
if (-not (Test-Administrator)) {
    Write-Error "This script must be run as Administrator!"
    exit 1
}

if ($Uninstall) {
    Unregister-OpenWithTool
} else {
    $exePath = Join-Path $InstallPath "OpenWithTool.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Error "OpenWithTool.exe not found at: $exePath"
        Write-Host "Please ensure the application is installed at the specified location."
        exit 1
    }
    
    Register-OpenWithTool -ExePath $exePath
    
    Write-Host ""
    Write-Host "OpenWith Tool has been registered as a browser!" -ForegroundColor Green
    Write-Host "You can now set it as your default browser in Windows Settings." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To open Windows Settings for default apps:" -ForegroundColor Yellow
    Write-Host "Settings > Apps > Default apps > Web browser" -ForegroundColor Yellow
}