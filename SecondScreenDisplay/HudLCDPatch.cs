using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;

namespace BrillcrafterSSD
{
    public class HudLcdPatch
    {
        private const string HudLcdId = "911144486";
    
        public static HudLcdPatch Instance { get; set; }
        
        private Dictionary<long, int> _displayedwindow = new Dictionary<long, int>();

        // Defaults &  Config Format
        const string configTag = "hudlcd";
        const char configDelim = ':';
        const double textPosXDefault = -0.98;
        const double textPosYDefault = -0.2;
        const double textScaleDefault = 0.8;
        const string textFontDefault = "white";
        const bool textFontShadowDefault = false;
        const string removeFromHudDefault = "remove";
        const int screenIdDefault = 0;
    
        double thisTextScale = textScaleDefault;
        
        //Config Format
        // hudlcd:{PosX}:{PosY}:{Fontsize}:{Colour}:{Shadow}:{Remove}:{ScreenID}
        //remove = is the display removed, duplicated, or not touched. ignore, duplicate, none
        //screenID = which window to display it on, 0 indexed
        
        static HudLcdPatch()
        {
            Instance = new HudLcdPatch();
        }
    
        public static void Start()
        {
            foreach (var kv in MyScriptManager.Static.Scripts)
            {
                if (kv.Key.String.Contains(HudLcdId))
                {
                    Patch(kv.Value);
                }
            }
        }

        private static void Patch(Assembly hudLcd)
        {
            var tComponent = hudLcd.GetType("Jawastew.HudLcd.HudLcd", false);
            if (tComponent != null)
            {
                HarmonyMethod hudLcdPreFix = new HarmonyMethod(typeof(HudLcdPatch), nameof(UpdateValues));
                var m = tComponent.GetMethod("UpdateValues", BindingFlags.Instance | BindingFlags.NonPublic);
                if (m != null)
                {
                    Plugin.HarmonyPatcher.Patch(m, hudLcdPreFix);
                }
                else MyLog.Default.Error("HudLcdPatch: UpdateValues method not found");
                var hudLcdOnClosePrefix = new HarmonyMethod(typeof(HudLcdPatch), nameof(OnHudLcdClose));
                var method = tComponent.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    Plugin.HarmonyPatcher.Patch(method, hudLcdOnClosePrefix);
                    Plugin.Instance.InitPatch = true;
                }
                else MyLog.Default.Error("HudLcdPatch: Close method not found");
            }
        }

        public static bool OnHudLcdClose(ref IMyTextPanel ___thisTextPanel)
        {
            //this is called when the hudlcd is closed, so we need to remove it from the list
            var entityId = ___thisTextPanel.EntityId;
            if (!Plugin.Instance.IsLoaded) return true;
            WindowThreadsInter.RemoveTextBoxInter(Instance._displayedwindow[entityId],entityId);
            Instance._displayedwindow.Remove(entityId);
            return true; //still run the origional method
        }

        public static bool UpdateValues(ref bool __result, ref IMyTextPanel ___thisTextPanel)
        {
            string config;
            if (___thisTextPanel.GetPublicTitle() != null &&
                ___thisTextPanel.GetPublicTitle().ToLower().Contains(configTag))
            {
                config = ___thisTextPanel.GetPublicTitle();
            }
            else if (___thisTextPanel.CustomData != null && ___thisTextPanel.CustomData.ToLower().Contains(configTag))
            {
                config = ___thisTextPanel.CustomData;
            }
            else
            {
                // no hudlcd config found.
                return false;
            }

            var entityId = ___thisTextPanel.EntityId;
            

            var configPos = new Vector2D();
            double textScale = 1;
            var fontColour = Color.Black;
            var screenId = screenIdDefault;
            var removeOption = removeFromHudDefault;
        
            //currentLcd.thisTextScale = ___thisTextPanel.FontSize;
            // Get config from config string
            config = config.ToLower();
            var lines = config.Split('\n');
            foreach (var line in lines)
            {
                if (line.ToLower().Contains(configTag))
                {
                    var rawconf =
                        line.Substring(line.IndexOf(configTag))
                            .Split(configDelim); // remove everything before hudlcd in the string.
                    for (var i = 0; i < 8; i++)
                    {
                        if (rawconf.Length > i && rawconf[i].Trim() != "") // Set values from Config Line
                        {
                            switch (i)
                            {
                                case 0:
                                    break;
                                case 1:
                                    configPos.X = Trygetdouble(rawconf[i], textPosXDefault);
                                    break;
                                case 2:
                                    configPos.Y = Trygetdouble(rawconf[i], textPosYDefault);
                                    break;
                                case 3:
                                    textScale = Trygetdouble(rawconf[i], textScaleDefault);
                                    break;
                                case 6:
                                    //removed or not
                                    break;
                                case 7:
                                    int.TryParse(rawconf[i], out screenId);
                                    break;
                            }
                        }
                        else // Set Default Values
                        {
                            switch (i)
                            {
                                case 0:
                                    break;
                                case 1:
                                    configPos.X = textPosXDefault;
                                    break;
                                case 2:
                                    configPos.Y = textPosYDefault;
                                    break;
                                case 3:
                                    textScale = ___thisTextPanel.FontSize;
                                    break;
                                case 4:
                                    var fontColourtemp = ___thisTextPanel.GetValueColor("FontColor");
                                    fontColour = fontColourtemp;
                                    break;
                                case 6:
                                    //default remove option
                                    break;
                                case 7:
                                    screenId = screenIdDefault;
                                    break;
                            }
                        }
                    }
                    break; // stop processing lines from Custom Data
                }
            }
            if (removeOption == "ignore")
            {
                //if we are ignoring it then let original method run
                return true;
            }
            
            configPos = new Vector2D((configPos.X + 1)/2 * Plugin.Instance.RealWindowWidth, 
                (1 - (configPos.Y + 1)/2) * Plugin.Instance.RealWindowHeight);
            int.TryParse(Config.Current.BaseFontSize, out var baseFontSize);
            textScale = baseFontSize * textScale ;
            var colour = new System.Windows.Media.Color //why do you have to use your own colour class keeen
            {
                R = fontColour.R,
                G = fontColour.G,
                B = fontColour.B,
                A = fontColour.A
            };
            
            var currentLcdText = new StringBuilder();
            ___thisTextPanel.ReadText(currentLcdText, true);
            var currentLcdTextString = currentLcdText.ToString();
            
            if (WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window))
            {
                if (!window.LcdDisplaysDictionary.ContainsKey(entityId))
                {
                    Instance._displayedwindow.Add(entityId, screenId);
                    WindowThreadsInter.AddTextBoxInter(screenId, entityId, textScale, colour, currentLcdTextString, configPos);
                }
                //now to actually read the stuff from the LCD
                WindowThreadsInter.UpdateTextBoxInter(screenId, entityId, textScale, colour, currentLcdTextString, configPos);
            }

            switch (removeOption)
            {
                default:
                    __result = false;
                    return false;
                case "duplicate":
                    return true;
            }
        }
    
        private static double Trygetdouble(string v, double defaultval)
        {
            try
            {
                return double.Parse(v);
            }
            catch (Exception)
            {
                return defaultval;
            }
        }
    }
}