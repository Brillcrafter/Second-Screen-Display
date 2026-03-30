using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;
using VRageColor = VRageMath.Color;
using AvaloniaColor = Avalonia.Media.Color;

// NOTE: System.Windows.Controls is gone – no WPF dependency here any more.

namespace BrillcrafterSSD
{
    public class HudLcdPatch
    {
        private const string HudLcdId = "911144486";

        public static HudLcdPatch Instance { get; set; } = null!;

        private Dictionary<long, int> _displayedwindow = new();

        // ── Config defaults ───────────────────────────────────────────
        const string configTag             = "hudlcd";
        const char   configDelim           = ':';
        const double textPosXDefault       = -0.98;
        const double textPosYDefault       = -0.2;
        const double textScaleDefault      = 0.8;
        const string textFontDefault       = "white";
        const bool   textFontShadowDefault = false;
        const string removeFromHudDefault  = "remove";
        const int    screenIdDefault       = 0;

        double thisTextScale = textScaleDefault;

        // Config format:
        // hudlcd:{PosX}:{PosY}:{FontSize}:{Colour}:{Shadow}:{Remove}:{ScreenID}
        // remove  = ignore | duplicate | (default = remove from HUD)
        // screenID = 0-indexed window id

        static HudLcdPatch()
        {
            Instance = new HudLcdPatch();
        }

        public static void Start()
        {
            foreach (var kv in MyScriptManager.Static.Scripts)
            {
                if (kv.Key.String.Contains(HudLcdId))
                    Patch(kv.Value);
            }
        }

        private static void Patch(Assembly hudLcd)
        {
            var tComponent = hudLcd.GetType("Jawastew.HudLcd.HudLcd", false);
            if (tComponent == null) return;

            var updateMethod = tComponent.GetMethod("UpdateValues",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (updateMethod != null)
                Plugin.HarmonyPatcher.Patch(updateMethod,
                    new HarmonyMethod(typeof(HudLcdPatch), nameof(UpdateValues)));
            else
                MyLog.Default.Error("HudLcdPatch: UpdateValues method not found");

            var closeMethod = tComponent.GetMethod("Close",
                BindingFlags.Instance | BindingFlags.Public);
            if (closeMethod != null)
            {
                Plugin.HarmonyPatcher.Patch(closeMethod,
                    new HarmonyMethod(typeof(HudLcdPatch), nameof(OnHudLcdClose)));
                Plugin.Instance.InitPatch = true;
            }
            else
                MyLog.Default.Error("HudLcdPatch: Close method not found");
        }

        public static bool OnHudLcdClose(ref IMyTextPanel ___thisTextPanel)
        {
            var entityId = ___thisTextPanel.EntityId;
            if (Instance._displayedwindow.TryGetValue(entityId, out var windowId))
            {
                WindowThreadsInter.RemoveTextBoxInter(windowId, entityId);
                Instance._displayedwindow.Remove(entityId);
            }
            return true;
        }

        public static bool UpdateValues(ref bool __result, ref IMyTextPanel ___thisTextPanel)
        {
            string? config;
            if (___thisTextPanel.GetPublicTitle() != null &&
                ___thisTextPanel.GetPublicTitle().ToLower().Contains(configTag))
            {
                config = ___thisTextPanel.GetPublicTitle();
            }
            else if (___thisTextPanel.CustomData != null &&
                     ___thisTextPanel.CustomData.ToLower().Contains(configTag))
            {
                config = ___thisTextPanel.CustomData;
            }
            else
            {
                return false;
            }

            var entityId     = ___thisTextPanel.EntityId;
            var configPos    = new Vector2D();
            double textScale = 1;
            var fontColour   = VRageColor.Black;
            var screenId     = screenIdDefault;
            var removeOption = removeFromHudDefault;

            config = config.ToLower();
            var lines = config.Split('\n');
            foreach (var line in lines)
            {
                if (!line.ToLower().Contains(configTag)) continue;

                var rawConf = line.Substring(line.IndexOf(configTag)).Split(configDelim);

                for (var i = 0; i < 8; i++)
                {
                    if (rawConf.Length > i && rawConf[i].Trim() != "")
                    {
                        switch (i)
                        {
                            case 1: configPos.X  = Trygetdouble(rawConf[i], textPosXDefault); break;
                            case 2: configPos.Y  = Trygetdouble(rawConf[i], textPosYDefault); break;
                            case 3: textScale    = Trygetdouble(rawConf[i], textScaleDefault); break;
                            case 6: removeOption = rawConf[i]; break;
                            case 7: int.TryParse(rawConf[i], out screenId); break;
                        }
                    }
                    else
                    {
                        switch (i)
                        {
                            case 1: configPos.X  = textPosXDefault; break;
                            case 2: configPos.Y  = textPosYDefault; break;
                            case 3: textScale    = ___thisTextPanel.FontSize; break;
                            case 4: fontColour   = ___thisTextPanel.GetValueColor("FontColor"); break;
                            case 7: screenId     = screenIdDefault; break;
                        }
                    }
                }
                break;
            }

            if (removeOption == "ignore")
                return true;

            configPos = new Vector2D(
                (configPos.X + 1) / 2 * Plugin.Instance.RealWindowWidth,
                (1 - (configPos.Y + 1) / 2) * Plugin.Instance.RealWindowHeight);

            int.TryParse(Config.Current.BaseFontSize, out var baseFontSize);
            textScale = baseFontSize * textScale;

            // ── Color conversion: VRageMath → Avalonia.Media ──────────
            // Previously this was System.Windows.Media.Color; same struct layout,
            // just a different namespace.
            var colour = AvaloniaColor.FromArgb(
                fontColour.A, fontColour.R, fontColour.G, fontColour.B);

            var sb = new StringBuilder();
            ___thisTextPanel.ReadText(sb, true);
            var lcdText = sb.ToString();

            if (WindowsThreadManager.WpfWindows.TryGetValue(screenId, out var window))
            {
                if (!window.LcdDisplaysDictionary.ContainsKey(entityId))
                {
                    Instance._displayedwindow.Add(entityId, screenId);
                    WindowThreadsInter.AddTextBoxInter(screenId, entityId, textScale, colour, lcdText, configPos);
                }
                WindowThreadsInter.UpdateTextBoxInter(screenId, entityId, textScale, colour, lcdText, configPos);
            }

            switch (removeOption)
            {
                case "duplicate": return true;
                default:
                    __result = false;
                    return false;
            }
        }

        private static double Trygetdouble(string v, double defaultVal)
        {
            try   { return double.Parse(v); }
            catch { return defaultVal; }
        }
    }
}
