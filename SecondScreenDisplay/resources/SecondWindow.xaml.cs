using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace BrillcrafterSSD
{
    public partial class SecondWindow
    {
        private Canvas _parentCanvas;
    
        //this stores the displayed text boxes
        public Dictionary<long, TextBox> LcdDisplaysDictionary = new Dictionary<long, TextBox>();

        private Color _colorCache;

        private int _screenId;
        
        public static SecondWindow Instance { get; set; }
    
        public SecondWindow(int screenId)
        {
            Instance = this;
            InitializeComponent();
            Instance._screenId = screenId;
            //I have to do this jank, Space.... packaging is so much more convenient.....
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            assemblyLocation = assemblyLocation.Remove(assemblyLocation.LastIndexOf(@"\", StringComparison.Ordinal));
            assemblyLocation += @"\Legacy\GitHub\Brillcrafter\Second-Screen-Display";
            var location = "file:///" + assemblyLocation + "/Assets";
            //for local testing, change to "file:///" + "C:/Users/Bredn/RiderProjects/Second-Screen-Display/Second Screen Display/resources";
            location = location.Replace(@"\", "/");
            var customFont = new FontFamily(location+"/#BigBlueTermPlus Nerd Font Mono");
            Title = "Second Screen Display";
            FontFamily = customFont;
            int.TryParse(Config.Current.SecondWindowWidth, out var secondWindowWidth);
            int.TryParse(Config.Current.SecondWindowHeight, out var secondWindowHeight);
            Width = secondWindowWidth;
            Height = secondWindowHeight;
            var colour = new Color()
            {
                R=Config.Current.SecondWindowBackgroundColor.R,
                G=Config.Current.SecondWindowBackgroundColor.G,
                B=Config.Current.SecondWindowBackgroundColor.B,
            };
            Background = new SolidColorBrush(colour);
            
            _parentCanvas = new Canvas
            {
                Width = secondWindowWidth,
                Height = secondWindowHeight
            };
            Content = _parentCanvas;
            int.TryParse(Config.Current.BaseFontSize, out var baseFontSize);
            FontSize = baseFontSize;
            SizeChanged += WindowSizeChanged; 
            Show();
        }

        private static void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Instance._parentCanvas.Width = e.NewSize.Width;
            Instance._parentCanvas.Height = e.NewSize.Height;
            Plugin.Instance.RealWindowHeight = (int)e.NewSize.Height;
            Plugin.Instance.RealWindowWidth = (int)e.NewSize.Width;
        }
    
        public static void AddTextBox(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            var textbox = new TextBox
            {
                Text = text,
                FontSize = fontsize,
                Foreground = new SolidColorBrush(textColor),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            textbox.SetValue(Canvas.LeftProperty, position.X);
            textbox.SetValue(Canvas.TopProperty, position.Y);
            Instance.LcdDisplaysDictionary.Add(entityId, textbox);
        }
    
        public static void UpdateTextBox(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            foreach (var kv in Instance.LcdDisplaysDictionary)
            {
                if (kv.Key != entityId) continue;
                kv.Value.Text = text;
                kv.Value.Foreground = new SolidColorBrush(textColor);
                kv.Value.FontSize = fontsize;
                kv.Value.SetValue(Canvas.LeftProperty, position.X);
                kv.Value.SetValue(Canvas.TopProperty, position.Y);
                break;
            }
        }
    
        public static void RemoveTextBox(long entityId)
        {
            foreach (var kv in Instance.LcdDisplaysDictionary)
            {
                if (kv.Key != entityId) continue;
                Instance.LcdDisplaysDictionary.Remove(kv.Key);
                break;
            }
        }

        public static void ClearDisplayList()
        {
            Instance.LcdDisplaysDictionary.Clear();
            Instance._parentCanvas.Children.Clear();
        }
    
        public static void UpdateOutput()
        {
            //called every 10 frames, this is what will update stuff on the second window
            //wish I could do this a smarter way, but i can't think of one
            Instance._parentCanvas.Children.Clear();
            foreach (var kv in Instance.LcdDisplaysDictionary)
            {
                Instance._parentCanvas.Children.Add(kv.Value);
            }

            if (Instance._colorCache.R == Config.Current.SecondWindowBackgroundColor.R &&
                Instance._colorCache.G == Config.Current.SecondWindowBackgroundColor.G &&
                Instance._colorCache.B == Config.Current.SecondWindowBackgroundColor.B)
                return;

            Instance._colorCache = new Color()
            {
                R = Config.Current.SecondWindowBackgroundColor.R,
                G = Config.Current.SecondWindowBackgroundColor.G,
                B = Config.Current.SecondWindowBackgroundColor.B,
            };
            WindowsThreadManager.WpfWindows[Instance._screenId].Background = new SolidColorBrush(Instance._colorCache);
        }
    
    }
}