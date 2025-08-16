using System;
using Windows.Win32.Foundation;
using HarmonyLib;
using VRage.Platform.Windows.Forms;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Platform.Windows.Render;
using VRage.Render11.Resources;
using VRageRender;

namespace ClientPlugin.ImGui;

[HarmonyPatch]
public static class RenderHookPatch
{
    [HarmonyPrefix, HarmonyPatch(typeof(SwapChain), nameof(SwapChain.Present))]
    private static void PresentPrefix(MyGameForm __instance)
    {
        if (!ImGuiHandler.Instance.WindowHooked) 
            ImGuiHandler.HookWindow((HWND)__instance.Handle);
        ImGuiHandler.Instance?.DoRender();
    }
}
[HarmonyPatch]
public static class SwapChainPatch
{
    internal static nint WindowHandle;

    [HarmonyPrefix, HarmonyPatch(typeof(MyPlatformRender), nameof(MyPlatformRender.CreateSwapChain))]
    private static bool SwapChainPrefix(nint windowHandle)
    {
        WindowHandle = windowHandle;
        MyPlatformRender.DisposeSwapChain();
        MyPlatformRender.Log.WriteLine("CreateDeviceInternal create swapchain");

        if (MyPlatformRender.m_swapchain != null)
            return false;

        var chainDescription = new SwapChainDescription
        {
            BufferCount = 2,
            Flags = SwapChainFlags.AllowModeSwitch,
            IsWindowed = true,
            ModeDescription = MyPlatformRender.GetCurrentModeDescriptor(MyPlatformRender.m_settings) with
            {
                Format = Format.R8G8B8A8_UNorm
            },
            SampleDescription = {
                Count = 1,
                Quality = 0
            },
            OutputHandle = windowHandle,
            Usage = Usage.ShaderInput | Usage.RenderTargetOutput,
            SwapEffect = SwapEffect.Discard
        };

        var factory = MyPlatformRender.GetFactory();
        try
        {
            MyPlatformRender.m_swapchain = new SwapChain(factory, MyPlatformRender.DeviceInstance, chainDescription);
        }
        catch (Exception)
        {
            MyPlatformRender.Log.WriteLine("SwapChain factory = " + factory);
            MyPlatformRender.Log.WriteLine("SwapChain Device = " + MyPlatformRender.DeviceInstance);
            MyPlatformRender.PrintSwapChainDescriptionToLog(chainDescription);
            throw;
        }
        factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAll);

        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MyRender11), nameof(MyRender11.CreateDeviceInternal))]
    private static void CreateDevicePostfix()
    {
        ImGuiHandler.Instance?.Init(WindowHandle, MyRender11.DeviceInstance, MyRender11.RC.DeviceContext);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyBackbuffer), MethodType.Constructor, typeof(SharpDX.Direct3D11.Resource))]
    private static bool SwapChainBBPrefix(MyBackbuffer __instance, SharpDX.Direct3D11.Resource swapChainBB)
    {
        __instance.m_resource = swapChainBB;
        __instance.m_rtv = new RenderTargetView(MyRender11.DeviceInstance, swapChainBB, new()
        {
            Format = Format.R8G8B8A8_UNorm_SRgb,
            Dimension = RenderTargetViewDimension.Texture2D,
        });
        __instance.m_srv = new ShaderResourceView(MyRender11.DeviceInstance, swapChainBB);

        ImGuiHandler.Rtv = new RenderTargetView(MyRender11.DeviceInstance, swapChainBB, new()
        {
            Format = Format.R8G8B8A8_UNorm,
            Dimension = RenderTargetViewDimension.Texture2D,
        });

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MyBackbuffer), nameof(MyBackbuffer.Release))]
    private static void SwapChainBBReleasePrefix(MyBackbuffer __instance)
    {
        if (ImGuiHandler.Rtv is null) return;

        ImGuiHandler.Rtv.Dispose();
        ImGuiHandler.Rtv = null;
    }
}