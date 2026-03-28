using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

using RichCanvas.Gestures;
using RichCanvas.Helpers;

namespace RichCanvas.States
{
    /// <summary>
    /// Defines a new state used when selecting multiple items action happens on <see cref="RichCanvas"/>.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Key changes:
    /// - Mouse events replaced with Pointer events.
    /// - Mouse.GetPosition replaced with PointerRoutedEventArgs.GetCurrentPoint().Position.
    /// - GeometryHitTest replaced with bounds-intersection testing via HitTestHelper.
    /// - VisualTreeHelper.HitTest with GeometryHitTestParameters not available in WinUI.
    /// </remarks>
    public class MultipleSelectionState : CanvasState
    {
        private Point _selectionRectangleInitialPosition;

        /// <summary>
        /// Initializes a new <see cref="MultipleSelectionState"/>.
        /// </summary>
        /// <param name="parent">Owner of the state.</param>
        public MultipleSelectionState(RichCanvas parent) : base(parent)
        {
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            Parent.SelectionRectangle = new Rect();
            Parent.IsSelecting = true;
            _selectionRectangleInitialPosition = Parent.MousePosition;
            Parent.UnselectAll();
        }

        /// <inheritdoc/>
        public override void ReEnter()
        {
            Parent.SelectionRectangle = SelectionHelper.DrawSelectionRectangle(Parent.MousePosition, _selectionRectangleInitialPosition);
            SelectItems();
        }

        /// <inheritdoc/>
        public override void HandleKeyDown(KeyRoutedEventArgs e)
        {
            if (RichCanvasGestures.Pan.Matches(e.OriginalSource, e))
            {
                PushState(new PanningState(Parent));
            }
        }

        /// <inheritdoc/>
        public override void HandlePointerPressed(PointerRoutedEventArgs e)
        {
            if (RichCanvasGestures.Pan.Matches(e.OriginalSource, e))
            {
                PushState(new PanningState(Parent));
            }
        }

        /// <inheritdoc/>
        public override void HandlePointerMoved(PointerRoutedEventArgs e)
        {
            if (!Parent.IsSelecting)
            {
                return;
            }

            Point position = e.GetCurrentPoint(Parent.ItemsHost).Position;
            Parent.SelectionRectangle = SelectionHelper.DrawSelectionRectangle(position, _selectionRectangleInitialPosition);

            if (Parent.RealTimeSelectionEnabled)
            {
                SelectItems();
            }
        }

        /// <inheritdoc/>
        public override void HandlePointerReleased(PointerRoutedEventArgs e)
        {
            if (!Parent.IsSelecting)
            {
                return;
            }

            Parent.IsSelecting = false;
            if (!Parent.RealTimeSelectionEnabled)
            {
                SelectItems();
            }
        }

        /// <inheritdoc/>
        public override void HandleAutoPanning(PointerRoutedEventArgs? e) => HandleAutoPanningInternal();

        private void HandleAutoPanningInternal()
        {
            if (!Parent.IsSelecting) return;
            Parent.SelectionRectangle = SelectionHelper.DrawSelectionRectangle(Parent.MousePosition, _selectionRectangleInitialPosition);
            if (Parent.RealTimeSelectionEnabled)
            {
                SelectItems();
            }
        }

        private void SelectItems()
        {
            Parent.UnselectAll();

            List<RichCanvasContainer> containers = HitTestHelper.FindContainersInArea(Parent.ItemsHost, Parent.SelectionRectangle);

            Parent.BeginSelectionTransaction();

            for (int i = 0; i < containers.Count; i++)
            {
                RichCanvasContainer container = containers[i];
                if (container.IsSelectable)
                {
                    Parent.InternalSelectedItems.Add(container.DataContext);
                }
            }

            Parent.EndSelectionTransaction();
        }
    }
}
