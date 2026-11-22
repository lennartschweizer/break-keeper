param(
    [ValidateSet("idle", "prewarning", "break", "break-complete")]
    [string]$View = "idle"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe not found at $dotnet"
}

$flagMap = @{
    "idle"           = "--sandbox"
    "prewarning"     = "--preview-prewarning"
    "break"          = "--preview-break"
    "break-complete" = "--preview-break-complete"
}

$flag = $flagMap[$View]
$project = Join-Path $root "src\Desktop\Desktop.csproj"
$exePath = Join-Path $root "src\Desktop\bin\Debug\net8.0-windows\BreakKeeper.App.exe"

& $dotnet build $project -c Debug --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $exePath)) {
    throw "Preview executable not found at $exePath"
}

$result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
    CommandLine = "`"$exePath`" $flag"
}
if ($result.ReturnValue -ne 0) {
    throw "Failed to launch preview process (Win32_Process.Create returned $($result.ReturnValue))"
}
