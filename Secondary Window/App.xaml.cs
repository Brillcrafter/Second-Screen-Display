using System.Windows;

namespace Secondary_Window;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var secondaryWindow = new SecondaryWindow();
        secondaryWindow.Show();
    }
}