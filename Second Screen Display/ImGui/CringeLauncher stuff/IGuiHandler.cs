namespace ClientPlugin.ImGui
{
    public interface IGuiHandler
    {
        /// <summary>
        /// Whether or not the Gui Handler is blocking keys from the game's input system.
        /// </summary>
        bool BlockKeys { get; }

        /// <summary>
        /// Whether or not the Gui Handler is blocking mouse input (excluding position) from the game's input system.
        /// </summary>
        bool BlockMouse { get; }

        /// <summary>
        /// Whether or not the Gui Handler is drawing the mouse cursor via keybinds (does not include when the game is drawing the mouse cursor).
        /// </summary>
        bool DrawMouse { get; }

        /// <summary>
        /// Whether or not the Gui Handler is initialized.
        /// </summary>
        bool Initialized { get; }
    }
}
