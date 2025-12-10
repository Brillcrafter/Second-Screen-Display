using System.Windows.Threading;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace BrillcrafterSSD
{
    public class WindowThreadsInter
    {
        //this is to make sure that it dosen't access a closed window
        public static void AddTextBoxInter(int screenId ,long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            if (WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window))
            {
                window.Dispatcher.BeginInvoke(() =>
                {
                    SecondWindow.AddTextBox(entityId, fontsize, textColor, text, position);
                });
            }
        
        }

        public static void UpdateTextBoxInter(int screenId ,long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            if (WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window))
            {
                window.Dispatcher.BeginInvoke(() =>
                {
                    SecondWindow.UpdateTextBox(entityId, fontsize, textColor, text, position);
                });
            }
        }

        public static void RemoveTextBoxInter(int screenId, long entityId)
        {
            if (WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window))
            {
                window.Dispatcher.BeginInvoke(() => { SecondWindow.RemoveTextBox(entityId); });
            }
        }

        public static void ClearDisplayListInter()
        {
            foreach (var kv in WindowsThreadManager.WpfWindows)
            {
                kv.Value.Dispatcher.BeginInvoke(SecondWindow.ClearDisplayList);
            }
        }

        public static void UpdateDisplayInter()
        {
            foreach (var kv in WindowsThreadManager.WpfWindows)
            {
                kv.Value.Dispatcher.BeginInvoke(SecondWindow.UpdateOutput);
            }
        }
    }
}
