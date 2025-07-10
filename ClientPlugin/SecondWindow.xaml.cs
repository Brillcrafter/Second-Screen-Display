using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientPlugin;


public partial class SecondWindow
{
    private static FontFamily CustomFont { get; set; }
    public SecondWindow()
    {
        InitializeComponent();
        var customFont = new FontFamily(new Uri("pack://application:,,,/SecondScreenDisplay;component/mywindow.xaml"),
            "./resources/#BigBlueTermPlus Nerd Font Mono");
        Title = "My Window";
        FontFamily = customFont;
        Width = 800;
        Height = 600;
        Content = new Canvas();
        FontSize = 12;
        Show();
    }
    
}