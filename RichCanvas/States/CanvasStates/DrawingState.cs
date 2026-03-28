using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using RichCanvas.Gestures;

namespace RichCanvas.States
{
    /// <summary>
    /// Defines a new state used when a drawing action happens on <see cref="RichCanvas"/>.
    /// </summary>
    public class DrawingState : CanvasState
    {
        private RichCanvasContainer _currentDrawingContainer = null!;
        private bool _isDrawing;

        /// <summary>
        /// Initializes a new <see cref="DrawingState"/>.
        /// </summary>
        /// <param name="parent">Owner of the state.</param>
        public DrawingState(RichCanvas parent) : base(parent)
        {
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            List<int> drawingContainersIndexes = Parent.CurrentDrawingIndexes;
            if (drawingContainersIndexes.Count == 0)
            {
                return;
            }

            int currentDrawingContainerIndex = drawingContainersIndexes[0];
            RichCanvasContainer? container = Parent.ContainerFromIndex(currentDrawingContainerIndex);
            if (container == null || container.IsValid())
            {
                drawingContainersIndexes.RemoveAt(0);
                return;
            }

            _currentDrawingContainer = container;
            _isDrawing = true;

            Point mousePosition = Parent.MousePosition;
            if (!_currentDrawingContainer.TopPropertyInitalized)
            {
                _currentDrawingContainer.Top = mousePosition.Y;
            }
            if (!_currentDrawingContainer.LeftPropertyInitialized)
            {
                _currentDrawingContainer.Left = mousePosition.X;
            }
            DrawContainer(mousePosition);

            drawingContainersIndexes.RemoveAt(0);
        }

        /// <inheritdoc/>
        public override void HandlePointerMoved(PointerRoutedEventArgs e)
        {
            if (!_isDrawing)
            {
                return;
            }

            Point mousePosition = e.GetCurrentPoint(Parent.ItemsHost).Position;
            DrawContainer(mousePosition);
        }

        /// <inheritdoc/>
        public override void HandlePointerReleased(PointerRoutedEventArgs e)
        {
            if (!_isDrawing)
            {
                return;
            }

            if (_currentDrawingContainer.AllowScaleChangeToUpdatePosition)
            {
                UpdateItemPositionByScale();
            }
            if (Parent.EnableSnapping)
            {
                SnapToGrid();
            }
            _currentDrawingContainer.Scale = new Point(_currentDrawingContainer.ScaleTransform?.ScaleX ?? 1, _currentDrawingContainer.ScaleTransform?.ScaleY ?? 1);

            Point mousePosition = e.GetCurrentPoint(Parent.ItemsHost).Position;
            Parent.RaiseDrawEndedEvent(_currentDrawingContainer.DataContext, mousePosition);
            if (Parent.DrawingEndedCommand?.CanExecute(mousePosition) ?? false)
            {
                Parent.DrawingEndedCommand?.Execute(mousePosition);
            }

            Parent.ItemsHost?.InvalidateMeasure();
            _isDrawing = false;
        }

        /// <inheritdoc/>
        public override void HandleAutoPanning(PointerRoutedEventArgs? e)
        {
            Point mousePosition = Parent.MousePosition;
            _currentDrawingContainer.Height = Math.Abs(mousePosition.Y - _currentDrawingContainer.Top);
            _currentDrawingContainer.Width = Math.Abs(mousePosition.X - _currentDrawingContainer.Left);
        }

        private void DrawContainer(Point mousePosition)
        {
            double width = mousePosition.X - _currentDrawingContainer.Left;
            double height = mousePosition.Y - _currentDrawingContainer.Top;

            _currentDrawingContainer.Width = width == 0 ? RichCanvasContainer.DefaultWidth : Math.Abs(width);
            _currentDrawingContainer.Height = height == 0 ? RichCanvasContainer.DefaultHeight : Math.Abs(height);

            ScaleTransform? scaleTransform = _currentDrawingContainer.ScaleTransform;
            if (scaleTransform != null)
            {
                if (width < 0 && scaleTransform.ScaleX == 1)
                {
                    scaleTransform.ScaleX = -1;
                }

                if (height < 0 && scaleTransform.ScaleY == 1)
                {
                    scaleTransform.ScaleY = -1;
                }

                if (height > 0 && scaleTransform.ScaleY == -1)
                {
                    scaleTransform.ScaleY = 1;
                }
                if (width > 0 && scaleTransform.ScaleX == -1)
                {
                    scaleTransform.ScaleX = 1;
                }
            }
        }

        private void UpdateItemPositionByScale()
        {
            ScaleTransform? scaleTransformItem = _currentDrawingContainer.ScaleTransform;

            if (scaleTransformItem != null)
            {
                _currentDrawingContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                if (scaleTransformItem.ScaleX < 0 && scaleTransformItem.ScaleY > 0)
                {
                    _currentDrawingContainer.Left -= _currentDrawingContainer.Width;
                }
                else if (scaleTransformItem.ScaleX < 0 && scaleTransformItem.ScaleY < 0)
                {
                    _currentDrawingContainer.Left -= _currentDrawingContainer.Width;
                    _currentDrawingContainer.Top -= _currentDrawingContainer.Height;
                }
                else if (scaleTransformItem.ScaleX > 0 && scaleTransformItem.ScaleY < 0)
                {
                    _currentDrawingContainer.Top -= _currentDrawingContainer.Height;
                }
            }
        }

        private void SnapToGrid()
        {
            _currentDrawingContainer.Left = Math.Round(_currentDrawingContainer.Left / Parent.GridSpacing) * Parent.GridSpacing;
            _currentDrawingContainer.Top = Math.Round(_currentDrawingContainer.Top / Parent.GridSpacing) * Parent.GridSpacing;
        }
    }
}
