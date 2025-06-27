using System;
using System.Reflection;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using VRage.Plugins;

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin, IDisposable
{
    public const string Name = "SecondScreenDisplay";
    public static Plugin Instance { get; private set; }
    private SettingsGenerator _settingsGenerator;
    /*
    what do I need to do?
    MVP is displaying stuff on an LCD in another window, preserving the location used for hudlcd.
    so, get currently piloting grid, look for LCDs on it, if they exist and are configured for hudlcd.
    edit the custom data to something like PLCD, grab the location and scale data.
    then send the currently displayed stuff to the second screen for displaying.

    */
        
        

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        Instance = this;
        Instance._settingsGenerator = new SettingsGenerator();

        // TODO: Put your one time initialization code here.
        Harmony harmony = new Harmony(Name);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
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