Create a complete production-quality Windows desktop application called “PushToOpen”.

Goal:
The app converts voice activity into automatic push-to-talk behavior. When the microphone volume goes above a configurable threshold, the app virtually “holds” a selected push-to-talk key. When the volume drops below the threshold for a configurable delay, it releases the key.

Tech Stack Requirements:
- Language: C#
- Runtime: .NET 8
- UI Framework: WinUI 3
- Audio Library: NAudio
- Architecture: clean, modular, scalable
- Must compile and run immediately
- No placeholder code
- No mock UI
- No TODO comments
- No fake implementations

Design Requirements:
The UI must look like it was designed by a professional product designer, not a programmer.

Visual style:
- Extremely clean modern UI
- Soft rounded corners on the window
- Dark mode default
- Subtle layered depth
- Smooth hover animations
- Smooth transitions
- Glass/acrylic effects where appropriate
- Balanced spacing and alignment
- Premium typography hierarchy
- Modern audio-tool aesthetic
- Minimal but polished
- No clutter
- No ugly default WinUI controls
- No generic Microsoft demo styling

Core Features:
1. Real-time microphone monitoring
- Display live microphone input levels
- Smooth animated VU meter
- Peak indicator
- Adjustable sensitivity threshold

2. Push-to-talk automation
- User selects a keyboard key
- App simulates key down when threshold exceeded
- App simulates key up when volume falls below threshold
- Adjustable attack delay
- Adjustable release delay
- Adjustable debounce

3. Audio settings
- Select microphone input device
- Auto-refresh devices
- Noise gate slider
- Threshold slider
- Gain multiplier slider

4. Overlay mode
- Optional compact floating overlay
- Always-on-top mode [Toggleable]
- Minimal transparent design
- Live mic level display

5. Performance requirements
- Very low latency
- Efficient audio processing
- Low CPU usage
- Stable threading
- No UI freezing
- Async-safe architecture

6. UX polish
- Smooth animations everywhere
- Keyboard shortcut support
- Tooltips
- Save/load settings automatically
- System tray support
- Minimize to tray
- Startup launch option
- Clean onboarding defaults

7. Reliability
- Handle disconnected microphones safely
- Handle invalid devices
- Prevent stuck keys
- Graceful shutdown
- Thread-safe audio handling

Code Quality Requirements:
- Full MVVM architecture
- Separate services for audio, hotkeys, settings, and overlay
- Strongly typed models
- Dependency injection
- Proper async/await usage
- Clean project structure
- Well-organized folders
- Modern C# conventions
- No spaghetti code

Project Structure:
Create:
- Complete solution
- All project files
- All source files
- XAML layouts
- Services
- Models
- ViewModels
- Utilities
- App icons/placeholders
- Settings persistence
- README.md
- Build instructions

UI Requirements:
Main window layout:
- Left sidebar navigation
- Main settings/content panel
- Live mic visualization section
- Bottom status bar

Include:
- Animated threshold line
- Smooth sliders
- Custom toggle switches
- Device dropdowns
- Rebindable hotkey button
- Elegant cards/panels
- Consistent spacing system

Animation Requirements:
- Smooth fade transitions
- Slider animations
- Hover states
- Animated VU meter
- Acrylic effects
- Responsive resizing
- Smooth sidebar interactions

Audio Processing:
Implement:
- RMS volume detection
- Peak smoothing
- Adjustable polling interval
- Optional noise suppression logic
- Sensitivity calibration

Keyboard Simulation:
Implement proper Windows input simulation using reliable APIs.

Tray Features:
- Mute toggle
- Enable/disable threshold detection
- Quick threshold presets
- Exit app

Deliverables:
Generate the FULL application codebase.
Output full implementations.

The final result should feel like a premium commercial application, not a hobby project.