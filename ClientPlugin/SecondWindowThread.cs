using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace ClientPlugin;

public class SecondWindowThread
{
    public static SecondWindow WpfWindow;

    public static void CreateThread()
    {
        var wpfThread = new Thread(() =>
        {
            var app = new Application();
            WpfWindow = new SecondWindow();
            app.Run(WpfWindow);
        });
        wpfThread.SetApartmentState(ApartmentState.STA); // WPF requires STA threads
        wpfThread.Start();
    }
}