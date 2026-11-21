param(
    [switch]$Disable
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$appId = $props.Project.PropertyGroup.AppId
if ([string]::IsNullOrWhiteSpace($appId)) {
    throw "AppId missing in Directory.Build.props"
}

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$legacyNames = @("FocusEnforcer", "BreakEnforcer")
$legacyShortcutNames = @("Focus Enforcer", "Break Enforcer")
$taskName = $appId
$displayName = "Break Keeper"
$exePath = Join-Path $root "out\release\$appId.App.exe"
$programsDir = [Environment]::GetFolderPath("Programs")
$shortcutPath = Join-Path $programsDir "$displayName.lnk"

foreach ($legacyShortcut in $legacyShortcutNames) {
    $legacyLnk = Join-Path $programsDir "$legacyShortcut.lnk"
    if (Test-Path $legacyLnk) {
        Remove-Item $legacyLnk -Force
    }
}

foreach ($legacyName in $legacyNames) {
    if (Get-ItemProperty -Path $runKey -Name $legacyName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $runKey -Name $legacyName
    }
    $legacyTask = Get-ScheduledTask -TaskName $legacyName -ErrorAction SilentlyContinue
    if ($legacyTask) {
        Unregister-ScheduledTask -TaskName $legacyName -Confirm:$false
    }
}
if (Get-ItemProperty -Path $runKey -Name $taskName -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $runKey -Name $taskName
}

if ($Disable) {
    if (Test-Path $shortcutPath) {
        Remove-Item $shortcutPath -Force
    }
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existingTask) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Output "Removed $taskName from Windows startup."
    } else {
        Write-Output "$taskName is not registered in Windows startup."
    }
    exit 0
}

if (-not (Test-Path $exePath)) {
    throw "Release executable not found at $exePath"
}

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory (Split-Path -Parent $exePath)
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -MultipleInstances IgnoreNew
$settings.Priority = 4
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Limited

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Force | Out-Null

$registered = Get-ScheduledTask -TaskName $taskName -ErrorAction Stop
$registeredAction = ($registered.Actions | Select-Object -First 1).Execute
if ($registeredAction -ne $exePath) {
    throw "Scheduled task verification failed: expected $exePath, got $registeredAction"
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = Split-Path -Parent $exePath
$shortcut.IconLocation = "$exePath,0"
$shortcut.Save()

Write-Output "Registered $taskName scheduled logon task: $registeredAction"
Write-Output "Created Start Menu shortcut: $shortcutPath"
