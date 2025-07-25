using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace ClientPlugin;

public class SecondWindowThread
{
    public static SecondWindow WpfWindow;
    public static Thread WpfThread;

    public static void CreateThread()
    {
        if (WpfThread != null && WpfThread.IsAlive)
        {
            if (WpfWindow != null)
            {
                WpfWindow.Dispatcher.Invoke(() =>
                {
                    if (!WpfWindow.IsVisible)
                        WpfWindow.Show();
                    WpfWindow.Activate();
                });
            }

            return;
        }
        WpfThread = new Thread(() =>
        {
            WpfWindow = new SecondWindow();
            WpfWindow.Closed += (sender, args) =>
            {
                Plugin.Instance.IsLoaded = false;
                WpfWindow.Dispatcher.InvokeShutdown(); // Properly shut down the dispatcher
                WpfWindow = null;
            };

            // Run the dispatcher loop for the thread (no need for explicit Application)
            System.Windows.Threading.Dispatcher.Run();
        });
        
        WpfThread.SetApartmentState(ApartmentState.STA); // WPF requires STA threads
        WpfThread.Start();


        
        
        
        //
        // WpfThread = new Thread(() =>
        // {
        //     var app = new Application();
        //     WpfWindow = new SecondWindow();
        //     app.Run(WpfWindow);
        // });
        // WpfThread.SetApartmentState(ApartmentState.STA); // WPF requires STA threads
        // WpfThread.Start();
    }
}