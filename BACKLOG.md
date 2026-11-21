# Backlog

## P1 — Architecture & Design Alignment
- [x] Confirm finalized MVP architecture (C# / WPF .NET 8, `WH_KEYBOARD_LL` break lock, 24-char + 5s emergency exit, 50/10 & 25/5 timing, no Cold Turkey CLI in MVP).
- [x] Create project-specific `AGENTS.md` and `UI.md`.

## P2 — Core MVP (Minimal Scope)
- [x] Install .NET 8 SDK (only .NET 8 Runtime currently present) and scaffold C# / WPF (.NET 8) solution under `C:\Entwicklung\focus-enforcer`.
- [x] Implement deterministic focus/break state machine (configurable presets e.g. 50/10, 25/5; optional 2-min pre-warning + max 1x +5 min extension) with crash/restart persistence.
- [x] Implement compact main control window.
- [x] Implement fullscreen break overlay (`HWND_TOPMOST` + `WH_KEYBOARD_LL` blocking `Alt+Tab`, `Win`, `Alt+F4`).
- [x] Implement high-friction emergency exit (random 24-character phrase + 5s hold + audit log).

## P3 — Hardening & Verification
- [x] Add automated tests for state machine transitions, extension limits, and persistence.
- [ ] Verify fullscreen lock behavior on sleep/wake.
- [ ] Rename project and product identity to "Break Enforcer" with a single source of truth for display name, executable, AppData path, and startup task.

## P4 — Deferred Blocking & Companion Ideas (Out of MVP)
- [x] Maintain permanent Cold Turkey `.txt` blacklist gaps (Mediatheken e.g. ZDF, Higgsfield AI short films, DAZN, UEFA) for PC and compatible mobile blocker.
