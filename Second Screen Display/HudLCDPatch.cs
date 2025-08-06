using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;

namespace ClientPlugin
{
    public class HudLcdPatch
    {
        private const string HudLcdId = "911144486";
    
        public static HudLcdPatch Instance { get; set; }

        // Defaults &  Config Format
        private const string ConfigTag = "hudlcd";
        private const char ConfigDelim = ':';
        private const double TextPosXDefault = -0.98;
        private const double TextPosYDefault = -0.2;
        private const double TextScaleDefault = 0.8;
    
        //defaults and Config Format for Sprite Display
        private const string SpriteTag = "spritelcd";
        private const char SpriteDelim = ':';
        private const double SpritePosXDefault = -0.98;
        private const double SpritePosYDefault = -0.2;
        private const double SpriteScaleDefault = 0.8;
        
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
            if (tComponent == null)
            {
                MyLog.Default.Error("HudLcdPatch: HudLcd class not found");
                return;
            }
            var hudLcdPreFix = new HarmonyMethod(typeof(HudLcdPatch), nameof(UpdateValues));
            var m = tComponent.GetMethod("UpdateValues", BindingFlags.Instance | BindingFlags.NonPublic);
            if (m != null)
            {
                Plugin.HarmonyPatcher.Patch(m, hudLcdPreFix);
            }
            else
            {
                MyLog.Default.Error("HudLcdPatch: UpdateValues method not found");
                return;
            }
            var hudLcdOnClosePrefix = new HarmonyMethod(typeof(HudLcdPatch), nameof(OnHudLcdClose));
            var method = tComponent.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                Plugin.HarmonyPatcher.Patch(method, hudLcdOnClosePrefix);
            }
            else
            {
                MyLog.Default.Error("HudLcdPatch: Close method not found");
                return;
            }
            Plugin.Instance.InitHudLcdPatch = true;
        }

        public static bool OnHudLcdClose(ref IMyTextPanel ___thisTextPanel)
        {
            //this is called when the hudlcd is closed, so we need to remove it from the list
            var entityId = ___thisTextPanel.EntityId;
            if (!Plugin.Instance.WindowOpen) return true;
            SecondWindowInter.RemoveTextBoxInter(entityId);
            return true; //still run the origional method
        }

        public static bool UpdateValues(ref bool __result, ref IMyTextPanel ___thisTextPanel)
        {
            string config;
            var runOriginal = true;
            if (___thisTextPanel.GetPublicTitle() != null &&
                ___thisTextPanel.GetPublicTitle().ToLower().Contains(ConfigTag))
            {
                config = ___thisTextPanel.GetPublicTitle();
                runOriginal = TextCdParser(___thisTextPanel, config);
            }

            if (___thisTextPanel.CustomData != null && ___thisTextPanel.CustomData.ToLower().Contains(ConfigTag))
            {
                config = ___thisTextPanel.CustomData;
                runOriginal = TextCdParser(___thisTextPanel, config);
            }

            if (___thisTextPanel.CustomData != null && ___thisTextPanel.CustomData.ToLower().Contains(SpriteTag))
            {
                runOriginal = SpriteCdParser(___thisTextPanel);
            }
            if (runOriginal)
            {
                return true;
            }
            __result = false;
            return false;
            // no hudlcd config found.
        }

        private static bool TextCdParser(IMyTextPanel block, string config)
        {
            var entityId = block.EntityId;
            var configPos = new Vector2D();
            double textScale = 1;
            var fontColour = Color.Black;
        
            //currentLcd.thisTextScale = ___thisTextPanel.FontSize;
            // Get config from config string
            var lines = config.Split('\n');
            foreach (var line in lines)
            {
                if (line.ToLower().Contains(ConfigTag))
                {
                    var rawconf =
                        line.Substring(line.IndexOf(ConfigTag))
                            .Split(ConfigDelim); // remove everything before hudlcd in the string.
                    for (int i = 0; i < 6; i++)
                    {
                        if (rawconf.Length > i && rawconf[i].Trim() != "") // Set values from Config Line
                        {
                            switch (i)
                            {
                                case 0:
                                    break;
                                case 1:
                                    configPos.X = Trygetdouble(rawconf[i], TextPosXDefault);
                                    break;
                                case 2:
                                    configPos.Y = Trygetdouble(rawconf[i], TextPosYDefault);
                                    break;
                                case 3:
                                    textScale = Trygetdouble(rawconf[i], TextScaleDefault);
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
                                    configPos.X = TextPosXDefault;
                                    break;
                                case 2:
                                    configPos.Y = TextPosYDefault;
                                    break;
                                case 3:
                                    textScale = block.FontSize;
                                    break;
                                case 4:
                                    var fontColourtemp = block.GetValueColor("FontColor");
                                    fontColour = fontColourtemp;
                                    break;
                            }
                        }
                    }
                    break; // stop processing lines from Custom Data
                }
            }
            int.TryParse(Config.Current.SecondWindowWidth, out var secondWindowWidth);
            int.TryParse(Config.Current.SecondWindowHeight, out var secondWindowHeight);
            configPos = new Vector2D((configPos.X + 1)/2 * secondWindowWidth, 
                (1 - (configPos.Y + 1)/2) * secondWindowHeight);
            int.TryParse(Config.Current.BaseFontSize, out var baseFontSize);
            textScale = baseFontSize * textScale;
            var colour = new System.Windows.Media.Color //why do you have to use your own colour class keeen
            {
                R = fontColour.R,
                G = fontColour.G,
                B = fontColour.B,
                A = fontColour.A
            };
            var currentLcdText = new StringBuilder();
            block.ReadText(currentLcdText, true);
            var currentLcdTextString = currentLcdText.ToString();
            if (Plugin.Instance.WindowOpen )
            {
                if (Plugin.IsControlled)
                {
                    SecondWindowInter.AddTextBoxInter(entityId, textScale, colour, currentLcdTextString, configPos);
                }
            }
            else
            {
                return true; //run the original method if the window isn't made
            }
            return false;
        }

        private static bool SpriteCdParser(IMyTextPanel block)
        {
            var config = block.CustomData;
            (Vector2D, double) positionData = (new Vector2D(), 0);
            var lines = config.Split('\n');
            foreach (var line in lines)
            {
                if (line.ToLower().Contains(SpriteTag))
                {
                    var rawconf =
                        line.Substring(line.IndexOf(SpriteTag))
                            .Split(SpriteDelim); // remove everything before hudlcd in the string.
                    for (var i = 0; i < 6; i++)
                    {
                        if (rawconf.Length > i && rawconf[i].Trim() != "") // Set values from Config Line
                        {
                            switch (i)
                            {
                                case 0:
                                    break;
                                case 1:
                                    positionData.Item1.X = Trygetdouble(rawconf[i], SpritePosXDefault);
                                    break;
                                case 2:
                                    positionData.Item1.Y = Trygetdouble(rawconf[i], SpritePosYDefault);
                                    break;
                                case 3:
                                     positionData.Item2 = Trygetdouble(rawconf[i], SpriteScaleDefault);
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
                                    positionData.Item1.X = SpritePosXDefault;
                                    break;
                                case 2:
                                    positionData.Item1.Y = SpritePosYDefault;
                                    break;
                                case 3:
                                    positionData.Item2 = SpriteScaleDefault;
                                    break;
                            }
                        }
                    }
                    break; // stop processing lines from Custom Data
                }
            }
            SecondWindowInter.AddSpritePositionInter(block.EntityId, positionData);
            return false;
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