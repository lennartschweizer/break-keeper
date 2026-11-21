## Project Architecture

Target .NET 8 C# WPF on Windows for the desktop application.
Decouple timer state transitions from platform UI layers.
Persist active session state atomically to local storage.
Enforce break locks across all monitors via topmost overlays and low-level keyboard hooks.
Restrict emergency break exits to a 24-character challenge plus a 5-second hold.
Append every emergency break exit to an audit log.
Exclude Cold Turkey Blocker CLI automation from the MVP.
Publish release builds with ReadyToRun and register login autostart via a zero-delay Scheduled Task.
