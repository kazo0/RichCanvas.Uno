using Windows.System;

using RichCanvas.States;
using RichCanvas.States.ContainerStates;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Holds all default <see cref="InputGesture"/>s used to match their associated state.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] WPF's System.Windows.Input.InputGesture, MouseGesture, and KeyGesture
    /// are not available in WinUI/Uno. Replaced with a custom InputGesture abstraction
    /// that uses pointer events and virtual key state checking.
    /// </remarks>
    public class RichCanvasGestures
    {
        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to match both <see cref="SingleSelectionState"/> or <see cref="MultipleSelectionState"/>.
        /// </summary>
        public static InputGesture Select { get; set; } = new PointerGesture(PointerAction.LeftClick);

        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to match the <see cref="DrawingState"/>.
        /// </summary>
        public static InputGesture Drawing { get; set; } = new PointerGesture(PointerAction.LeftClick);

        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to match the <see cref="DraggingContainerState"/>.
        /// </summary>
        public static InputGesture Drag { get; set; } = new PointerGesture(PointerAction.LeftClick);

        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to invoke ZoomIn command.
        /// </summary>
        /// <remarks>
        /// VirtualKey 187 = OemPlus ('+' / '=' key). Cast from int because the enum name varies across platforms.
        /// </remarks>
        public static InputGesture ZoomIn { get; set; } = new KeyOnlyGesture((VirtualKey)187, VirtualKeyModifiers.Control);

        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to invoke ZoomOut command.
        /// </summary>
        /// <remarks>
        /// VirtualKey 189 = OemMinus ('-' key). Cast from int because the enum name varies across platforms.
        /// </remarks>
        public static InputGesture ZoomOut { get; set; } = new KeyOnlyGesture((VirtualKey)189, VirtualKeyModifiers.Control);

        /// <summary>
        /// Gets or sets the <see cref="InputGesture"/> used to match the <see cref="PanningState"/>.
        /// </summary>
        public static InputGesture Pan { get; set; } = new PointerKeyGesture(new PointerGesture(PointerAction.LeftClick), VirtualKey.Space);

        /// <summary>
        /// Gets or sets the <see cref="VirtualKeyModifiers"/> used together with PointerWheel for zooming.
        /// <br/>
        /// Default is <see cref="VirtualKeyModifiers.Control"/>.
        /// </summary>
        public static VirtualKeyModifiers ZoomModifierKey { get; set; } = VirtualKeyModifiers.Control;
    }
}
