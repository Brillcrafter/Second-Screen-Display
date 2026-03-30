using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace BrillcrafterSSD
{
    /// <summary>
    /// Minimal Avalonia Application required to host windows outside of a
    /// normal program entry point.  Instantiated once on the dedicated UI thread
    /// inside <see cref="WindowsThreadManager"/>.
    /// </summary>
    public class SSDApplication : Application
    {
        public override void Initialize()
        {
            // FluentTheme gives us sensible default control styles on every platform.
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // We intentionally do NOT set a MainWindow here; windows are created
            // on-demand by WindowsThreadManager.CreateWindow().
            base.OnFrameworkInitializationCompleted();
        }
    }
}
