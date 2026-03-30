using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using VRage.Utils;

namespace BrillcrafterSSD
{
    /// <summary>
    /// Manages the lifetime of <see cref="SecondWindow"/> instances and the
    /// single Avalonia UI thread they all share.
    ///
    /// WPF original: each window lived on its own STA thread with its own
    ///               Dispatcher.Run() loop.
    /// Avalonia port: one dedicated background thread runs Avalonia's event
    ///               loop; all windows are created/destroyed on that thread
    ///               via <c>Dispatcher.UIThread.InvokeAsync</c>.
    /// </summary>
    public static class WindowsThreadManager
    {
        // Publicly readable so HudLCDPatch can check LcdDisplaysDictionary.
        public static Dictionary<int, SecondWindow> WpfWindows { get; } = new();

        private static Thread?  _avaloniaThread;
        private static bool     _avaloniaStarted;
        private static readonly object _startLock = new();

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Open (or bring to front) the overlay window with the given numeric ID.
        /// Safe to call from any thread.
        /// </summary>
        public static void CreateThread(int id)
        {
            EnsureAvaloniaStarted();

            // Guard against double-creation – must be checked from the UI thread
            // so we dispatch the whole thing there.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (WpfWindows.ContainsKey(id))
                {
                    // Window already open; just bring it to the foreground.
                    WpfWindows[id].Activate();
                    return;
                }

                var window = new SecondWindow(id);

                window.Closed += (_, _) =>
                {
                    window.ClearDisplayList();
                    WpfWindows.Remove(id);
                };

                WpfWindows[id] = window;
                window.Show();
            });
        }

        // ------------------------------------------------------------------
        // Avalonia bootstrap
        // ------------------------------------------------------------------

        /// <summary>
        /// Starts the Avalonia UI thread exactly once.  Blocks the calling
        /// thread until Avalonia's infrastructure is fully initialised so that
        /// subsequent <c>Dispatcher.UIThread.InvokeAsync</c> calls are safe.
        /// </summary>
        private static void EnsureAvaloniaStarted()
        {
            lock (_startLock)
            {
                if (_avaloniaStarted) return;
                _avaloniaStarted = true;

                // Signal set once SetupWithoutStarting() returns on the new thread.
                using var ready = new ManualResetEventSlim(false);

                _avaloniaThread = new Thread(() =>
                {
                    try
                    {
                        // Configure and initialise Avalonia on *this* thread.
                        // SetupWithoutStarting() designates the calling thread as
                        // Dispatcher.UIThread without running a blocking event loop,
                        // so we can signal readiness before entering MainLoop.
                        AppBuilder
                            .Configure<SSDApplication>()
                            .UsePlatformDetect()   // X11 / Wayland on Linux, Win32 on Windows
                            .LogToTrace()
                            .SetupWithoutStarting();

                        ready.Set();               // unblock the game thread

                        // Run the event loop indefinitely (background thread, so it
                        // exits automatically when the process shuts down).
                        Dispatcher.UIThread.MainLoop(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        MyLog.Default.Error($"[SSD] Avalonia UI thread crashed: {ex}");
                        ready.Set(); // don't leave the game thread hanging
                    }
                });

                _avaloniaThread.IsBackground = true;
                _avaloniaThread.Name = "SSD-Avalonia-UI";
                _avaloniaThread.Start();

                // Wait until Avalonia is ready before returning to the game thread.
                ready.Wait();
            }
        }
    }
}
