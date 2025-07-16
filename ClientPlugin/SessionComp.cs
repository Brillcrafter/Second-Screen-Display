using System;
using VRage.Game.Components;
using VRage.Utils;

namespace ClientPlugin;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class SessionComp : MySessionComponentBase
{
    public static SessionComp Instance;
    
    public override void BeforeStart()
    {
        if (!Plugin.Instance.InitPatch){HudLcdPatch.Start();}
    }

    protected override void UnloadData()
    {
        try
        {
            Plugin.Instance.InitPatch = false;
        }
        catch (Exception e)
        {
            MyLog.Default.Error(e.ToString());

        }
        finally
        {
            Instance = null;
        }
    }
}