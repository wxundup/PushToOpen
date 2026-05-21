using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PushToOpen.Services;
using PushToOpen.Utilities;
using PushToOpen.ViewModels;
using PushToOpen.Views;

namespace PushToOpen;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public new static App Current => (App)Application.Current;

    public MainWindow? MainWindow { get; private set; }
    public OverlayWindow? OverlayWindow { get; private set; }
    internal EventHandler<PushToOpen.Models.AppSettings>? _overlaySettingsHandler;

    public App()
    {
        InitializeComponent();
        Services = BuildServices();
        UnhandledException += (_, e) =>
        {
            e.Handled = true;
            var msg = e.Exception?.ToString() ?? e.Message;
            System.Diagnostics.Debug.WriteLine("[PushToOpen FATAL] " + msg);
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PushToOpen", "crash.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.WriteAllText(logPath, DateTimeOffset.Now + "\n" + msg);
            }
            catch { }
            // crash logged
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        var coordinator = Services.GetRequiredService<PushToTalkCoordinator>();
        await coordinator.StartAsync();

        MainWindow = Services.GetRequiredService<MainWindow>();
        DispatcherHelper.Initialize(MainWindow.DispatcherQueue);

        bool startMinimized = settings.Current.StartMinimized
            || Array.Exists(Environment.GetCommandLineArgs(), a =>
                a.Equals("--tray", StringComparison.OrdinalIgnoreCase));

        if (startMinimized && settings.Current.MinimizeToTray)
        {
            MainWindow.Activate();
            MainWindow.HideToTray();
        }
        else
        {
            MainWindow.Activate();
        }

        if (settings.Current.ShowOverlay)
        {
            ShowOverlay();
        }

        _overlaySettingsHandler = (_, s) =>
        {
            DispatcherHelper.Post(() =>
            {
                if (s.ShowOverlay && OverlayWindow is null) ShowOverlay();
                else if (!s.ShowOverlay && OverlayWindow is not null) HideOverlay();
            });
        };
        settings.SettingsChanged += _overlaySettingsHandler;

        // Global mute-toggle hotkey: listen for the bound key + flip s.Muted.
        var muteListener = Services.GetRequiredService<IGlobalHotkeyListener>();
        muteListener.SetBinding(settings.Current.MuteToggleHotkey);
        muteListener.Triggered += (_, _) =>
        {
            settings.Mutate(s => s.Muted = !s.Muted);
        };
        muteListener.Start();
        settings.SettingsChanged += (_, s) => muteListener.SetBinding(s.MuteToggleHotkey);
    }

    public void ShowOverlay()
    {
        if (OverlayWindow is not null) return;
        OverlayWindow = Services.GetRequiredService<OverlayWindow>();
        OverlayWindow.Closed += (_, _) => OverlayWindow = null;
        OverlayWindow.Activate();
    }

    public void HideOverlay()
    {
        OverlayWindow?.Close();
        OverlayWindow = null;
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<IThresholdEngine, ThresholdEngine>();
        services.AddSingleton<IInputSimulator, InputSimulator>();
        services.AddSingleton<IHotkeyCaptureService, HotkeyCaptureService>();
        services.AddSingleton<IGlobalHotkeyListener, GlobalHotkeyListener>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<PushToTalkCoordinator>();

        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<AudioViewModel>();
        services.AddSingleton<HotkeyViewModel>();
        services.AddSingleton<OverlayViewModel>();
        services.AddSingleton<AppPreferencesViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<OverlayWindow>();

        return services.BuildServiceProvider();
    }
}
