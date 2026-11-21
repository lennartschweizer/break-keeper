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
- [x] Verify fullscreen lock behavior on sleep/wake.
- [x] Rename project and product identity to "Break Keeper" (via "Break Enforcer") with a single source of truth for display name, executable, AppData path, and startup task.
- [x] Stack break-complete action buttons (`Start Next Focus` and `Stop (Idle)`) vertically.
- [x] Support full arrow-key navigation with clearly visible focus indicators across all controls (audit every window and control for remaining keyboard-navigation gaps).
- [x] Centralize UI design tokens, shared `PhaseBadgeStyle` (`#8E8E93`), and outline-only button styles in `App.xaml`.
- [x] Restrict custom duration fields to valid numeric values (reject non-numeric, empty, or out-of-range inputs with immediate UI validation) and auto-select all text on focus for immediate replacement.
- [x] Simplify Emergency Exit to a single 15s hold-only button (remove 24-character phrase): label `Emergency Exit (Hold)`, changing while pressed to `Hold (<remaining>s)` without progress animation (`AGENTS.md` and `UI.md` updated).
- [x] Surface remaining focus time without restoring the window (transparent system tray icon with remaining-time tooltip and click toggle).
- [x] Hide main window from the taskbar when minimized or toggled to tray.
- [x] Open Question: Clarify functional progress animations vs. decorative animations in `~/.gemini/UI.md` (`Borderless-Filled` styling adopted; hold-button animation removed so `Zero decorative noise` remains strict).

## P4 — Release, Monetization, Cross-Device Sync & Deferred Ideas (Out of MVP)
- [x] Free public Windows release (Phase 1 — GitHub Releases): package Release build archive with README usage/SmartScreen notes. (`v1.0.0`, `3ffbbbc`)
- [ ] Enable NuGetAudit in build configuration and add GitHub Dependabot/CodeQL security workflows.
- [ ] Free public Windows release (Phase 2 — Microsoft Store): package as MSIX with manifest startup task and submit via free individual developer account.
- [ ] Freemium monetization tied to future Pro features (e.g., cross-device sync, scheduling, call detection) rather than paywalling existing timer inputs.
- [ ] User-created custom presets (or minimal alternative: persist last-used focus/break durations across restarts without preset-management UI bloat).
- [ ] Companion Android (and optionally iOS) app with automatic cross-device sync so tablet and PC enter break mode simultaneously.
- [x] Maintain permanent Cold Turkey `.txt` blacklist gaps (Mediatheken e.g. ZDF, Higgsfield AI short films, DAZN, UEFA) for PC and compatible mobile blocker.
