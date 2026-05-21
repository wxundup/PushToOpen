using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PushToOpen.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public HomeViewModel Home { get; }
    public AudioViewModel Audio { get; }
    public HotkeyViewModel Hotkey { get; }
    public OverlayViewModel Overlay { get; }
    public AppPreferencesViewModel Preferences { get; }

    public MainViewModel(
        HomeViewModel home,
        AudioViewModel audio,
        HotkeyViewModel hotkey,
        OverlayViewModel overlay,
        AppPreferencesViewModel preferences)
    {
        Home = home;
        Audio = audio;
        Hotkey = hotkey;
        Overlay = overlay;
        Preferences = preferences;
    }

    [ObservableProperty] private string activeSection = "home";

    [RelayCommand]
    private void Navigate(string section) => ActiveSection = section;
}
