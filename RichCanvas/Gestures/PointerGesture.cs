using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using Windows.System;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Matches a pointer button press, optionally combined with keyboard modifiers.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces System.Windows.Input.MouseGesture.
    /// </remarks>
    public class PointerGesture : InputGesture
    {
        /// <summary>
        /// The pointer action to match.
        /// </summary>
        public PointerAction PointerAction { get; set; }

        /// <summary>
        /// Optional keyboard modifiers that must be held.
        /// </summary>
        public VirtualKeyModifiers Modifiers { get; set; }

        /// <summary>
        /// Creates a new <see cref="PointerGesture"/>.
        /// </summary>
        public PointerGesture(PointerAction action, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None)
        {
            PointerAction = action;
            Modifiers = modifiers;
        }

        /// <inheritdoc/>
        public override bool Matches(object sender, PointerRoutedEventArgs? e)
        {
            if (e == null) return false;

            if (Modifiers != VirtualKeyModifiers.None && e.KeyModifiers != Modifiers)
                return false;

            PointerPoint pp = e.GetCurrentPoint(null);
            return PointerAction switch
            {
                PointerAction.LeftClick => pp.Properties.IsLeftButtonPressed,
                PointerAction.RightClick => pp.Properties.IsRightButtonPressed,
                PointerAction.MiddleClick => pp.Properties.IsMiddleButtonPressed,
                _ => false
            };
        }
    }
}
