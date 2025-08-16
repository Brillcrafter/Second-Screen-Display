using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using SharpDX.Direct3D11;
using static ImGuiNET.ImGui;
using VRage;
using Sandbox.Graphics.GUI;
using ClientPlugin;

namespace ClientPlugin.ImGui;

internal sealed class ImGuiHandler : IGuiHandler, IDisposable
{
    private readonly DirectoryInfo _configDir;
    private DeviceContext? _deviceContext;
    private int _blockKeysCounter;
    private static nint _wndproc;

    public bool BlockMouse { get; private set; }
    public bool BlockKeys => _blockKeysCounter > 0;
    public bool DrawMouse { get; private set; }

    public bool MouseToggle { get; set; }
    public bool MouseKey { get; set; }

    public bool Initialized => _init;

    public static ImGuiHandler? Instance;

    public static RenderTargetView? Rtv;

    private readonly IRootRenderComponent _renderHandler;
    private readonly ImGuiImageService _imageService;
    private static bool _init;

    public ImGuiHandler(DirectoryInfo configDir)
    {
        _configDir = configDir;
        _renderHandler = new RenderHandler(this);
        _imageService = (ImGuiImageService)GameServicesExtension.GameServices.GetRequiredService<IImGuiImageService>();
    }

    public unsafe void Init(nint windowHandle, Device1 device, DeviceContext deviceContext)
    {
        _deviceContext = deviceContext;

        CreateContext();

        var io = GetIO();

        var path = Path.Join(_configDir.FullName, "imgui.ini");

        io.NativePtr->IniFilename = Utf8StringMarshaller.ConvertToUnmanaged(path);

        io.ConfigWindowsMoveFromTitleBarOnly = true;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

        ImGui_ImplWin32_Init(windowHandle);
        ImGui_ImplDX11_Init(device.NativePointer, deviceContext.NativePointer);
        _init = true;

        _imageService.Initialize();
    }

    public static void HookWindow(HWND windowHandle)
    {
        _wndproc = PInvoke.GetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC);

        unsafe
        {
            delegate* unmanaged[Stdcall]<HWND, int, nint, nint, int> wndProcHook = &WndProcHook;

            PInvoke.SetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, (nint)wndProcHook);
        }
    }

    public void DoRender()
    {
        if (Rtv is null)
            return;

        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        NewFrame();

        var io = GetIO();
        BlockMouse = io.WantCaptureMouse;

        if (io.WantTextInput)
            _blockKeysCounter = 10; //WantTextInput can be false briefly after pressing enter in a textbox
        else
            _blockKeysCounter--;

        DrawMouse = io.MouseDrawCursor || MouseToggle || MouseKey;

        var focusedScreen = MyScreenManager.GetScreenWithFocus(); //migrated logic from MyDX9Gui.Draw

        if (DrawMouse || focusedScreen?.GetDrawMouseCursor() == true || (MyScreenManager.InputToNonFocusedScreens && MyScreenManager.GetScreensCount() > 1))
        {
            MyGuiSandbox.SetMouseCursorVisibility(true, false);
        }
        else if (focusedScreen != null)
        {
            MyGuiSandbox.SetMouseCursorVisibility(focusedScreen.GetDrawMouseCursor());
        }

        _renderHandler.OnFrame();

        Render();

        _deviceContext!.ClearState();
        _deviceContext.OutputMerger.SetRenderTargets(Rtv);

        ImGui_ImplDX11_RenderDrawData(GetDrawData());

        UpdatePlatformWindows();
        RenderPlatformWindowsDefault();

        _imageService.Update();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe int WndProcHook(HWND hWnd, int msg, nint wParam, nint lParam)
    {
        //special handling for the mouse free key

        if (msg is 0x0100 or 0x104 && (int)wParam == 0xC0 && Instance != null)
        {
            Instance.MouseKey = true;

            return 0;
        }

        if (msg is 0x0101 or 0x105 && (int)wParam == 0xC0 && Instance != null)
        {
            Instance.MouseKey = false;

            return 0;
        }

        if (msg == 0x102 && (char)(int)wParam == '`')
            return 0;

        //ignore input if mouse is hidden
        if (Instance?.BlockKeys != true && MyVRage.Platform?.Input?.ShowCursor == false && Instance?.DrawMouse != true)
            return CallWindowProc(_wndproc, hWnd, msg, wParam, lParam);

        var hookResult = ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam);

        if (hookResult != 0)
            return hookResult;

        if (!_init)
            return CallWindowProc(_wndproc, hWnd, msg, wParam, lParam);

        var io = GetIO();

        var blockMessage = (msg is >= 256 and <= 265 && io.WantTextInput)
            || (msg is >= 512 and <= 526 && io.WantCaptureMouse);

        return blockMessage ? hookResult : CallWindowProc(_wndproc, hWnd, msg, wParam, lParam);
    }

    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern int CallWindowProc(nint lpPrevWndFunc, HWND hWnd, int msg, nint wParam, nint lParam);

    public void Dispose()
    {
        _deviceContext?.Dispose();
        _renderHandler.Dispose();
    }
}