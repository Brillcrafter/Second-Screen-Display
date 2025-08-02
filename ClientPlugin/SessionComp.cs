using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace ClientPlugin
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
                if (Plugin.Instance.IsLoaded) SecondWindowInter.ClearDisplayListInter();
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
                var text = MessageText.Split(' ')[1];
                if (text == "open" && !Plugin.Instance.IsLoaded)
                {
                    SecondWindowThread.CreateThread();
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