param(
    [switch]$Package
)

$ErrorActionPreference = 'Stop'

dotnet publish "$PSScriptRoot\OpenWithTool.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:Platform=x64 `
    -o "$PSScriptRoot\publish\x64"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputDirectory = Join-Path $PSScriptRoot 'publish\x64'
Write-Host "Release build: $outputDirectory" -ForegroundColor Green

if (-not $Package) {
    Write-Host 'Pass -Package to create a signed development MSIX.' -ForegroundColor DarkGray
    exit 0
}

$certificatePath = Join-Path $PSScriptRoot 'OpenWithTool_cert.pfx'
$packagePath = Join-Path $PSScriptRoot 'OpenWithTool_1.3.0.0_x64.msix'
if (Test-Path -LiteralPath $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

$packageArguments = @(
    'package',
    $outputDirectory,
    '--manifest', "$PSScriptRoot\Package.appxmanifest",
    '--executable', 'OpenWithTool.exe',
    '--output', $packagePath
)

if (Test-Path -LiteralPath $certificatePath) {
    $packageArguments += @('--cert', $certificatePath)
} else {
    $packageArguments += '--generate-cert'
}

& winapp @packageArguments

exit $LASTEXITCODE
