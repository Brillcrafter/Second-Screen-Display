using HarmonyLib;
using SpaceEngineers.Game;

namespace ClientPlugin.ImGui;

[HarmonyPatch(typeof(SpaceEngineersGame), "InitializeRender")]
public static class RenderInitPatch
{
    private static bool Prefix() => false;
}