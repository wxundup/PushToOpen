# PushToOpen

Voice-activated push-to-talk for Windows. Your microphone level crosses a threshold → PushToOpen virtually holds a key you've bound. You go quiet → it releases. Threshold, key, timing, input device, monitor, noise suppression, window-restriction, theme — all configurable from a clean WinUI 3 desktop app.

## Highlights

### Core
- Real-time RMS + peak metering with a smoothed gradient VU bar + moving threshold line
- Threshold state machine with **attack**, **release**, and **debounce** windows
- Reliable Windows `SendInput`-based key simulation (scan codes + extended-key handling); keyboard or mouse buttons 1–5
- Settings auto-persist (debounced JSON write to `%LocalAppData%\PushToOpen\settings.json`)
- Compact floating **overlay** window with **lock-position** toggle, smooth pointer-capture drag, live mute icon, dB readout
- System tray with Open / threshold presets / Exit (Command-bound, namescope-safe)
- Optional **launch on Windows startup** (per-user `HKCU\…\Run`)
- Device hot-swap: disconnect/reconnect the mic and PushToOpen rebinds automatically
- Fail-safe: any unexpected exit, device loss, or close releases the virtual key

### Audio
- **Hear yourself** — pipe your mic to your default output for monitoring (with its own gain). Use headphones.
- **Voice noise suppression** — pure C# FFT-based spectral subtraction. Hand-rolled Cooley-Tukey, periodic-Hann analysis at 50% hop (COLA), per-bin noise floor learned during low-energy frames, oversubtraction with smoothed Wiener gain. Cleans steady-state hiss + fan noise from the threshold detection and monitor path. (Other apps still see your raw mic — see *honest limits* below.)
- Input gain trim + noise gate floor

### Hotkeys
- **Push-to-talk** binding (keyboard or mouse, default `V`)
- **Mute toggle** global hotkey — separate key flips Mute on/off from any focused window
- Both bind via in-app capture (Esc cancels)

### Window restriction
- "Window" tab — pick an open app. PushToOpen only fires while that process owns the foreground window. Tab to anything else → gate stays closed, no key. Auto-resumes when you focus it again.

