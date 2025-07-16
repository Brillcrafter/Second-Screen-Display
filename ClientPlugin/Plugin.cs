using System;
using System.Collections.Generic;
using System.Reflection;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Plugins;
using VRage.Utils;

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin: IPlugin
{
    public const string Name = "SecondScreenDisplay";
    public static Plugin Instance { get; private set; }
    private SettingsGenerator _settingsGenerator;
    private bool _isLoaded;
    public bool InitPatch;
    public static Harmony HarmonyPatcher { get; private set; }

    public int WindowWidth = 1920;
    public int WindowHeight = 1080;
    
    bool IsControlled => MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity is IMyTerminalBlock;

    private int _counter;
    
    /*
    what do I need to do?
    MVP is displaying stuff on an LCD in another window, preserving the location used for hudlcd.
    so, get currently piloting grid, look for LCDs on it, if they exist and are configured for hudlcd.
    edit the custom data to something like PLCD, grab the location and scale data.
    then send the currently displayed stuff to the second screen for displaying.
    HUDLCD uses -1 to 1 to determine where to place the item and uses top left corner as origin
    at first it will stop hudlcd from displaying the text and place it on the window
    later add options to have it on both or only SE.
    
    */
        
        

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;
        Instance._settingsGenerator = new SettingsGenerator();

        // TODO: Put your one time initialization code here.
        HudLcdPatch.Instance = new HudLcdPatch();
        HarmonyPatcher = new Harmony(Name);
        MyLog.Default.Info("Second Screen display Init Complete");
    }

    public void Dispose()
    {
        // TODO: Save state and close resources here, called when the game exits (not guaranteed!)
        // IMPORTANT: Do NOT call harmony.UnpatchAll() here! It may break other plugins.

        Instance = null;
    }

    public void Update()
    {
        // TODO: Put your update code here. It is called on every simulation frame!
        if (!Instance._isLoaded)
        {
            SecondWindowThread.CreateThread();
            Instance._isLoaded = true;  
        }
        if (MyAPIGateway.Multiplayer == null || MySession.Static?.LocalCharacter == null || MyAPIGateway.Session == null)
        {
            return;
        }
        
        
        if (Instance._counter == 10)
        {
            Instance._counter = 0;
            if (Instance.IsControlled)
            {
                SecondWindowThread.WpfWindow.Dispatcher.Invoke(SecondWindow.UpdateDisplay);
                //send the call to update the second window output
            }
        }
        else Instance._counter++;
    }

    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        Instance._settingsGenerator.SetLayout<Simple>();
        MyGuiSandbox.AddScreen(Instance._settingsGenerator.Dialog);
    }

    //TODO: Uncomment and use this method to load asset files
    /*public void LoadAssets(string folder)
    {

    }*/
}