using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

using RichCanvas.Gestures;
using RichCanvas.Helpers;

namespace RichCanvas.States
{
    /// <summary>
    /// Defines a new state used when single selection action happens on <see cref="RichCanvas"/>.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] GeometryHitTest replaced with bounds-intersection testing via HitTestHelper.
    /// </remarks>
    public class SingleSelectionState : CanvasState
    {
        private Point _selectionRectangleInitialPosition;
        private RichCanvasContainer? _selectedContainer;
        private List<RichCanvasContainer> _selectedContainers = new List<RichCanvasContainer>();

        /// <summary>
        /// Initializes a new <see cref="SingleSelectionState"/>.
        /// </summary>
        /// <param name="parent">Owner of the state.</param>
        public SingleSelectionState(RichCanvas parent) : base(parent)
        {
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            Parent.SelectionRectangle = new Rect();
            Parent.IsSelecting = true;
            _selectionRectangleInitialPosition = Parent.MousePosition;
            Parent.SelectedItem = null;
        }

        /// <inheritdoc/>
        public override void ReEnter()
        {
            SelectItem(Parent.RealTimeSelectionEnabled);
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
                SelectItem();
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
                SelectItem(true);
            }
        }

        /// <inheritdoc/>
        public override void HandleAutoPanning(PointerRoutedEventArgs? e)
        {
            if (!Parent.IsSelecting) return;
            Parent.SelectionRectangle = SelectionHelper.DrawSelectionRectangle(Parent.MousePosition, _selectionRectangleInitialPosition);
            if (Parent.RealTimeSelectionEnabled)
            {
                SelectItem();
            }
        }

        private void SelectItem(bool defferedSelection = false)
        {
            if (!defferedSelection)
            {
                _selectedContainers.Clear();
            }

            List<RichCanvasContainer> containers = HitTestHelper.FindContainersInArea(Parent.ItemsHost, Parent.SelectionRectangle);

            for (int i = 0; i < containers.Count; i++)
            {
                RichCanvasContainer container = containers[i];
                if (container.IsSelectable)
                {
                    _selectedContainers.Add(container);
                }
            }

            if (!defferedSelection)
            {
                if (Parent.SelectedItem == null && _selectedContainers.Count > 0)
                {
                    UpdateSelectedItem();
                }
                if ((_selectedContainers.Count > 0 && !_selectedContainers.Contains(_selectedContainer!)) || _selectedContainers.Count == 0)
                {
                    Parent.SelectedItem = null;
                    if (_selectedContainer != null)
                    {
                        _selectedContainer.IsSelected = false;
                    }
                    if (_selectedContainers.Count > 0)
                    {
                        UpdateSelectedItem();
                    }
                }
            }

            if (defferedSelection && _selectedContainers.Count > 0)
            {
                _selectedContainers[0].IsSelected = true;
            }
        }

        private void UpdateSelectedItem()
        {
            _selectedContainers[0].IsSelected = true;
            _selectedContainer = _selectedContainers[0];
        }
    }
}
