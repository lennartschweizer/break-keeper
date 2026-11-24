$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$appId = $props.Project.PropertyGroup.AppId
$version = $props.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($appId) -or [string]::IsNullOrWhiteSpace($version)) {
    throw "AppId or Version missing in Directory.Build.props"
}

$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCmd) {
        throw "dotnet.exe not found at $dotnet or in PATH"
    }
    $dotnet = $dotnetCmd.Source
}

& $dotnet test (Join-Path $root "App.sln") -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

$stageDir = Join-Path $root "out\stage"
$distDir = Join-Path $root "out\dist"
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$publishArgs = @(
    (Join-Path $root "src\Desktop\Desktop.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishReadyToRun=true",
    "-p:PublishSingleFile=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-o", $stageDir
)

& $dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Get-ChildItem -Path $stageDir -Filter "*.pdb" -Recurse | Remove-Item -Force

$zipName = "$appId-v$version-win-x64.zip"
$zipPath = Join-Path $distDir $zipName
$hashPath = "$zipPath.sha256"

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $hashPath) { Remove-Item $hashPath -Force }

Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $zipName" | Out-File -FilePath $hashPath -Encoding ascii -NoNewline

Write-Output "Created release archive: $zipPath"
Write-Output "Created SHA256 checksum: $hashPath"
