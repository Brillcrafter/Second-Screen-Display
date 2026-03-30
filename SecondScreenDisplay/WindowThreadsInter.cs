using Avalonia.Threading;
using VRageMath;
using AvaloniaColor = Avalonia.Media.Color;

namespace BrillcrafterSSD
{
    /// <summary>
    /// Thread-safe bridge between the SE game loop (any thread) and the Avalonia
    /// UI thread.
    ///
    /// WPF original: used window.Dispatcher.BeginInvoke(...) and static methods
    ///               on SecondWindow (relying on [ThreadStatic] per-STA-thread).
    /// Avalonia port: all windows share one UI thread, so we use
    ///               Dispatcher.UIThread.InvokeAsync and call instance methods
    ///               directly on the captured window reference.
    /// </summary>
    public static class WindowThreadsInter
    {
        public static void AddTextBoxInter(int screenId, long entityId,
                                           double fontSize, AvaloniaColor textColor,
                                           string text, Vector2D position)
        {
            if (!WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window)) return;

            Dispatcher.UIThread.InvokeAsync(() =>
                window.AddTextBox(entityId, fontSize, textColor, text, position));
        }

        public static void UpdateTextBoxInter(int screenId, long entityId,
                                              double fontSize, AvaloniaColor textColor,
                                              string text, Vector2D position)
        {
            if (!WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window)) return;

            Dispatcher.UIThread.InvokeAsync(() =>
                window.UpdateTextBox(entityId, fontSize, textColor, text, position));
        }

        public static void RemoveTextBoxInter(int screenId, long entityId)
        {
            if (!WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window)) return;

            Dispatcher.UIThread.InvokeAsync(() => window.RemoveTextBox(entityId));
        }

        public static void ClearDisplayListInter()
        {
            // Snapshot the values so the foreach isn't affected by concurrent modifications.
            foreach (var window in WindowsThreadManager.WpfWindows.Values)
            {
                var captured = window;
                Dispatcher.UIThread.InvokeAsync(() => captured.ClearDisplayList());
            }
        }

        public static void UpdateDisplayInter()
        {
            foreach (var window in WindowsThreadManager.WpfWindows.Values)
            {
                var captured = window;
                Dispatcher.UIThread.InvokeAsync(() => captured.UpdateOutput());
            }
        }
    }
}
