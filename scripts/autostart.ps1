param(
    [switch]$Disable
)

$ErrorActionPreference = "Stop"

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$entryName = "FocusEnforcer"
$taskName = "FocusEnforcer"
$root = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $root "out\release\FocusEnforcer.App.exe"

if (Get-ItemProperty -Path $runKey -Name $entryName -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $runKey -Name $entryName
}

if ($Disable) {
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

Write-Output "Registered $taskName scheduled logon task: $registeredAction"
