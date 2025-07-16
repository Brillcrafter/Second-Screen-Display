using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace ClientPlugin;

public partial class SecondWindow
{
    
    public static List<LcdDisplay> LcdDisplays = [];
    
    private static Canvas _parentCanvas;
    
    //this stores the 
    private static Dictionary<long, TextBox> _lcdDisplaysDictionary = new();
    
    
    public SecondWindow()
    {
        InitializeComponent();
        var customFont = new FontFamily(new Uri("pack://application:,,,/SecondScreenDisplay;component/mywindow.xaml"),
            "./resources/#BigBlueTermPlus Nerd Font Mono");
        Title = "Second Screen Display";
        FontFamily = customFont;
        Width = Plugin.Instance.WindowWidth;
        Height = Plugin.Instance.WindowHeight;
        _parentCanvas = new Canvas
        {
            Width = Plugin.Instance.WindowWidth,
            Height = Plugin.Instance.WindowHeight
        };
        Content = _parentCanvas;
        FontSize = 12;
        Show();
    }

    public static void AddDisplayToList(LcdDisplay lcdDisplay)
    {
        LcdDisplays.Add(lcdDisplay);
    }

    public static void UpdateLcdList(LcdDisplay lcdDisplay)
    {
        foreach (var lcd in LcdDisplays)
        {
            if (lcd.Block.EntityId != lcdDisplay.Block.EntityId) continue;
            lcd.LcdText = lcdDisplay.LcdText;
            lcd.thisTextcolour = lcdDisplay.thisTextcolour;
            lcd.thisTextScale = lcdDisplay.thisTextScale;
            lcd.thisTextPosition = lcdDisplay.thisTextPosition;
            break;
        }
    }
    
    public static void RemoveLcdFromList(long entityId)
    {
        foreach (var lcd in LcdDisplays)
        {
            if (lcd.Block.EntityId != entityId) continue;
            LcdDisplays.Remove(lcd);
            break;
        }
    }

    public static void UpdateDisplay()
    {
        var displays = LcdDisplays;
        var displaysDictionary = _lcdDisplaysDictionary;
        //called every 10 frames, this is what will update stuff on the second window
        foreach (var lcd in LcdDisplays)
        {
            var createNew = true;
            foreach (var kv in _lcdDisplaysDictionary)
            {
                if (kv.Key == lcd.Block.EntityId)
                {
                    //then its just updating the textbox
                    //text
                    kv.Value.Text = lcd.LcdText.ToString();
                    //position
                    Vector2D canvasVector = LcdDisplay.ConvertToCanvasPos(lcd.thisTextPosition);
                    kv.Value.SetValue(Canvas.LeftProperty, canvasVector.X);
                    kv.Value.SetValue(Canvas.TopProperty, canvasVector.Y);
                    //scale
                    kv.Value.FontSize = 10;
                    //kv.Value.FontSize = lcd.thisTextScale; //probably will need to scale this to match SE
                    //colour
                    var color = new Color //why do you have to use your own colour class keeen
                    {
                        R = lcd.thisTextcolour.R,
                        G = lcd.thisTextcolour.G,
                        B = lcd.thisTextcolour.B,
                        A = lcd.thisTextcolour.A
                    };
                    kv.Value.Foreground = new SolidColorBrush(color);
                    createNew = false;
                    break;
                }
            }
            if (createNew)
            {
                //then its creating a new textbox
                var textbox = new TextBox();
                Vector2D canvasVector = LcdDisplay.ConvertToCanvasPos(lcd.thisTextPosition);
                textbox.SetValue(Canvas.LeftProperty, canvasVector.X);
                textbox.SetValue(Canvas.TopProperty, canvasVector.Y);
                textbox.FontSize = lcd.thisTextScale;
                textbox.Text = lcd.LcdText.ToString();
                textbox.Background = new SolidColorBrush(Colors.Transparent);
                var color = new Color //why do you have to use your own colour class keeen
                {
                    R = lcd.thisTextcolour.R,
                    G = lcd.thisTextcolour.G,
                    B = lcd.thisTextcolour.B,
                    A = lcd.thisTextcolour.A
                };
                textbox.Foreground = new SolidColorBrush(color);
                _lcdDisplaysDictionary.Add(lcd.Block.EntityId, textbox);
            }
        }
        
        var canvas = _parentCanvas;
        //MAKE SURE TO MAKE THIS MORE PERFORMANT!!!
        _parentCanvas.Children.Clear();
        foreach (var kv in _lcdDisplaysDictionary)
        {
            _parentCanvas.Children.Add(kv.Value);
        }
    }
    
}