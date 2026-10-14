<#
.SYNOPSIS
    Registers J-Center-Lite in the Start menu so it can be launched (and pinned) like an installed app.

.DESCRIPTION
    Creates a per-user shortcut in
    %APPDATA%\Microsoft\Windows\Start Menu\Programs\J-Center-Lite.lnk pointing at the
    published executable, with the app icon and working directory set.

    With -Pin it also asks the shell to pin the shortcut to Start. Windows 11 only
    honours that undocumented verb for some shells; when it is ignored the shortcut
    still shows up under "All apps" and can be pinned with the context menu.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\tools\install-start-menu-shortcut.ps1 -Pin
#>
[CmdletBinding()]
param(
    [string]$TargetPath,
    [switch]$Pin
)

$ErrorActionPreference = 'Stop'

if (-not $TargetPath) {
    $TargetPath = Join-Path $PSScriptRoot '..\dist\win-x64\JCenterLite.exe'
}

$target = [System.IO.Path]::GetFullPath($TargetPath)
if (-not (Test-Path -LiteralPath $target)) {
    throw "Target executable not found: $target (run dotnet publish first)"
}

$programs = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
New-Item -ItemType Directory -Path $programs -Force | Out-Null

$linkPath = Join-Path $programs 'J-Center-Lite.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($linkPath)
$shortcut.TargetPath = $target
$shortcut.WorkingDirectory = Split-Path -Parent $target
$shortcut.IconLocation = "$target,0"
$shortcut.Description = 'J-Center-Lite secondary display controller'
$shortcut.Save()

Write-Host "Start menu shortcut created: $linkPath"

if ($Pin) {
    try {
        $appShell = New-Object -ComObject Shell.Application
        $item = $appShell.Namespace($programs).ParseName('J-Center-Lite.lnk')
        $item.InvokeVerb('startpin')
        Write-Host 'Requested a pin to Start (right-click the entry if the shell ignored it).'
    }
    catch {
        Write-Warning "Pin request failed: $($_.Exception.Message)"
    }
}
