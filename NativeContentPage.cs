using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FusionLedger.Windows;

public sealed class NativeContentPage : Page
{
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Content = e.Parameter as FrameworkElement;
        base.OnNavigatedTo(e);
    }
}
