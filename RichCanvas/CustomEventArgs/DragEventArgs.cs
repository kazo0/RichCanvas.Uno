using System;
using Windows.Foundation;

namespace RichCanvas.CustomEventArgs
{
    /// <summary>
    /// Event args for when a drag operation starts on a <see cref="RichCanvasContainer"/>.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] In WPF, DragStartedEventArgs was a RoutedEventArgs from Primitives.
    /// In WinUI, DragStartedEventArgs exists on Thumb but we use our own for the container's drag events
    /// since these are now CLR events rather than routed events.
    /// </remarks>
    public class ContainerDragStartedEventArgs : EventArgs
    {
        /// <summary>The horizontal position at drag start.</summary>
        public double HorizontalOffset { get; }

        /// <summary>The vertical position at drag start.</summary>
        public double VerticalOffset { get; }

        /// <summary>Creates a new instance.</summary>
        public ContainerDragStartedEventArgs(double horizontalOffset, double verticalOffset)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
        }
    }

    /// <summary>
    /// Event args for when a drag operation progresses on a <see cref="RichCanvasContainer"/>.
    /// </summary>
    public class ContainerDragDeltaEventArgs : EventArgs
    {
        /// <summary>The horizontal delta of the drag.</summary>
        public double HorizontalChange { get; }

        /// <summary>The vertical delta of the drag.</summary>
        public double VerticalChange { get; }

        /// <summary>Creates a new instance.</summary>
        public ContainerDragDeltaEventArgs(double horizontalChange, double verticalChange)
        {
            HorizontalChange = horizontalChange;
            VerticalChange = verticalChange;
        }
    }

    /// <summary>
    /// Event args for when a drag operation completes on a <see cref="RichCanvasContainer"/>.
    /// </summary>
    public class ContainerDragCompletedEventArgs : EventArgs
    {
        /// <summary>The horizontal position at drag end.</summary>
        public double HorizontalOffset { get; }

        /// <summary>The vertical position at drag end.</summary>
        public double VerticalOffset { get; }

        /// <summary>Creates a new instance.</summary>
        public ContainerDragCompletedEventArgs(double horizontalOffset, double verticalOffset)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
        }
    }
}
