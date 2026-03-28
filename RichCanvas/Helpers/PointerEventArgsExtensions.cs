using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace RichCanvas.Helpers
{
    /// <summary>
    /// Extension methods for pointer event args.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces MouseEventArgsExtensions which used WPF's MouseButtonState.
    /// Now uses WinUI's PointerPointProperties to check button state.
    /// </remarks>
    internal static class PointerEventArgsExtensions
    {
        internal static bool HasAnyButtonPressed(this PointerRoutedEventArgs e)
        {
            PointerPointProperties props = e.GetCurrentPoint(null).Properties;
            return props.IsLeftButtonPressed || props.IsRightButtonPressed || props.IsMiddleButtonPressed;
        }

        internal static bool HasAllButtonsReleased(this PointerRoutedEventArgs e)
        {
            PointerPointProperties props = e.GetCurrentPoint(null).Properties;
            return !props.IsLeftButtonPressed && !props.IsRightButtonPressed && !props.IsMiddleButtonPressed;
        }
    }
}
