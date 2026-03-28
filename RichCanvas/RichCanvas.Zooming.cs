using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using RichCanvas.Gestures;

namespace RichCanvas
{
    public partial class RichCanvas
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ScaleFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty = DependencyProperty.Register(
            nameof(ScaleFactor), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(1.1d));

        /// <summary>
        /// Gets or sets the factor used to change <see cref="ScaleTransform"/> on zoom.
        /// Default is 1.1d.
        /// </summary>
        public double ScaleFactor
        {
            get => (double)GetValue(ScaleFactorProperty);
            set => SetValue(ScaleFactorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DisableZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisableZoomProperty = DependencyProperty.Register(
            nameof(DisableZoom), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether zooming operation is disabled.
        /// Default is enabled.
        /// </summary>
        public bool DisableZoom
        {
            get => (bool)GetValue(DisableZoomProperty);
            set => SetValue(DisableZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxScale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxScaleProperty = DependencyProperty.Register(
            nameof(MaxScale), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(2d, OnMaxScaleChanged));

        /// <summary>
        /// Gets or sets maximum scale for <see cref="ScaleTransform"/>.
        /// Default is 2.
        /// </summary>
        public double MaxScale
        {
            get => (double)GetValue(MaxScaleProperty);
            set => SetValue(MaxScaleProperty, value);
        }

        private static void OnMaxScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zoom = (RichCanvas)d;
            double newMax = (double)e.NewValue;
            // Coerce: max must be >= min
            if (newMax < zoom.MinScale)
            {
                zoom.MaxScale = 2d;
                return;
            }
            // Coerce ViewportZoom to be within range
            zoom.CoerceViewportZoom();
        }

        /// <summary>
        /// Identifies the <see cref="MinScale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinScaleProperty = DependencyProperty.Register(
            nameof(MinScale), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(0.1d, OnMinimumScaleChanged));

        /// <summary>
        /// Gets or sets minimum scale for <see cref="RichCanvas.ScaleTransform"/>.
        /// Default is 0.1d.
        /// </summary>
        public double MinScale
        {
            get => (double)GetValue(MinScaleProperty);
            set => SetValue(MinScaleProperty, value);
        }

        private static void OnMinimumScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zoom = (RichCanvas)d;
            double newMin = (double)e.NewValue;
            // Coerce: min must be > 0
            if (newMin <= 0)
            {
                zoom.MinScale = 0.1d;
                return;
            }
            // Coerce MaxScale if needed
            if (zoom.MaxScale < newMin)
            {
                zoom.MaxScale = 2d;
            }
            zoom.CoerceViewportZoom();
        }

        /// <summary>
        /// Identifies the <see cref="ViewportZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewportZoomProperty = DependencyProperty.Register(
            nameof(ViewportZoom), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(1d, OnViewportZoomChanged));

        /// <summary>
        /// Gets or sets the current <see cref="RichCanvas.ScaleTransform"/> value.
        /// Default is 1.
        /// </summary>
        public double ViewportZoom
        {
            get => (double)GetValue(ViewportZoomProperty);
            set => SetValue(ViewportZoomProperty, value);
        }

        private static void OnViewportZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (RichCanvas)d;
            double newValue = (double)e.NewValue;

            // Coerce within range
            if (canvas.DisableZoom)
            {
                double oldValue = (double)e.OldValue;
                if (Math.Abs(newValue - oldValue) > 0.0001)
                {
                    canvas.ViewportZoom = oldValue;
                    return;
                }
            }

            if (newValue < canvas.MinScale)
            {
                canvas.ViewportZoom = canvas.MinScale;
                return;
            }
            if (newValue > canvas.MaxScale)
            {
                canvas.ViewportZoom = canvas.MaxScale;
                return;
            }

            canvas.OverrideScale(newValue);
        }

        /// <summary>
        /// Occurs whenever <see cref="RichCanvas"/> is zoomed in or out.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent. Now a CLR event.
        /// </remarks>
        public event EventHandler<Point>? Zooming;

        #endregion Dependency Properties

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
        {
            // [WPF Migration] WPF used OnPreviewMouseWheel with Keyboard.Modifiers.
            // WinUI uses OnPointerWheelChanged with e.KeyModifiers.
            var keyModifiers = InputHelper.GetCurrentModifiers();
            if (RichCanvasGestures.ZoomModifierKey == keyModifiers)
            {
                Point position = e.GetCurrentPoint(ItemsHost).Position;
                IsZooming = true;
                int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                double scaleFactor = delta > 0 ? ScaleFactor : 1 / ScaleFactor;
                ZoomAtPosition(position, scaleFactor);
                IsZooming = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Zooms the <see cref="RichCanvas"/> at the specified <paramref name="mousePosition"/> using the given <paramref name="delta"/>.
        /// </summary>
        /// <param name="mousePosition">Mouse position where to zoom at.</param>
        /// <param name="delta">Value of each zooming step.</param>
        public void ZoomAtPosition(Point mousePosition, double delta)
        {
            if (!DisableZoom)
            {
                Point previouslyTransformedMousePosition = AppliedTransform.TransformPoint(mousePosition);

                double previousZoom = ViewportZoom;
                ViewportZoom *= delta;

                if (Math.Abs(previousZoom - ViewportZoom) > 0.001)
                {
                    Point transformedMousePositionAfterScaling = AppliedTransform.TransformPoint(mousePosition);

                    double adjX = previouslyTransformedMousePosition.X - transformedMousePositionAfterScaling.X;
                    double adjY = previouslyTransformedMousePosition.Y - transformedMousePositionAfterScaling.Y;
                    Point newTranslation = new Point(TranslateTransform.X + adjX, TranslateTransform.Y + adjY);

                    double viewportX = -newTranslation.X / ViewportZoom;
                    double viewportY = -newTranslation.Y / ViewportZoom;

                    ViewportLocation = new Point(viewportX, viewportY);
                }
            }
        }

        /// <summary>
        /// Zooms in the <see cref="RichCanvas"/> using its <see cref="MousePosition"/> and its <see cref="ScaleFactor"/>.
        /// </summary>
        public void ZoomIn() => ZoomAtPosition(MousePosition, ScaleFactor);

        /// <summary>
        /// Zooms out the <see cref="RichCanvas"/> using its <see cref="MousePosition"/> and its <see cref="ScaleFactor"/>.
        /// </summary>
        public void ZoomOut() => ZoomAtPosition(MousePosition, 1 / ScaleFactor);

        private void OverrideScale(double zoom)
        {
            ScaleTransform.ScaleX = zoom;
            ScaleTransform.ScaleY = zoom;
            Zooming?.Invoke(this, new Point(ScaleTransform.ScaleX, ScaleTransform.ScaleY));

            ViewportSize = new Size(ActualWidth / ViewportZoom, ActualHeight / ViewportZoom);

            UpdateScrollbars();
        }

        /// <summary>
        /// Coerces the ViewportZoom value to be within MinScale..MaxScale range.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Replaces WPF's CoerceValueCallback on the ViewportZoom DP.
        /// </remarks>
        private void CoerceViewportZoom()
        {
            double current = ViewportZoom;
            if (current < MinScale)
            {
                ViewportZoom = MinScale;
            }
            else if (current > MaxScale)
            {
                ViewportZoom = MaxScale;
            }
        }
    }
}
