# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

This repo currently contains only `PROMPT.md` — a product specification. No source, no solution, no build system exists yet. The spec is the source of truth for what to build; everything else must be created.

Do not invent commands that imply an existing build (`dotnet build`, `dotnet test`, etc.) until the solution actually exists on disk. Once the solution is scaffolded, update this file with the real commands.

## Product: PushToOpen

Windows desktop app that converts voice activity into automatic push-to-talk. Microphone RMS above threshold → virtually hold a configured key. RMS below threshold for the release delay → release the key. See `PROMPT.md` for the full spec; the points below capture the constraints that are easy to violate.

### Required stack (non-negotiable per spec)

- C# / .NET 8
- WinUI 3 for UI
- NAudio for audio capture
- MVVM with DI
- No placeholder code, no TODO comments, no mock UI — spec demands a production-quality build that compiles and runs immediately

### Architecture intent

The spec mandates *separate services* for audio, hotkeys, settings, and overlay — wired through DI, consumed by ViewModels, with strongly typed Models. Keep these seams when adding features; do not let audio logic leak into ViewModels or XAML code-behind.

Expected service responsibilities:

- **Audio service** — NAudio capture, RMS computation, peak smoothing, gain, noise gate. Runs on its own thread/timer; must not block the UI thread. Owns device enumeration and hot-swap on disconnect.
- **Threshold/state machine** — consumes audio levels, applies threshold + attack delay + release delay + debounce, emits "press" / "release" events. Keep this pure and testable; it is the core behavior of the product.
- **Hotkey / input simulation service** — Windows input simulation (SendInput-based) for the bound key. Must guarantee the key is released on shutdown, device loss, or app crash to prevent stuck-key bugs (called out explicitly in spec §7).
- **Settings service** — persist/load all user config (device, threshold, delays, hotkey, overlay, tray, startup). Auto-save on change.
- **Overlay service / window** — optional always-on-top compact window showing live level; toggled independently of main window.
- **Tray service** — minimize-to-tray, mute toggle, enable/disable detection, threshold presets, exit.

### Design constraints (spec §"Design Requirements")

The UI bar is high: dark mode default, acrylic, rounded window, smooth animations, custom toggles/sliders, no default WinUI control look. When adding UI, restyle controls — do not ship stock WinUI chrome. The spec explicitly rejects "generic Microsoft demo styling."

### Reliability invariants

These are easy to regress and matter more than features:

1. **Never leave a key stuck down.** Any path that issues key-down must guarantee a paired key-up: app exit, device disconnect, settings change, unhandled exception, sleep/resume.
2. **Audio thread isolation.** UI must remain responsive while audio polls. Marshal level updates to the UI thread; never call NAudio APIs from the UI thread.
3. **Device hot-swap.** Disconnecting/reconnecting the mic must not crash or silently stop detection — the audio service must rebind.

## Commands

To be filled in once the solution is scaffolded. Likely shape:

- Build: `dotnet build` (from solution root)
- Run: `dotnet run --project src/PushToOpen` or launch from Visual Studio
- Test: `dotnet test` (no test project exists yet)

Update this section when those become real.
