using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace ClientPlugin
{
    public class SecondWindowThread
    {
        public static SecondWindow WpfWindow;
        private static Thread _wpfThread;

        public static void CreateThread()
        {
            if (Plugin.Instance.WindowOpen) return;
            if (_wpfThread != null && _wpfThread.IsAlive)
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
            _wpfThread = new Thread(() =>
            {
                WpfWindow = new SecondWindow();
                WpfWindow.Closed += (sender, args) =>
                {
                    WpfWindow.Dispatcher.Invoke(SecondWindow.ClearDisplayList);
                    Plugin.Instance.WindowOpen = false;
                    WpfWindow.Dispatcher.InvokeShutdown(); // Properly shut down the dispatcher
                    WpfWindow = null;
                };

                // Run the dispatcher loop for the thread (no need for explicit Application)
                System.Windows.Threading.Dispatcher.Run();
            });
        
            _wpfThread.SetApartmentState(ApartmentState.STA); // WPF requires STA threads
            _wpfThread.Start();
        }
    }
}