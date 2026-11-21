param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$appId = $props.Project.PropertyGroup.AppId
if ([string]::IsNullOrWhiteSpace($appId)) {
    throw "AppId missing in Directory.Build.props"
}

$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe not found at $dotnet"
}

$config = if ($Release) { "Release" } else { "Debug" }
$outDir = if ($Release) { Join-Path $root "out\release" } else { Join-Path $root "out\dev" }

Get-Process -Name "FocusEnforcer.App", "BreakEnforcer.App", "$appId.App" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

$publishArgs = @(
    (Join-Path $root "src\Desktop\Desktop.csproj"),
    "-c", $config,
    "-r", "win-x64",
    "--self-contained",
    "-o", $outDir
)
if ($Release) {
    $publishArgs += "-p:PublishReadyToRun=true"
}

& $dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $outDir "$appId.App.exe"
if ($Release -and (Get-ScheduledTask -TaskName $appId -ErrorAction SilentlyContinue)) {
    Start-ScheduledTask -TaskName $appId
} else {
    Start-Process $exePath
}
