# Break Keeper

A minimalist Windows focus timer that enforces breaks.

Set your focus and break durations and start a session. Two minutes before a break begins, a small warning lets you wrap up or extend the session once by 5 minutes. When the break starts, Break Keeper locks all screens with a fullscreen overlay that cannot easily be bypassed or dismissed until the timer runs out — with a 15-second hold emergency exit if you genuinely need to get back to work.

## Installation

1. Download `BreakKeeper-v<version>-win-x64.zip` from the latest GitHub Release.
2. Extract the archive to a folder of your choice.
3. Run `BreakKeeper.App.exe`.

> **Windows SmartScreen:** Because the GitHub Release binary is currently unsigned, Windows Defender SmartScreen may show a warning on first launch. Click **More info** → **Run anyway**.

## Build & Package From Source

Requirements: Windows 10/11 (x64) and .NET 8 SDK.

```powershell
# Run tests and build the portable release ZIP + SHA256 checksum
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
```

Output artifacts are written to `out\dist\`.

## Local Data

Session state, last-used durations, and the emergency-exit audit log are stored in `%LocalAppData%\BreakKeeper\`.

## License

Licensed under the [MIT License](LICENSE).
