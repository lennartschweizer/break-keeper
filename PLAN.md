# Focus Enforcer — Project Plan

## Problem & Context
- **Goal:** Overcome university task procrastination, enforce regular restorative breaks, and build tolerance for boredom without turning tool-building itself into procrastination.
- **Separation from Content Blocking:** Permanent website/app blocking already runs separately via Cold Turkey Blocker (PC) and a Cold Turkey `.txt`-compatible blocker app on Android.

## Core Design Principles
1. **Enforced Breaks (Hard Lock):** Fullscreen break overlay across all monitors (`HWND_TOPMOST` + `WH_KEYBOARD_LL` blocking `Alt+Tab`, `Win`, `Alt+F4`) until the break timer expires.
2. **High-Friction Emergency Exit:** Deliberate exit requires typing a random 24-character phrase plus a 5-second button hold, recorded in an audit log.
3. **Configurable Timing Model:** Presets (e.g., 50/10, 25/5) with optional 2-minute pre-warning and at most one `+5 min` extension before hard cutoff.
4. **Minimal MVP Scope:** Keep `focus-enforcer` strictly focused on timer, persistence, and break lock; manage blacklists (Mediatheken/ZDF, Higgsfield, DAZN, UEFA) and strict whitelists directly in Cold Turkey Blocker.

## Decided Architecture
- **Tech Stack:** C# / WPF (.NET 8) for native Win32 multi-monitor control, low-level keyboard hooks, and single-file deployment.
- **Deferred (Out of MVP):** Cold Turkey CLI integration, Gemini web/mobile blocking automation, and Android blocker app release.
