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

    public string thisTextcolour;

    public bool thisTextFontShadow;
    
    public StringBuilder LcdText;
    
    //and there probably will be a reference to the a canvas object here
    
    public LcdDisplay(IMyTextPanel block)
    {
        Block = block;
    }
}