using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace ClientPlugin
{
    public partial class SecondWindow
    {
        private static Canvas _parentCanvas;
    
        //this stores the displayed text boxes
        public static Dictionary<long, TextBox> LcdDisplaysDictionary = new Dictionary<long, TextBox>();
        
        //sprites to display, Viewport offset, surface size (total drawable area)
        public static Dictionary<long, (List<MySprite>, Vector2, Vector2) > SpriteDictionary = new Dictionary<long, (List<MySprite>, Vector2, Vector2)>();
    
        //sprites location and scale on the window
        public static Dictionary<long, (Vector2D, double)> SpriteOutputDictionary = new Dictionary<long, (Vector2D, double)>();

        private static FontFamily _SeFont;
        
        public SecondWindow()
        {
            InitializeComponent();
            //I have to do this jank, Space.... packaging is so much more convenient.....
            var location = "file:///" + AppContext.BaseDirectory + "Plugins/Local/SecondScreenDisplay/resources";
            //for pluginhub version, change to "Plugins/Github/Brillcrafter/Second-Screen-Display/resources"
            //for local testing, change to "Plugins/Local/SecondScreenDisplay/resources"
            location = location.Replace(@"\", "/");
            var customFont = new FontFamily(location+"/#BigBlueTermPlus Nerd Font Mono");
            _SeFont = new FontFamily(location+"/#Space Engineers");
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
            Plugin.Instance.WindowOpen = true;
        }

        public static void AddSpriteLcd(long entityId, (List<MySprite>, Vector2, Vector2) spriteList)
        {
            var tempList = new List<MySprite>();
            foreach (var sprite in spriteList.Item1)
            {
                var replaceData = sprite.Data;
                if (sprite.Type == SpriteType.TEXTURE)
                {//to remove my placeholder chars
                    replaceData = sprite.Data.Replace("/", ";");
                    replaceData = replaceData.Replace(@"\", "#");
                }

                var tempSprite = new MySprite
                {
                    Type = sprite.Type,
                    Data = replaceData,
                    Position = sprite.Position,
                    Size = sprite.Size,
                    Color = sprite.Color,
                    Alignment = sprite.Alignment,
                    RotationOrScale = sprite.RotationOrScale,
                    FontId = sprite.FontId,
                };
                tempList.Add(tempSprite);
            }
            SpriteDictionary[entityId] = (tempList, spriteList.Item2, spriteList.Item3);
        }

        public static void AddSpritePosition(long entityId, (Vector2D, double) posScale)
        {
            SpriteOutputDictionary[entityId] = posScale;
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
            LcdDisplaysDictionary[entityId] = textbox;
        }
    
        /*public static void UpdateTextBox(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
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
        }*/
    
        public static void RemoveTextBox(long entityId)
        {
            LcdDisplaysDictionary.Remove(entityId);
        }

        public static void ClearDisplayList()
        {
            LcdDisplaysDictionary.Clear();
            _parentCanvas.Children.Clear();
            SpriteDictionary.Clear();
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
			//now I need to render the sprites
            foreach (var kv in SpriteDictionary)
            {
                var posData = SpriteOutputDictionary[kv.Key];
                var displayData = kv.Value;
                //how would I go about doing this.....

                foreach (var sprite in displayData.Item1)
                {
                    switch (sprite.Type)
                    {
                        case SpriteType.TEXT:
                        {
                            //then I can create a textbox
                            var textbox = new TextBox();
                            textbox.Text = sprite.Data;
                            textbox.FontFamily = _SeFont;
                            if (sprite.Color.HasValue)
                            {
                                var colour = new Color //why do you have to use your own colour class keeen
                                {
                                    R = sprite.Color.Value.R,
                                    G = sprite.Color.Value.G,
                                    B = sprite.Color.Value.B,
                                    A = sprite.Color.Value.A
                                };
                                textbox.Foreground = new SolidColorBrush(colour);
                            }
                            
                            break;
                        }
                        case SpriteType.TEXTURE:
                        {
                            //I need to load the correct image
                            
                            break;
                        }
                        case SpriteType.CLIP_RECT:
                        {
                            //I need to figure this one out
                            break;
                        }
                    }
                }
                
                
            }
			
				
        }
    
    }
}