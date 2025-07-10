using System.Threading;
using System.Windows;

namespace ClientPlugin;

public class SecondWindowThread
{
    public static MyWindow WpfWindow;

    public static void CreateThread()
    {
        var wpfThread = new Thread(() =>
        {
            var app = new Application();
            WpfWindow = new MyWindow();
            app.Run(WpfWindow);
        });
        wpfThread.SetApartmentState(ApartmentState.STA); // WPF requires STA threads
        wpfThread.Start();

    }
}