using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace BrillcrafterSSD
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SessionComp : MySessionComponentBase
    {
        public static SessionComp Instance;
        private bool _chatCommandsInit;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;
        }
    
        public override void BeforeStart()
        {
            if (!Plugin.Instance.InitPatch){HudLcdPatch.Start();}
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= HandleCommand;
                Instance._chatCommandsInit = false;
                Plugin.Instance.InitPatch = false;
                if (Plugin.Instance.IsLoaded) WindowThreadsInter.ClearDisplayListInter();
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
    
        private static void HandleCommand(ulong Sender, string MessageText, ref bool sendToOthers)
        {
            if (MessageText.ToLower().StartsWith("/ssd"))
            {
                sendToOthers = false;
                var messageSplit = MessageText.Split(' ');
                var layer1 = messageSplit[1];
                
                if (layer1 == "open" && !Plugin.Instance.IsLoaded)
                {
                    if (messageSplit.Length > 2)
                    {
                        var layer2 = messageSplit[2];
                        WindowsThreadManager.CreateThread(Convert.ToInt32(layer2));
                    }
                    else
                    {
                        WindowsThreadManager.CreateThread(0);
                    }
                    //SecondWindowThread.CreateThread();
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!Instance._chatCommandsInit)
            {
                Instance._chatCommandsInit = true;  
                MyAPIGateway.Utilities.MessageEnteredSender -= HandleCommand;
                MyAPIGateway.Utilities.MessageEnteredSender += HandleCommand;
            }
        }
    }
}