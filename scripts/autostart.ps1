param(
    [switch]$Disable
)

$ErrorActionPreference = "Stop"

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$entryName = "FocusEnforcer"
$root = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $root "out\release\FocusEnforcer.App.exe"

if ($Disable) {
    if (Get-ItemProperty -Path $runKey -Name $entryName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $runKey -Name $entryName
        Write-Output "Removed $entryName from Windows startup."
    } else {
        Write-Output "$entryName is not registered in Windows startup."
    }
    exit 0
}

if (-not (Test-Path $exePath)) {
    throw "Release executable not found at $exePath"
}

$quotedCommand = "`"$exePath`""
Set-ItemProperty -Path $runKey -Name $entryName -Value $quotedCommand -Type String

$actual = (Get-ItemProperty -Path $runKey -Name $entryName).$entryName
if ($actual -ne $quotedCommand) {
    throw "Registry verification failed: expected $quotedCommand, got $actual"
}

Write-Output "Registered $entryName in Windows startup: $actual"
