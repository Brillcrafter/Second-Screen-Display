using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRageRender;

namespace ClientPlugin;

public class HudLcdPatch
{
    private const string HudLcdId = "911144486";
    
    public static HudLcdPatch Instance { get; set; }

    // Defaults &  Config Format
    const string configTag = "hudlcd";
    const char configDelim = ':';
    const double textPosXDefault = -0.98;
    const double textPosYDefault = -0.2;
    const double textScaleDefault = 0.8;
    const string textFontDefault = "white";
    const bool textFontShadowDefault = false;
    
    double thisTextScale = textScaleDefault;
    
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
        SecondWindowThread.WpfWindow.Dispatcher.Invoke(() =>
        {
            SecondWindow.RemoveLcdFromList(entityId);
        });
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

        LcdDisplay currentLcd = null;
        foreach (var lcd in SecondWindow.LcdDisplays)
        {
            if (lcd.Block.EntityId == ___thisTextPanel.EntityId)
            {
                currentLcd = lcd;
                break;
            }
        }

        var newLcd = false;
        if (currentLcd == null)
        {
            //create a new one
            currentLcd = new LcdDisplay(___thisTextPanel);
            newLcd = true;
        }

        currentLcd.thisTextScale = ___thisTextPanel.FontSize;
        // Get config from config string
        var lines = config.Split('\n');
        foreach (var line in lines)
        {
            if (line.ToLower().Contains(configTag))
            {
                var rawconf =
                    line.Substring(line.IndexOf(configTag))
                        .Split(configDelim); // remove everything before hudlcd in the string.
                for (int i = 0; i < 6; i++)
                {
                    if (rawconf.Length > i && rawconf[i].Trim() != "") // Set values from Config Line
                    {
                        switch (i)
                        {
                            case 0:
                                break;
                            case 1:
                                currentLcd.thisTextPosition.X = Trygetdouble(rawconf[i], textPosXDefault);
                                break;
                            case 2:
                                currentLcd.thisTextPosition.Y = Trygetdouble(rawconf[i], textPosYDefault);
                                break;
                            case 3:
                                currentLcd.thisTextScale = Trygetdouble(rawconf[i], textScaleDefault);
                                break;
                            case 4:
                                //currentLcd.thisTextcolour = "<color=" + rawconf[i].Trim() + ">";
                                //going to remove support for colour defined in the config,
                                //I don't think any script uses it
                                break;
                            case 5:
                                if (rawconf[i].Trim() == "1")
                                {
                                    currentLcd.thisTextFontShadow = true;
                                }
                                else
                                {
                                    currentLcd.thisTextFontShadow = false;
                                }
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
                                currentLcd.thisTextPosition.X = textPosXDefault;
                                break;
                            case 2:
                                currentLcd.thisTextPosition.Y = textPosYDefault;
                                break;
                            case 3:
                                currentLcd.thisTextScale = ___thisTextPanel.FontSize;
                                break;
                            case 4:
                                var fontColor = ___thisTextPanel.GetValueColor("FontColor");
                                currentLcd.thisTextcolour = fontColor;
                                break;
                            case 5:
                                currentLcd.thisTextFontShadow = false;
                                break;
                        }
                    }

                }
                break; // stop processing lines from Custom Data
            }
        }

        if (newLcd)
            SecondWindowThread.WpfWindow.Dispatcher.Invoke(() =>
            {
                SecondWindow.AddDisplayToList(currentLcd);
            });
        //now to actually read the stuff from the LCD
        UpdateHudMessage(currentLcd);
        
        __result = false; 
        return false; //stopping hudlcd from creating the hudmessage thingy
    }
    
    private static void UpdateHudMessage(LcdDisplay currentLcd)
    {
        currentLcd.LcdText ??= new StringBuilder(500);
        currentLcd.LcdText.Clear();
        currentLcd.Block.ReadText(currentLcd.LcdText, true);
        SecondWindowThread.WpfWindow.Dispatcher.Invoke(() =>
        {
            SecondWindow.UpdateLcdList(currentLcd);
        });
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