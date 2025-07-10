using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientPlugin;


public class MyWindow : Window
{
    private static FontFamily CustomFont { get; set; }
    public MyWindow()
    {
        var customFont = new FontFamily(new Uri("pack://application:,,,/"), "./resources/#BigBlueTermPlus Nerd Font Mono");
        
        //Title = "My Window";
        //Width = 800;
        //Height = 600;
        Content = new TextBlock
        {
            //Text = "Hello World!",
        };
        FontSize = 12;
        Show();
    }
}