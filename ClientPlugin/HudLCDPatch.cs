using System;
using System.Reflection;
using HarmonyLib;
using Sandbox.Game.World;

namespace ClientPlugin;

public class HudLcdPatch
{
    private const string HudLcdId = "911144486";
    
    public static HudLcdPatch Instance { get; set; }

    static HudLcdPatch()
    {
        Instance = new HudLcdPatch();
    }
    
    public void Start()
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
            var m = tComponent.GetMethod("UpdateValues");
            if (m != null)
            {
                Plugin.HarmonyPatcher.Patch(m, hudLcdPreFix);
                Plugin.Instance.InitPatch = true;
            }
        }
    }

    public static bool UpdateValues(ref bool __result)
    {
        //essentially turning off hudlcd
        __result = false;
        return false;
    }
    
}