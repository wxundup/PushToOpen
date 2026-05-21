using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PushToOpen.ViewModels;

namespace PushToOpen.Views;

public sealed partial class MainShellView : UserControl
{
    public MainShellView()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }

    public MainViewModel ViewModel { get; }

    internal Grid TitleBarRegion => AppTitleBar;
}
