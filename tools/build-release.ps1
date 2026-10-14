<#
.SYNOPSIS
    Builds the self-contained win-x64 package of J-Center-Lite and writes a matching .sha256 file.

.DESCRIPTION
    Produces dist\J-Center-Lite-<Version>-win-x64.zip plus dist\J-Center-Lite-<Version>-win-x64.zip.sha256.
    The package is self-contained: the target machine does not need the .NET runtime installed.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version 1.0.0
#>
[CmdletBinding()]
param(
    [string]$Version = '1.0.0',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot

$dotnet = 'dotnet'
if ($env:DOTNET_ROOT) {
    $candidate = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
    if (Test-Path -LiteralPath $candidate) { $dotnet = $candidate }
}
if ($dotnet -eq 'dotnet') {
    $candidate = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $candidate) { $dotnet = $candidate }
}

$project = Join-Path $repo 'src\JCenterLite.App\JCenterLite.App.csproj'
$outDir = Join-Path $repo 'dist\win-x64'
$zip = Join-Path $repo "dist\J-Center-Lite-$Version-win-x64.zip"

Write-Host "dotnet : $dotnet"
Write-Host "version: $Version"

& $dotnet publish $project -c $Configuration -r win-x64 -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None `
    -p:Version=$Version -o $outDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

if (Test-Path -LiteralPath $zip) {
    [System.IO.File]::Delete($zip)
}

Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip -CompressionLevel Optimal

$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
"$hash  $(Split-Path -Leaf $zip)" | Set-Content -LiteralPath "$zip.sha256" -Encoding ASCII

Write-Host "package: $zip"
Write-Host "sha256 : $hash"
Write-Host ("size   : {0:N1} MB" -f ((Get-Item -LiteralPath $zip).Length / 1MB))
