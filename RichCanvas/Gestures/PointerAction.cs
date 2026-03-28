namespace RichCanvas.Gestures
{
    /// <summary>
    /// Defines pointer (mouse) actions for gesture matching.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces System.Windows.Input.MouseAction enum.
    /// </remarks>
    public enum PointerAction
    {
        /// <summary>No action.</summary>
        None,

        /// <summary>A left button click.</summary>
        LeftClick,

        /// <summary>A right button click.</summary>
        RightClick,

        /// <summary>A middle button click.</summary>
        MiddleClick
    }
}
