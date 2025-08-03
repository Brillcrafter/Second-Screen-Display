using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace ClientPlugin
{
    public partial class SecondWindow
    {
        private static Canvas _parentCanvas;
    
        //this stores the displayed text boxes
        public static Dictionary<long, TextBox> LcdDisplaysDictionary = new Dictionary<long, TextBox>();
    
        public SecondWindow()
        {
            InitializeComponent();
            //I have to do this jank, Space.... packaging is so much more convenient.....
            var location = "file:///" + AppContext.BaseDirectory + "Plugins/Github/Brillcrafter/Second-Screen-Display/resources";
            //for pluginhub version, change to "Plugins/Github/Brillcrafter/Second-Screen-Display/resources"
            //for local testing, change to "Plugins/Local/SecondScreenDisplay/resources"
            location = location.Replace(@"\", "/");
            var customFont = new FontFamily(location+"/#BigBlueTermPlus Nerd Font Mono");
            Title = "Second Screen Display";
            FontFamily = customFont;
            int.TryParse(Config.Current.SecondWindowWidth, out var secondWindowWidth);
            int.TryParse(Config.Current.SecondWindowHeight, out var secondWindowHeight);
            Width = secondWindowWidth;
            Height = secondWindowHeight;
            _parentCanvas = new Canvas
            {
                Width = secondWindowWidth,
                Height = secondWindowHeight
            };
            Content = _parentCanvas;
            int.TryParse(Config.Current.BaseFontSize, out var baseFontSize);
            FontSize = baseFontSize;
            Show();
            Plugin.Instance.IsLoaded = true;
        }
    
        public static void AddTextBox(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            //Application.Current.Dispatcher.Invoke(() =>
            //{
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
            LcdDisplaysDictionary.Add(entityId, textbox);
            //});
        }
    
        public static void UpdateTextBox(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            //Application.Current.Dispatcher.Invoke(() =>
            //{
            foreach (var kv in LcdDisplaysDictionary)
            {
                if (kv.Key != entityId) continue;
                kv.Value.Text = text;
                kv.Value.Foreground = new SolidColorBrush(textColor);
                kv.Value.FontSize = fontsize;
                kv.Value.SetValue(Canvas.LeftProperty, position.X);
                kv.Value.SetValue(Canvas.TopProperty, position.Y);
                break;
            }
            //});
        }
    
        public static void RemoveTextBox(long entityId)
        {
            foreach (var kv in LcdDisplaysDictionary)
            {
                if (kv.Key != entityId) continue;
                LcdDisplaysDictionary.Remove(kv.Key);
                break;
            }
        }

        public static void ClearDisplayList()
        {
            LcdDisplaysDictionary.Clear();
            _parentCanvas.Children.Clear();
        }
    
        public static void UpdateOutput()
        {
            //called every 10 frames, this is what will update stuff on the second window
            //wish I could do this a smarter way, but i can't think of one
            _parentCanvas.Children.Clear();
            foreach (var kv in LcdDisplaysDictionary)
            {
                _parentCanvas.Children.Add(kv.Value);
            }
        }
    
    }
}