### Themes
- Six built-in palettes — **Indigo Night**, **Phosphor**, **Crimson**, **Aurora**, **Sunset**, **Mono**
- Custom **PNG/JPG background image** with adjustable opacity (translucent watermark — unpackaged WinUI 3 can't do real backdrop blur)
- **Layout modes**: full Sidebar or Compact (icon-only nav rail)

## Honest limits

PushToOpen is a **key simulator**, not a virtual audio device. It reads your microphone to decide whether to press a key — it doesn't replace what Discord, Teams, Zoom, etc. capture. That means:

- The noise-suppression toggle improves *threshold accuracy* (no false triggers from fan noise) + the *Hear-yourself* monitor playback. It does **not** clean up the mic stream other apps record.
- For system-wide neural noise suppression (Krisp-grade), use Discord's built-in Krisp toggle (Voice Settings → Noise Suppression → Krisp), NVIDIA Broadcast, or a virtual cable + VoiceMeeter.

## Requirements

- Windows 10 build 19041+ or Windows 11, x64
- Self-contained published binaries need **no .NET install**
- Building from source: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run prebuilt (casual users)

The `dist/` folder in the repo holds a ready-to-run self-contained build:

```
dist/
├── Start PushToOpen.lnk         ← double-click this
├── Install Desktop Shortcut.bat ← optional: adds a Desktop shortcut
├── README.txt
└── PushToOpen/                  ← program + DLLs (don't touch)
```

Zip the whole `dist/` folder to share. Recipient unzips anywhere, double-clicks `Start PushToOpen.lnk`. No installer, no admin, no runtime download.

## Build from source

From the repo root:

```powershell
dotnet restore
dotnet msbuild .\src\PushToOpen\PushToOpen.csproj -p:Configuration=Release -p:Platform=x64
```

Or open `PushToOpen.sln` in Visual Studio 2022 (17.8+) and press **F5**. **Platform must be `x64`** — WindowsAppSDK self-contained mode rejects `AnyCPU`.

Single-folder self-contained publish:

```powershell
dotnet publish .\src\PushToOpen\PushToOpen.csproj `
    -c Release -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true `
    -p:SelfContained=true `
    --runtime win-x64 `
    --output .\dist\PushToOpen
```

Both `<SelfContained>true</SelfContained>` and `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` are required — the second bundles the WinAppSDK native DLLs alongside the .NET runtime.

## First-run tips

1. Open the app.
2. **Audio** tab → pick your microphone.
3. **Hotkey** tab → bind the key PushToOpen should hold while you talk (default `V`).
4. **Live** tab → speak. Drag *Threshold* so the gate opens when you talk and closes when you're silent.
5. (Optional) **Hear yourself** under Audio to verify what the noise suppressor is doing. *Headphones*.
6. (Optional) **Window** tab → pin PushToOpen to one app so it doesn't fire while you type in other windows.
7. (Optional) **Themes** tab → pick a palette + background image + layout.

## Architecture

```
src/PushToOpen/
├── App.xaml(.cs)                   – DI bootstrap, theme apply, lifecycle, overlay mgmt, global hotkey wiring
├── Program.cs                      – STA entry point
├── MainWindow.xaml(.cs)            – Shell, tray icon (Command-bound), status bar
├── Views/
│   ├── MainShellView.xaml(.cs)     – Sidebar / content / status, layout-mode switching, theme bg image
│   ├── OverlayView.xaml(.cs)       – Compact pod with lock toggle + mute icon
│   └── OverlayWindow.xaml(.cs)     – Borderless window + pointer-capture drag
├── Controls/VuMeter.cs             – Custom level + threshold + active-glow control
├── Styles/                         – Color brushes + control templates (live-mutated by ThemeService)
├── Models/
│   ├── AppSettings                 – Persistent state for everything below
│   ├── AudioDeviceInfo, KeyBindingInfo, WindowInfo
│   └── ThemeDefinition             – Palette + accent gradient stops
└── Services/
    ├── SettingsService             – Debounced atomic JSON persistence (tmp → rename)
    ├── AudioCaptureService         – WASAPI capture, RMS / peak, device hot-swap, monitor, NS pipeline
    ├── NoiseSuppressor             – Hand-rolled FFT + spectral subtraction
    ├── ThresholdEngine             – Attack / release / debounce / mute / app-gate state machine
    ├── InputSimulator              – SendInput key down/up (down stays true on failure → retries)
    ├── HotkeyCaptureService        – Low-level kbd + mouse hook for binding capture
    ├── GlobalHotkeyListener        – Always-on hook listening for the mute-toggle binding
    ├── ForegroundWatcher           – Polls GetForegroundWindow every 250ms
    ├── WindowEnumerator            – EnumWindows + DWM-cloak / tool-window filtering
    ├── ThemeService                – Six palettes, live brush.Color mutation
    ├── StartupService              – HKCU Run key registration
    └── PushToTalkCoordinator       – Wires audio → engine → input + foreground → app-gate + settings → everything
```

### Data flow

```
WASAPI capture ─► AudioCaptureService ─► [optional NS] ─► ThresholdEngine ─► InputSimulator
                          │                                       │
                          ▼                                       ▼
                  ViewModels                              Tray / Overlay UI

GetForegroundWindow (250ms poll) ─► ForegroundWatcher ─► Coordinator.ApplyAppGate
                                                              │
                                                              ▼
                                                       ThresholdEngine.SetAppGate

Low-level hook (mute-toggle key) ─► GlobalHotkeyListener ─► Task.Run ─► Settings.Mutate(Muted = !Muted)
```

### Reliability invariants

1. **Never leak a held key.** `PushToTalkCoordinator` subscribes to `AppDomain.ProcessExit` and `UnhandledException` and releases the simulator before exit. `InputSimulator.Dispose()` also releases. `OnTrayExit` releases + force-terminates.
2. **Audio thread isolation.** WASAPI `OnDataAvailable` reads shared doubles under `_gate` lock; UI updates marshal via `DispatcherHelper`. Noise suppressor processes in-place on the callback thread (cheap STFT, ~few % of one core).
3. **Device hot-swap.** `AudioCaptureService` registers `IMMNotificationClient`; device add/remove/default-change triggers `Task.Run(RefreshDevicesAsync)` (off the COM callback thread).
4. **Hook-thread safety.** Low-level keyboard hook callbacks (mute-toggle handler) bounce off the hook thread via `Task.Run` before doing settings mutation — keeps callbacks well under `LowLevelHooksTimeout`.

## Settings file

`%LocalAppData%\PushToOpen\settings.json` — atomic write (tmp → rename) with ~400 ms debounce. Delete to reset.

## License

MIT.
