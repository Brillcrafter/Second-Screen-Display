using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;

namespace ClientPlugin
{
    public class SpritePatch
    {
        public static void Start()
        {
            MyLog.Default.WriteLine("SpritePatch started");
            var assembly = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => 
                a.GetName().Name == "Sandbox.Game");
            if (assembly == null)
            {
                MyLog.Default.WriteLine("SpritePatch failed to find Sandbox.Game assembly");
                return;
            }
            var type = assembly.GetType("Sandbox.Game.Entities.Blocks.MyTextPanelComponent");
            if (type == null)
            {
                MyLog.Default.WriteLine("SpritePatch failed to find Sandbox.Game.Entities.Blocks.MyTextPanelComponent type");
                return;
            }
            var method = type.GetMethod("UpdateSpritesTexture");
            if (method == null)
            {
                MyLog.Default.WriteLine("SpritePatch failed to find Sandbox.Game.Entities.Blocks.MyTextPanelComponent.UpdateSpritesTexture method");
                return;
            }
            // create harmony patch here
            var postfix = typeof(SpritePatch).GetMethod(nameof(UpdateSpritesTexturePostFix));
            Plugin.HarmonyPatcher.Patch(method, postfix: new HarmonyMethod(postfix));
            MyLog.Default.WriteLine("SpritePatch finished");
            Plugin.Instance.InitSpritePatch = true;
        }

        private static void UpdateSpritesTexturePostFix(MyTextPanelComponent __instance, bool __result, List<MySprite> ___m_renderLayers, MyTerminalBlock ___m_block)
        {
            if (!__result || !Plugin.IsControlled) return;
            SecondWindowInter.AddSpriteLcdInter(___m_block.EntityId, (___m_renderLayers, 
                __instance.TextureSize - __instance.SurfaceSize / 2f, __instance.SurfaceSize));
        }
    }
}