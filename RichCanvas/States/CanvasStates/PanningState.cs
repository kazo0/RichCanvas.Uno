using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace RichCanvas.States
{
    /// <summary>
    /// Defines a new state used when panning action happens on <see cref="RichCanvas"/>.
    /// </summary>
    public class PanningState : CanvasState
    {
        private Point _initialPosition;

        /// <summary>
        /// Initializes a new <see cref="PanningState"/>.
        /// </summary>
        /// <param name="parent">Owner of the state.</param>
        public PanningState(RichCanvas parent) : base(parent)
        {
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            _initialPosition = Parent.LastPointerPosition;
            Parent.IsPanning = true;
            // [WPF Migration] WPF used Cursors.Hand on the FrameworkElement.
            // In WinUI, ProtectedCursor is protected. We set the cursor on the control itself.
            SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }

        /// <inheritdoc/>
        public override void HandlePointerMoved(PointerRoutedEventArgs e)
        {
            if (Parent.IsPanning)
            {
                Point currentPosition = e.GetCurrentPoint(Parent).Position;
                double deltaX = currentPosition.X - _initialPosition.X;
                double deltaY = currentPosition.Y - _initialPosition.Y;

                double locX = Parent.ViewportLocation.X - deltaX / Parent.ViewportZoom;
                double locY = Parent.ViewportLocation.Y - deltaY / Parent.ViewportZoom;
                Parent.ViewportLocation = new Point(locX, locY);

                _initialPosition = currentPosition;
            }
        }

        /// <inheritdoc/>
        public override void Exit()
        {
            Parent.IsPanning = false;
            SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        }

        private void SetCursor(Microsoft.UI.Input.InputSystemCursorShape shape)
        {
            // [WPF Migration] WPF used FrameworkElement.Cursor = Cursors.Hand/Arrow.
            // WinUI ProtectedCursor is protected. We use reflection to set it on the Parent control.
            try
            {
                var cursor = Microsoft.UI.Input.InputSystemCursor.Create(shape);
                var prop = typeof(UIElement).GetProperty("ProtectedCursor",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                prop?.SetValue(Parent, cursor);
            }
            catch
            {
                // Silently fail if cursor cannot be set on this platform
            }
        }
    }
}
