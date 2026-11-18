# Backlog

## P1 — Architecture & Design Alignment
- [x] Confirm finalized MVP architecture (C# / WPF .NET 8, `WH_KEYBOARD_LL` break lock, 24-char + 5s emergency exit, 50/10 & 25/5 timing, no Cold Turkey CLI in MVP).
- [x] Create project-specific `AGENTS.md` and `UI.md`.

## P2 — Core MVP (Minimal Scope)
- [ ] Install .NET 8 SDK (only .NET 8 Runtime currently present) and scaffold C# / WPF (.NET 8) solution under `C:\Entwicklung\focus-enforcer`.
- [ ] Implement deterministic focus/break state machine (configurable presets e.g. 50/10, 25/5; optional 2-min pre-warning + max 1x +5 min extension) with crash/restart persistence.
- [ ] Implement compact main control window with optional single-line 2-minute entry-step prompt before session start.
- [ ] Implement multi-monitor fullscreen break overlay (`HWND_TOPMOST` per monitor + `WH_KEYBOARD_LL` blocking `Alt+Tab`, `Win`, `Alt+F4`).
- [ ] Implement high-friction emergency exit (random 24-character phrase + 5s hold + audit log).

## P3 — Hardening & Verification
- [ ] Add automated tests for state machine transitions, extension limits, and persistence.
- [ ] Verify multi-monitor fullscreen lock behavior and edge cases (sleep/wake, display disconnect).

## P4 — Deferred Blocking & Companion Ideas (Out of MVP)
- [x] Maintain permanent Cold Turkey `.txt` blacklist gaps (Mediatheken e.g. ZDF, Higgsfield AI short films, DAZN, UEFA) for PC and compatible mobile blocker.
- [ ] Configure strict Cold Turkey whitelist profile for deep work blocks (including blocking Gemini web app).
- [ ] Investigate blocking Gemini mobile app during strict work blocks.
- [ ] Polish and release existing Android blocker app (deferred until after university phases).
- [ ] Optional: Cold Turkey Blocker CLI adapter (`-status`, `-list-blocks`, `-start`, `-lock`, `-stop`) if automated profile switching is later desired.
