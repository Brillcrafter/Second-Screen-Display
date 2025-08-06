using System.Collections.Generic;
using System.Windows.Threading;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Color = System.Windows.Media.Color;

namespace ClientPlugin
{
    public class SecondWindowInter
    {
        //this is to make sure that it dosen't access a closed window

        public static void AddSpriteLcdInter(long entityId, (List<MySprite>, Vector2, Vector2) spriteList)
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(() =>
            {
                SecondWindow.AddSpriteLcd(entityId, spriteList);
            });
        }
        
        public static void AddSpritePositionInter(long entityId, (Vector2D, double) posScale)
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(() =>
            {
                SecondWindow.AddSpritePosition(entityId, posScale);
            });
        }
        
        public static void AddTextBoxInter(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(() =>
            {
                SecondWindow.AddTextBox(entityId, fontsize, textColor, text, position);
            });
        
        }

        /*public static void UpdateTextBoxInter(long entityId, double fontsize, Color textColor, string text, Vector2D position)
        {
            if (!Plugin.Instance.IsLoaded) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(() =>
            {
                SecondWindow.UpdateTextBox(entityId, fontsize, textColor, text, position);
            });
        }*/

        public static void RemoveTextBoxInter(long entityId)
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(() =>
            {
                SecondWindow.RemoveTextBox(entityId);
            });
        }

        public static void ClearDisplayListInter()
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(SecondWindow.ClearDisplayList);
        }

        public static void UpdateDisplayInter()
        {
            if (!Plugin.Instance.WindowOpen) return;
            SecondWindowThread.WpfWindow.Dispatcher.BeginInvoke(SecondWindow.UpdateOutput);
        }
    }
}
