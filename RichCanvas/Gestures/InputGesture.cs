using Microsoft.UI.Xaml.Input;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Abstract base class for input gesture matching.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces System.Windows.Input.InputGesture which is not available in WinUI/Uno.
    /// WinUI does not have a built-in gesture matching system; this custom abstraction provides equivalent functionality
    /// using pointer events and keyboard state.
    /// </remarks>
    public abstract class InputGesture
    {
        /// <summary>
        /// Determines whether this gesture matches the given pointer event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The pointer event args, or null for keyboard-only gestures.</param>
        /// <returns>True if the gesture matches.</returns>
        public abstract bool Matches(object sender, PointerRoutedEventArgs? e);

        /// <summary>
        /// Determines whether this gesture matches the given keyboard event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key event args.</param>
        /// <returns>True if the gesture matches.</returns>
        public virtual bool Matches(object sender, KeyRoutedEventArgs e) => false;
    }
}
