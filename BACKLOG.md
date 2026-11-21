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
- [x] Rename project and product identity to "Break Enforcer" with a single source of truth for display name, executable, AppData path, and startup task.
- [x] Stack break-complete action buttons (`Start Next Focus` and `Stop (Idle)`) vertically.
- [x] Support full arrow-key navigation with clearly visible focus indicators across all controls (audit every window and control for remaining keyboard-navigation gaps).
- [x] Centralize UI design tokens, shared `PhaseBadgeStyle` (`#8E8E93`), and outline-only button styles in `App.xaml`.
- [x] Restrict custom duration fields to valid numeric values (reject non-numeric, empty, or out-of-range inputs with immediate UI validation) and auto-select all text on focus for immediate replacement.
- [ ] Simplify Emergency Exit to a single 15s or 30s hold-only button (remove 24-character phrase): label `Emergency Exit (Hold)`, changing while pressed to `Hold (<remaining>s)` with a functional hold-progress animation (requires updating `AGENTS.md` and `UI.md` rule on approval).
- [ ] Surface remaining focus time without restoring the window (taskbar title/tooltip or system tray toggle).
- [ ] Open Question: Clarify functional progress animations (e.g., hold-button fill) vs. decorative animations in `~/.gemini/UI.md` (`Borderless-Filled` styling is now adopted in `~/.gemini/UI.md`).

## P4 — Release, Monetization, Cross-Device Sync & Deferred Ideas (Out of MVP)
- [ ] Release & Freemium monetization: maximize revenue via freemium model (e.g., fixed focus/break presets free, custom focus/break durations paid via lifetime unlock or similar).
- [ ] User-created custom presets (or minimal alternative: persist last-used focus/break durations across restarts without preset-management UI bloat).
- [ ] Cross-app product philosophy — user freedom & customizability: give users maximum freedom to customize settings and workflows across all apps.
- [ ] Cross-app monetization & transparent suffering-reduction pledge: transparently donate a share of revenue to highly effective suffering-reduction organizations and present it as a selling point if it increases total user spend (maximizing total spend toward personal revenue + effective suffering-reduction impact).
- [ ] Companion Android (and optionally iOS) app with automatic cross-device sync so tablet and PC enter break mode simultaneously.
- [x] Maintain permanent Cold Turkey `.txt` blacklist gaps (Mediatheken e.g. ZDF, Higgsfield AI short films, DAZN, UEFA) for PC and compatible mobile blocker.
