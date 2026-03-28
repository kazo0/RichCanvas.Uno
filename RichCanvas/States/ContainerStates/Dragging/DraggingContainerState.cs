using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

namespace RichCanvas.States.ContainerStates
{
    /// <summary>
    /// Defines a new state used when dragging action happens on <see cref="RichCanvasContainer"/>.
    /// </summary>
    public class DraggingContainerState : ContainerState
    {
        private Point _initialPosition;
        private DraggingStrategy? _draggingStrategy;
        private DraggingStrategy DraggingStrategy => _draggingStrategy ??= Container.Host.CanSelectMultipleItems ? new MultipleDraggingStrategy(Container) : new SingleDraggingStrategy(Container);

        /// <summary>
        /// Initializes a new <see cref="DraggingContainerState"/>.
        /// </summary>
        /// <param name="container">Owner of the state.</param>
        public DraggingContainerState(RichCanvasContainer container) : base(container)
        {
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            _initialPosition = Container.Host.MousePosition;
            if (Container.IsSelectable)
            {
                if (Container.Host.CanSelectMultipleItems)
                {
                    Container.IsSelected = true;
                }
                else
                {
                    Container.Host.UpdateSingleSelectedItem(Container);
                }
            }
            Container.Host.IsDragging = true;
            DraggingStrategy.OnItemsDragStarted();
            Container.RaiseDragStartedEvent(_initialPosition);
        }

        /// <inheritdoc/>
        public override void HandlePointerMoved(PointerRoutedEventArgs e)
        {
            Point currentPosition = e.GetCurrentPoint(Container.Host.ItemsHost).Position;
            double offsetX = currentPosition.X - _initialPosition.X;
            double offsetY = currentPosition.Y - _initialPosition.Y;
            if (offsetX != 0 || offsetY != 0)
            {
                var offsetPoint = new Point(offsetX, offsetY);
                DraggingStrategy.OnItemsDragDelta(offsetPoint);
                Container.RaiseDragDeltaEvent(offsetPoint);

                _initialPosition = currentPosition;
            }
        }

        /// <inheritdoc/>
        public override void HandlePointerReleased(PointerRoutedEventArgs e)
        {
            DraggingStrategy.OnItemsDragCompleted();
            Container.Host.IsDragging = false;
            Container.RaiseDragCompletedEvent(e.GetCurrentPoint(Container.Host.ItemsHost).Position);
        }
    }
}
