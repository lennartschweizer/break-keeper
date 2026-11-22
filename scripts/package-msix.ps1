param(
    [switch]$SelfSign
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$appId = $props.Project.PropertyGroup.AppId
$version = $props.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($appId) -or [string]::IsNullOrWhiteSpace($version)) {
    throw "AppId or Version missing in Directory.Build.props"
}

$msixVersion = if ($version -match '^\d+\.\d+\.\d+$') { "$version.0" } else { $version }

$dotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCmd) {
        throw "dotnet.exe not found at $dotnet or in PATH"
    }
    $dotnet = $dotnetCmd.Source
}

function Find-WinSdkTool {
    param([string]$ToolName)
    $sdkRoots = @("${env:ProgramFiles(x86)}\Windows Kits\10\bin", "${env:ProgramFiles}\Windows Kits\10\bin")
    $candidates = Get-ChildItem -Path $sdkRoots -Filter $ToolName -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match '\\x64$' } |
        Sort-Object FullName -Descending
    if (-not $candidates) {
        throw "$ToolName not found under Windows Kits 10 bin\\*\\x64"
    }
    return $candidates[0].FullName
}

function New-IconPng {
    param(
        [string]$IconPath,
        [string]$OutputPath,
        [int]$Width,
        [int]$Height
    )
    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase

    $stream = [System.IO.File]::OpenRead($IconPath)
    try {
        $decoder = [System.Windows.Media.Imaging.IconBitmapDecoder]::new(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $bestFrame = $decoder.Frames | Sort-Object PixelWidth -Descending | Select-Object -First 1
    }
    finally {
        $stream.Dispose()
    }

    $iconSize = [double][Math]::Min($Width, $Height)
    $x = ($Width - $iconSize) / 2.0
    $y = ($Height - $iconSize) / 2.0

    $visual = [System.Windows.Media.DrawingVisual]::new()
    $ctx = $visual.RenderOpen()
    $ctx.DrawImage($bestFrame, [System.Windows.Rect]::new($x, $y, $iconSize, $iconSize))
    $ctx.Close()

    $rtb = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($Width, $Height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($visual)

    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $outStream = [System.IO.File]::Create($OutputPath)
    try {
        $encoder.Save($outStream)
    }
    finally {
        $outStream.Dispose()
    }
}

$makeappx = Find-WinSdkTool -ToolName "makeappx.exe"

& $dotnet test (Join-Path $root "App.sln") -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

$stageDir = Join-Path $root "out\msix-stage"
$assetsDir = Join-Path $stageDir "Assets"
$distDir = Join-Path $root "out\dist"
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$publishArgs = @(
    (Join-Path $root "src\Desktop\Desktop.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishReadyToRun=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-o", $stageDir
)

& $dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Get-ChildItem -Path $stageDir -Filter "*.pdb" -Recurse | Remove-Item -Force

$iconPath = Join-Path $root "src\Desktop\Assets\app.ico"
New-IconPng -IconPath $iconPath -OutputPath (Join-Path $assetsDir "StoreLogo.png") -Width 50 -Height 50
New-IconPng -IconPath $iconPath -OutputPath (Join-Path $assetsDir "Square44x44Logo.png") -Width 44 -Height 44
New-IconPng -IconPath $iconPath -OutputPath (Join-Path $assetsDir "Square150x150Logo.png") -Width 150 -Height 150
New-IconPng -IconPath $iconPath -OutputPath (Join-Path $assetsDir "Wide310x150Logo.png") -Width 310 -Height 150

$manifestTemplate = Get-Content (Join-Path $root "store\AppxManifest.template.xml") -Raw
$manifestContent = $manifestTemplate.Replace("{{VERSION}}", $msixVersion)
$manifestPath = Join-Path $stageDir "AppxManifest.xml"
[System.IO.File]::WriteAllText($manifestPath, $manifestContent, [System.Text.UTF8Encoding]::new($false))

$msixName = "$appId-v$version-win-x64.msix"
$msixPath = Join-Path $distDir $msixName
$hashPath = "$msixPath.sha256"
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }
if (Test-Path $hashPath) { Remove-Item $hashPath -Force }

& $makeappx pack /d $stageDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx pack failed with exit code $LASTEXITCODE"
}

if ($SelfSign) {
    $signtool = Find-WinSdkTool -ToolName "signtool.exe"
    $publisher = "CN=593A6B7E-EFE8-4B33-92F7-3F0D286D80B9"
    $cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $publisher -and $_.HasPrivateKey } |
        Select-Object -First 1
    if (-not $cert) {
        $cert = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature -FriendlyName "BreakKeeper Local Dev" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
        Write-Output "Created self-signed dev certificate: $($cert.Thumbprint)"
    }
    & $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $msixPath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed with exit code $LASTEXITCODE"
    }
}

$hash = (Get-FileHash -Path $msixPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $msixName" | Out-File -FilePath $hashPath -Encoding ascii -NoNewline

Write-Output "Created MSIX package: $msixPath"
Write-Output "Created SHA256 checksum: $hashPath"
