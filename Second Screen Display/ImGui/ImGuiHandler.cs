using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Sandbox.Graphics.GUI;
using SharpDX.Direct3D11;
using static ImGuiNET.ImGui;
using VRage;



namespace ClientPlugin.ImGui;

public class ImGuiHandler : IDisposable
{
    public static ImGuiHandler Instance;
    private DeviceContext _deviceContext;
    private static bool _init;
    
    public bool BlockMouse { get; private set; }
    public bool MouseToggle { get; set; }
    public bool MouseKey { get; set; }
    public bool DrawMouse { get; private set; }
    public bool BlockKeys => _blockKeysCounter > 0;
    private int _blockKeysCounter;

    public bool WindowHooked;
    
    public static RenderTargetView Rtv;

    
    private static readonly WndProcDelegate _wndProc = WndProcHook;
    private static readonly IntPtr _wndProcPtr =
        Marshal.GetFunctionPointerForDelegate(_wndProc);
    private static IntPtr _originalWndProc;



    


    public unsafe void Init(nint windowHandle, Device1 device, DeviceContext deviceContext)
    {
        _deviceContext = deviceContext;
        CreateContext();
        
        // var io = GetIO();
        //
        // var path = Path.Combine(_configDir.FullName, "imgui.ini");
        //
        // io.NativePtr->IniFilename = Utf8StringMarshaller.ConvertToUnmanaged(path);
        //
        // io.ConfigWindowsMoveFromTitleBarOnly = true;
        // io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;
        
        ImGui_ImplWin32_Init(windowHandle);
        ImGui_ImplDX11_Init(device.NativePointer, deviceContext.NativePointer);
        _init = true;
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

        //call my update output method

        Render();

        _deviceContext!.ClearState();
        _deviceContext.OutputMerger.SetRenderTargets(Rtv);

        ImGui_ImplDX11_RenderDrawData(GetDrawData());

        UpdatePlatformWindows();
        RenderPlatformWindowsDefault();
    }
    
    
    
    
    
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int WndProcDelegate(HWND hWnd, int msg, IntPtr wParam, IntPtr lParam);
    
    internal static void HookWindow(HWND windowHandle)
    {
        _originalWndProc = PInvoke.GetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC);

        PInvoke.SetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, _wndProcPtr);
        
        Instance.WindowHooked = true;
    }
    
    private static unsafe int WndProcHook(HWND hWnd, int msg, IntPtr wParam, IntPtr lParam)
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
            return CallWindowProc(_wndProcPtr, hWnd, msg, wParam, lParam);
        
        var hookResult = ImGui_ImplWin32_WndProcHandler(hWnd,msg, wParam, lParam);

        if (hookResult != 0)
            return hookResult;

        if (!_init)
            return CallWindowProc(_wndProcPtr, hWnd, msg, wParam, lParam);

        var io = GetIO();

        var blockMessage = (msg is >= 256 and <= 265 && io.WantTextInput)
                           || (msg is >= 512 and <= 526 && io.WantCaptureMouse);

        return blockMessage ? hookResult : CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam
        );
    }

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW", ExactSpelling = true)]
    private static extern int CallWindowProc(
        IntPtr lpPrevWndFunc,
        HWND   hWnd,
        int    msg,
        IntPtr wParam,
        IntPtr lParam);


    public void Dispose()
    {
        _deviceContext?.Dispose();
    }
}