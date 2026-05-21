# PushToOpen

Voice-activated push-to-talk for Windows. When your microphone goes above a configurable threshold, PushToOpen virtually holds a key of your choosing; when you go quiet, it releases. The key, the threshold, the timing, and the input device are all configurable from a clean WinUI 3 desktop app.

## Highlights

- Real-time RMS + peak metering with a smoothed VU bar and a moving threshold line
- Threshold state machine with **attack**, **release**, and **debounce** windows
- Reliable Windows `SendInput`-based key simulation (with scan codes + extended-key handling); supports keyboard buttons and mouse buttons 1–5
- Settings auto-persist (debounced JSON write to `%LocalAppData%\PushToOpen\settings.json`)
- Compact floating overlay window (drag-move, always-on-top, transparent acrylic) for at-a-glance level monitoring
- System tray with mute, enable/disable, threshold presets, exit
- Optional **launch on Windows startup** (per-user `HKCU\…\Run`)
- Device hot-swap: disconnect/reconnect the mic and PushToOpen rebinds automatically
- Fail-safe: any unexpected exit or device loss releases the virtual key

## Requirements

- Windows 10 19041+ (build 19041) or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- The **.NET Desktop Development** workload from the Visual Studio installer, or install the Windows App SDK runtime separately from <https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads>

## Build

From the repo root:

```powershell
dotnet restore
dotnet build .\PushToOpen.sln -c Release -p:Platform=x64
```

Or open `PushToOpen.sln` in Visual Studio 2022 (17.8+) and press **F5**. Pick the `x64` configuration.

## Run

```powershell
dotnet run --project .\src\PushToOpen\PushToOpen.csproj -c Release -p:Platform=x64
```

Pass `--tray` to start hidden in the system tray:

```powershell
.\PushToOpen.exe --tray
```

## Publish (single-folder, unpackaged)

```powershell
dotnet publish .\src\PushToOpen\PushToOpen.csproj `
    -c Release -p:Platform=x64 `
    -p:WindowsPackageType=None `
    -p:SelfContained=true `
    --runtime win-x64 `
    --output .\dist\PushToOpen
```

The output folder can be zipped and run on any Windows 10 19041+ machine without installing the SDK.

## Architecture

```
src/PushToOpen/
├── App.xaml(.cs)                   – DI container bootstrap, lifecycle, overlay management
├── Program.cs                      – STA entry point
├── MainWindow.xaml(.cs)            – Shell, navigation, tray icon, status bar
├── Views/OverlayWindow.xaml(.cs)   – Floating compact level meter
├── Controls/VuMeter.cs             – Custom level + threshold control
├── Styles/                         – Dark theme tokens, card / nav / button styles
├── Models/                         – AppSettings, AudioDeviceInfo, KeyBindingInfo
├── Services/
│   ├── SettingsService             – Debounced JSON persistence
│   ├── AudioCaptureService         – WASAPI capture, RMS / peak, device hot-swap
│   ├── ThresholdEngine             – Attack / release / debounce state machine
│   ├── InputSimulator              – SendInput-based key down/up (guaranteed release)
│   ├── HotkeyCaptureService        – Low-level kbd + mouse hook for rebinding
│   ├── StartupService              – HKCU Run key registration
│   ├── TrayService                 – Tray state surface (UI lives in MainWindow.xaml)
│   └── PushToTalkCoordinator       – Wires audio → engine → input
├── ViewModels/                     – MVVM (CommunityToolkit.Mvvm)
└── Utilities/                      – Dispatcher marshalling, converters, key code mapping
```

### Data flow

```
WASAPI capture ─► AudioCaptureService ─► ThresholdEngine ─► InputSimulator
                          │                     │
                          ▼                     ▼
                  HomeViewModel           Tray / Overlay UI
```

The coordinator owns the wiring: audio levels feed the engine, gate-open/close events drive the input simulator. The engine is pure C# (no NAudio dependency), making it straightforward to unit-test.

### Reliability invariants

1. **Never leak a held key.** `PushToTalkCoordinator` subscribes to `AppDomain.ProcessExit` and `UnhandledException` and releases the simulator before exit. `InputSimulator.Dispose()` also releases.
2. **Audio thread isolation.** UI updates from the WASAPI callback are marshalled to the UI dispatcher via `DispatcherHelper`. NAudio is never called from the UI thread.
3. **Device hot-swap.** `AudioCaptureService` registers an `IMMNotificationClient`; device add/remove/default-change triggers a refresh and the next capture cycle picks up the new endpoint.

## Settings file

`%LocalAppData%\PushToOpen\settings.json` — written atomically with a debounce (~400 ms). Delete it to reset to defaults.

## License

MIT.
