# Break Keeper — Project UI Specification

Inherits all visual tokens, typography, and interaction rules from `~/.gemini/UI.md`.

## Shared Controls

- All buttons inherit fixed height and padding from centralized size tokens (`ButtonHeightStandard` 44px for primary/secondary actions, `ButtonHeightCompact` 32px for banner/utility actions, `IconButtonSizeCompact` 28px for compact icon actions, `TitleBarButtonStyle` 40x36px); per-button ad-hoc `Height` overrides in window XAML are banned.

## Main Control Window

- Compact single-view layout directly on `#000000` background (no outer card or bounding box; `#101010`/`#181818` reserved for controls and inputs) with fixed dimensions to prevent layout shift.
- Centered monospace countdown timer (`#FFFFFF`) above a single secondary state badge (`#8E8E93`, shared `PhaseBadgeStyle`).
- Fixed `Focus (min)` / `Break (min)` inputs (persisting last-used valid durations across restarts, defaulting to `25` / `5`) and start control shown only while idle (`Collapsed` when active so the timer centers vertically, zero layout shift while editing).
- Borderless filled action and preset buttons (`#101010`/`#181818` resting fill, `#262626` focus fill, `#C7C7CC` fill with `#000000` text for active selection, `#FFFFFF` fill when active selection is focused; primary actions use `#FFFFFF` text, secondary actions `#8E8E93` until hovered or focused).
- Pre-warning state (2 minutes before break) surfaces a single monochrome `+5 min` filled control, disabled after one use per cycle.
- Non-activating bottom-right pre-warning banner (`#000000` surface without border) surfaces a white `Break in 2 min` notice, filled `+5 min` extension, and dismiss control for 15 seconds with a subtle fade-in/fade-out and no focus steal.
- System tray icon displays remaining focus time in its tooltip and toggles the main window on left-click; minimizing or toggling to tray hides the main window from the taskbar.

## Fullscreen Break Overlay

- Full-bleed `#000000` topmost window spawned independently on every active monitor.
- Centered monospace remaining break time (`#FFFFFF`) above a single phase badge (`BREAK` / `BREAK COMPLETE`); zero decorative elements or external links.
- Unobtrusive single `Emergency Exit (Hold)` control at bottom center; pressing and holding for 15 seconds transitions its label to `Hold (<remaining>s)`.
