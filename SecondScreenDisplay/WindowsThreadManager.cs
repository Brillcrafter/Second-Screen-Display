using System.Collections.Generic;
using System.Threading;

namespace BrillcrafterSSD
{
    public static class WindowsThreadManager
    {
        public static Dictionary<int,SecondWindow> WpfWindows = new Dictionary<int, SecondWindow>();
        private static Dictionary<int,Thread> _wpfThreads = new Dictionary<int, Thread>();

        public static void CreateThread(int id)
        {
            if (_wpfThreads.ContainsKey(id)) return;
            _wpfThreads.Add(id,  new Thread(() =>
            {
                WpfWindows.Add(id, new SecondWindow(id));
                WpfWindows[id].Closed += (sender, args) =>
                {
                    WpfWindows[id].Dispatcher.Invoke(SecondWindow.ClearDisplayList);
                    WpfWindows[id].Dispatcher.InvokeShutdown(); // Properly shut down the dispatcher
                    WpfWindows = null;
                };

                // Run the dispatcher loop for the thread (no need for explicit Application)
                System.Windows.Threading.Dispatcher.Run();
            }));
        
            _wpfThreads[id].SetApartmentState(ApartmentState.STA); // WPF requires STA threads
            _wpfThreads[id].Start();
        }
    }
}