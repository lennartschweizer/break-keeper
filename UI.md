# Break Enforcer — Project UI Specification

Inherits all visual tokens, typography, and interaction rules from `~/.gemini/UI.md`.

## Main Control Window

- Compact single-view layout directly on `#000000` background (no outer card or bounding box; `#101010`/`#181818` reserved for controls and inputs) with fixed dimensions to prevent layout shift.
- Centered monospace countdown timer (`#FFFFFF`) above a single secondary state badge (`#8E8E93`).
- Preset selector (`50/10`, `25/5`, custom) and start control shown only while idle (`Collapsed` when active so the timer centers vertically).
- Pre-warning state (2 minutes before break) surfaces a single monochrome `+5 min` extension control (`#FFFFFF` fill, `#000000` text), disabled after one use per cycle.
- Non-activating bottom-right pre-warning banner (`#000000` surface, `#262626` border) surfaces a white `Break in 2 min` notice, white `+5 min` extension, and dismiss control for 10 seconds with a subtle fade-in/fade-out and no focus steal.

## Fullscreen Break Overlay

- Full-bleed `#000000` topmost window spawned independently on every active monitor.
- Centered monospace remaining break time (`#FFFFFF`) above a single phase badge (`BREAK` / `BREAK COMPLETE`); zero decorative elements or external links.
- Unobtrusive emergency-exit trigger at bottom center; activating it reveals the random 24-character monospace challenge and 5-second hold control (`#FF453A`).
