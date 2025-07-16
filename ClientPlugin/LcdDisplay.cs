using System.Text;
using Sandbox.ModAPI;
using VRageMath;

namespace ClientPlugin;

public class LcdDisplay
{
    //this represents one LCD, that is being displayed on the second window

    public IMyTextPanel Block;
    
    public double thisTextScale = 0.8;

    public Vector2D thisTextPosition;

    public Color thisTextcolour;

    public bool thisTextFontShadow;
    
    public StringBuilder LcdText;
    
    //and there probably will be a reference to the a canvas object here
    
    public LcdDisplay(IMyTextPanel block)
    {
        Block = block;
    }

    public static Vector2D ConvertToCanvasPos(Vector2D position)
    {
        var width = Plugin.Instance.WindowWidth;
        var height = Plugin.Instance.WindowHeight;
        var x = (position.X + 1)/2 * Plugin.Instance.WindowWidth;
        var y = (1 - (position.Y + 1)/2) * Plugin.Instance.WindowHeight;
        //now 0,0 is bottom left instead of center
        return new Vector2D(x, y);
    }
}