# Focus Enforcer — Project UI Specification

Inherits all visual tokens, typography, and interaction rules from `~/.gemini/UI.md`.

## Main Control Window

- Compact single-view layout (`#101010` surface on `#000000` background) with fixed dimensions to prevent layout shift.
- Centered monospace countdown timer (`#FFFFFF`) above a single state badge (`#30D158` active focus, `#FF9F0A` pre-warning, `#8E8E93` idle).
- Preset selector (`50/10`, `25/5`, custom) with inverted active state (`#FFFFFF` fill, `#000000` text).
- Optional single-line entry-step input (`Next 2-min step`) visible in `Idle` state to lower task-initiation friction.
- Pre-warning state (2 minutes before break) surfaces a single `+5 min` extension control (`#FF9F0A`), disabled after one use per cycle.

## Fullscreen Break Overlay

- Full-bleed `#000000` topmost window spawned independently on every active monitor.
- Centered monospace remaining break time (`#FFFFFF`) and minimal secondary status line (`#8E8E93`); zero decorative elements or external links.
- Unobtrusive emergency-exit trigger at bottom center; activating it reveals the random 24-character monospace challenge and 5-second hold control (`#FF453A`).
