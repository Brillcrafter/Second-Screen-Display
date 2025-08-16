using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Input;
using VRage.Input.Keyboard;
using VRage.Platform.Windows.Input;
using VRageMath;

namespace ClientPlugin.ImGui;

[HarmonyPatch]
internal static class InputPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(MyDirectInput), nameof(MyDirectInput.GetMouseState))]
    private static void GetMouseStatePostfix(ref MyMouseState state)
    {
        if (ImGuiHandler.Instance?.BlockMouse == true && (MyVRage.Platform.Input.ShowCursor || ImGuiHandler.Instance.DrawMouse))
        {
            state.LeftButton = false;
            state.RightButton = false;
            state.MiddleButton = false;
            state.XButton1 = false;
            state.XButton2 = false;
            state.ScrollWheelValue = 0;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyVRageInput), nameof(MyVRageInput.GetMouseXForGamePlayF))]
    private static bool GetMouseXForGamePlayFPrefix(ref float __result)
    {
        if (ImGuiHandler.Instance?.DrawMouse == true)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyVRageInput), nameof(MyVRageInput.GetMouseYForGamePlayF))]
    private static bool GetMouseYForGamePlayFPrefix(ref float __result)
    {
        if (ImGuiHandler.Instance?.DrawMouse == true)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MyDX9Gui), nameof(MyDX9Gui.Draw))]
    private static List<CodeInstruction> Dx9GuiDrawTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();

        var method = AccessTools.Method(typeof(MyVideoSettingsManager), nameof(MyVideoSettingsManager.IsHardwareCursorUsed));

        var index = list.FindIndex(b => b.Calls(method));

        list[index].opcode = OpCodes.Ret;
        list[index].operand = null;

        list.RemoveRange(index + 1, list.Count - index - 1);

        return list;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyGuiLocalizedKeyboardState), nameof(MyGuiLocalizedKeyboardState.GetCurrentState))]
    private static bool GetKeyboardStatePrefix(ref MyKeyboardState __result)
    {
        if (ImGuiHandler.Instance?.BlockKeys == true)
        {
            __result = default;
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyGuiManager), nameof(MyGuiManager.MouseCursorPosition), MethodType.Getter)]
    private static bool GetMouseCursorPositionPrefix(ref Vector2 __result)
    {
        if (ImGuiHandler.Instance?.BlockMouse == true && (MyVRage.Platform.Input.ShowCursor || ImGuiHandler.Instance.DrawMouse))
        {
            __result = Vector2.Zero;
            return false;
        }
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MySession), nameof(MySession.HandleInput))]
    private static void HandleInputPostfix()
    {
        if (ImGuiHandler.Instance == null || MyInput.Static == null)
            return;

        if (MyInput.Static.IsAnyAltKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
        {
            ImGuiHandler.Instance.MouseToggle = !ImGuiHandler.Instance.MouseToggle;
        }
    }
}
