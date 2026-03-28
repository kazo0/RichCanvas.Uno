using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Matches a keyboard key press with optional modifiers.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces System.Windows.Input.KeyGesture for key-only gestures (e.g., Ctrl+Plus for ZoomIn).
    /// </remarks>
    public class KeyOnlyGesture : InputGesture
    {
        /// <summary>
        /// The virtual key to match.
        /// </summary>
        public VirtualKey Key { get; set; }

        /// <summary>
        /// The required modifiers.
        /// </summary>
        public VirtualKeyModifiers Modifiers { get; set; }

        /// <summary>
        /// Creates a new <see cref="KeyOnlyGesture"/>.
        /// </summary>
        public KeyOnlyGesture(VirtualKey key, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None)
        {
            Key = key;
            Modifiers = modifiers;
        }

        /// <inheritdoc/>
        public override bool Matches(object sender, PointerRoutedEventArgs? e) => false;

        /// <inheritdoc/>
        public override bool Matches(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Key) return false;

            // Check modifiers via InputKeyboardSource
            var currentModifiers = VirtualKeyModifiers.None;
            if (InputHelper.IsKeyDown(VirtualKey.Control))
                currentModifiers |= VirtualKeyModifiers.Control;
            if (InputHelper.IsKeyDown(VirtualKey.Shift))
                currentModifiers |= VirtualKeyModifiers.Shift;
            if (InputHelper.IsKeyDown(VirtualKey.Menu))
                currentModifiers |= VirtualKeyModifiers.Menu;

            return currentModifiers == Modifiers;
        }
    }
}
