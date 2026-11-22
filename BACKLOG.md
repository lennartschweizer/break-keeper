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
- [x] Centralize fixed button size tokens and shared styles in `App.xaml` (remove per-button ad-hoc `Height` overrides across all windows) and enforce token-based button sizing in `UI.md`.
- [x] Rebuild `out/release/BreakKeeper.App.exe` so the autostart executable includes all latest UI fixes.
- [x] Replace internal `hold-only` phrasing in `README.md` with end-user wording (`15-second hold emergency exit`).
- [x] Remove `real` qualifier from tagline in `README.md` (`A minimalist Windows focus timer that enforces breaks.`).
- [x] Add isolated preview startup flags (`--preview-prewarning`, `--preview-break-complete`) and a preview launcher script for testing all windows and states without affecting the live session.
- [x] Fix idle arrow-key navigation and initial focus recovery in the main window (arrow keys between inputs and Start button work immediately without clicking a text field first).
- [x] Audit and improve the contrast/surface color of the "+5 min" button in the pre-warning banner so it stands out clearly against the banner background.
- [x] Increase default pre-warning banner display duration from 10s to 15s.

## P4 — Release, Monetization, Cross-Device Sync & Deferred Ideas (Out of MVP)
- [x] Free public Windows release (Phase 1 — GitHub Releases): package Release build archive with README usage/SmartScreen notes. (`v1.0.0`, `3ffbbbc`)
- [x] Enable NuGetAudit in build configuration and add GitHub Dependabot/CodeQL security workflows.
- [ ] Harden NuGet and .NET build security (enable `packages.lock.json` with CI `--locked-mode`, treat `NU1901`–`NU1904` warnings as errors, and enable latest .NET analyzers).
- [ ] Harden GitHub repository and workflows (pin Actions to SHA hashes, enforce read-only default token permissions, add `SECURITY.md`, enable Secret Scanning/Push Protection, and add OpenSSF Scorecard workflow).
- [ ] Move GitHub Release builds to ephemeral CI workflows with SLSA provenance generation.
- [ ] Free public Windows release (Phase 2 — Microsoft Store): package as MSIX with manifest startup task and submit via free individual developer account.
- [ ] Freemium monetization tied to future Pro features (e.g., cross-device sync, scheduling, call detection) rather than paywalling existing timer inputs.
- [ ] Add a dedicated settings view to customize all timer, pre-warning (including banner display duration), extension, hold-duration, autostart, and behavior preferences.
- [ ] User-created custom presets (or minimal alternative: persist last-used focus/break durations across restarts without preset-management UI bloat).
- [ ] Companion Android (and optionally iOS) app with automatic cross-device sync so tablet and PC enter break mode simultaneously.
- [x] Maintain permanent Cold Turkey `.txt` blacklist gaps (Mediatheken e.g. ZDF, Higgsfield AI short films, DAZN, UEFA) for PC and compatible mobile blocker.
