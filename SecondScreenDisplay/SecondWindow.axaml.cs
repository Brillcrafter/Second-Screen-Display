using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using VRageMath;

namespace BrillcrafterSSD
{
    /// <summary>
    /// The floating overlay window shown on a second monitor.
    /// Replaces the original WPF Window and its auto-generated _g.cs counterpart.
    ///
    /// All public methods MUST be called from the Avalonia UI thread.
    /// <see cref="WindowThreadsInter"/> handles the dispatch.
    /// </summary>
    public partial class SecondWindow : Avalonia.Controls.Window
    {
        // ------------------------------------------------------------------
        // Fields
        // ------------------------------------------------------------------

        /// <summary>Keyed by SE entity ID → the TextBlock rendering that LCD.</summary>
        public Dictionary<long, TextBlock> LcdDisplaysDictionary { get; } = new();

        private Canvas _canvas = null!;

        // Font loaded from the embedded Nerd Font .ttf
        private static readonly FontFamily NerdFont =
            new FontFamily("avares://SecondScreenDisplay/resources#BigBlueTerm Plus Nerd Font Mono");

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public SecondWindow(int id)
        {
            // Let Avalonia's XAML loader wire up the tree defined in SecondWindow.axaml
            AvaloniaXamlLoader.Load(this);

            _canvas = this.FindControl<Canvas>("DisplayCanvas")
                      ?? throw new System.Exception("[SSD] DisplayCanvas not found in SecondWindow.axaml");

            // Apply user config values
            Width  = Plugin.Instance.RealWindowWidth;
            Height = Plugin.Instance.RealWindowHeight;

            var bg = Config.Current.SecondWindowBackgroundColor;
            Background = new SolidColorBrush(
                Avalonia.Media.Color.FromArgb(bg.A, bg.R, bg.G, bg.B));

            Title = $"Second Screen Display [{id}]";
        }

        // ------------------------------------------------------------------
        // LCD management – called by WindowThreadsInter on the UI thread
        // ------------------------------------------------------------------

        /// <summary>Create a new TextBlock for the given LCD entity.</summary>
        public void AddTextBox(long entityId, double fontSize,
                               Avalonia.Media.Color textColor, string text,
                               Vector2D position)
        {
            var tb = MakeTextBlock(fontSize, textColor, text);
            Canvas.SetLeft(tb, position.X);
            Canvas.SetTop(tb,  position.Y);
            _canvas.Children.Add(tb);
            LcdDisplaysDictionary[entityId] = tb;
        }

        /// <summary>Update an existing TextBlock in-place.</summary>
        public void UpdateTextBox(long entityId, double fontSize,
                                  Avalonia.Media.Color textColor, string text,
                                  Vector2D position)
        {
            if (!LcdDisplaysDictionary.TryGetValue(entityId, out var tb)) return;

            tb.Text       = text;
            tb.FontSize   = fontSize;
            tb.Foreground = new SolidColorBrush(textColor);
            Canvas.SetLeft(tb, position.X);
            Canvas.SetTop(tb,  position.Y);
        }

        /// <summary>Remove the TextBlock for one entity.</summary>
        public void RemoveTextBox(long entityId)
        {
            if (!LcdDisplaysDictionary.TryGetValue(entityId, out var tb)) return;
            _canvas.Children.Remove(tb);
            LcdDisplaysDictionary.Remove(entityId);
        }

        /// <summary>Remove all TextBlocks (e.g. player leaves a cockpit).</summary>
        public void ClearDisplayList()
        {
            _canvas.Children.Clear();
            LcdDisplaysDictionary.Clear();
        }

        /// <summary>
        /// Called every few game ticks.  Avalonia redraws reactively so there is
        /// nothing to do here, but the hook is kept for future use.
        /// </summary>
        public void UpdateOutput() { /* no-op: Avalonia redraws on property change */ }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static TextBlock MakeTextBlock(double fontSize,
                                               Avalonia.Media.Color color,
                                               string text)
        {
            return new TextBlock
            {
                Text       = text,
                FontSize   = fontSize,
                FontFamily = NerdFont,
                Foreground = new SolidColorBrush(color),
                // Preserve newlines from LCD text panels
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            };
        }
    }
}
