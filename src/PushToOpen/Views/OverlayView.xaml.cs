using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PushToOpen.ViewModels;

namespace PushToOpen.Views;

public sealed partial class OverlayView : UserControl
{
    public OverlayView()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OverlayViewModel>();
    }

    public OverlayViewModel ViewModel { get; }

    internal Grid Root => OverlayRoot;
}
