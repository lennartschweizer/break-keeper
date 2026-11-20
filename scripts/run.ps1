$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe not found at $dotnet"
}

Get-Process -Name "FocusEnforcer.App" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

& $dotnet publish (Join-Path $root "src\FocusEnforcer.App\FocusEnforcer.App.csproj") -c Debug -r win-x64 --self-contained -o (Join-Path $root "out\dev")
Start-Process (Join-Path $root "out\dev\FocusEnforcer.App.exe")
