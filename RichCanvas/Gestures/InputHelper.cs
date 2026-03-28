using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Helper for checking keyboard state in WinUI/Uno.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces static Keyboard.IsKeyDown() and Keyboard.Modifiers from WPF.
    /// Uses InputKeyboardSource.GetKeyStateForCurrentThread in WinUI.
    /// </remarks>
    public static class InputHelper
    {
        /// <summary>
        /// Checks whether the specified key is currently pressed.
        /// </summary>
        public static bool IsKeyDown(VirtualKey key)
        {
            var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
            return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        /// <summary>
        /// Gets the current keyboard modifier state.
        /// </summary>
        public static VirtualKeyModifiers GetCurrentModifiers()
        {
            var modifiers = VirtualKeyModifiers.None;
            if (IsKeyDown(VirtualKey.Control))
                modifiers |= VirtualKeyModifiers.Control;
            if (IsKeyDown(VirtualKey.Shift))
                modifiers |= VirtualKeyModifiers.Shift;
            if (IsKeyDown(VirtualKey.Menu))
                modifiers |= VirtualKeyModifiers.Menu;
            return modifiers;
        }
    }
}
