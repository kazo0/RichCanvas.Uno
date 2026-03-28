using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace RichCanvas
{
    /// <summary>
    /// Defines scrolling functionality for <see cref="RichCanvas"/>.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] WPF's IScrollInfo interface is not available in WinUI/Uno.
    /// The scrolling logic has been preserved as public methods on the RichCanvas class.
    /// The extent/offset/viewport tracking is done internally.
    /// ScrollViewer interaction is handled via the ViewportLocation property instead of IScrollInfo callbacks.
    /// </remarks>
    public partial class RichCanvas
    {
        #region Private Fields

        private double _offsetX;
        private double _offsetY;
        private double _extentWidth;
        private double _extentHeight;
        private Point? _viewportLocationBeforeScrolling;
        private bool _isScrolling;

        #endregion Private Fields

        #region Scroll Properties

        /// <summary>
        /// Gets the total height of the scrollable content.
        /// </summary>
        public double ExtentHeight => _extentHeight;

        /// <summary>
        /// Gets the total width of the scrollable content.
        /// </summary>
        public double ExtentWidth => _extentWidth;

        /// <summary>
        /// Gets the current horizontal scroll offset.
        /// </summary>
        public double HorizontalOffset => _offsetX;

        /// <summary>
        /// Gets the current vertical scroll offset.
        /// </summary>
        public double VerticalOffset => _offsetY;

        /// <summary>
        /// Gets the viewport height.
        /// </summary>
        public double ViewportHeight => ViewportSize.Height;

        /// <summary>
        /// Gets the viewport width.
        /// </summary>
        public double ViewportWidth => ViewportSize.Width;

        #endregion Scroll Properties

        #region Scroll Methods

        /// <summary>Scrolls down by the <see cref="ScrollFactor"/>.</summary>
        public void LineDown()
        {
            ViewportLocation = new Point(ViewportLocation.X, ViewportLocation.Y + ScrollFactor);
        }

        /// <summary>Scrolls left by the <see cref="ScrollFactor"/>.</summary>
        public void LineLeft()
        {
            ViewportLocation = new Point(ViewportLocation.X - ScrollFactor, ViewportLocation.Y);
        }

        /// <summary>Scrolls right by the <see cref="ScrollFactor"/>.</summary>
        public void LineRight()
        {
            ViewportLocation = new Point(ViewportLocation.X + ScrollFactor, ViewportLocation.Y);
        }

        /// <summary>Scrolls up by the <see cref="ScrollFactor"/>.</summary>
        public void LineUp()
        {
            ViewportLocation = new Point(ViewportLocation.X, ViewportLocation.Y - ScrollFactor);
        }

        /// <summary>Scrolls down by the viewport height.</summary>
        public void PageDown()
            => ViewportLocation = new Point(ViewportLocation.X, ViewportLocation.Y + ViewportSize.Height);

        /// <summary>Scrolls left by the viewport width.</summary>
        public void PageLeft()
            => ViewportLocation = new Point(ViewportLocation.X - ViewportSize.Width, ViewportLocation.Y);

        /// <summary>Scrolls right by the viewport width.</summary>
        public void PageRight()
            => ViewportLocation = new Point(ViewportLocation.X + ViewportSize.Width, ViewportLocation.Y);

        /// <summary>Scrolls up by the viewport height.</summary>
        public void PageUp()
            => ViewportLocation = new Point(ViewportLocation.X, ViewportLocation.Y - ViewportSize.Height);

        /// <summary>
        /// Sets the horizontal scroll offset.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            _offsetX = offset;
            UpdateViewportLocationOnScroll();
        }

        /// <summary>
        /// Sets the vertical scroll offset.
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            _offsetY = offset;
            UpdateViewportLocationOnScroll();
        }

        #endregion Scroll Methods

        #region Internal Scroll Logic

        private void UpdateViewportLocationOnScroll()
        {
            if (!_viewportLocationBeforeScrolling.HasValue)
            {
                _viewportLocationBeforeScrolling = ViewportLocation;
            }

            _isScrolling = true;

            double locationX = Math.Min(ItemsExtent.Left, _viewportLocationBeforeScrolling.Value.X) + HorizontalOffset;
            double locationY = Math.Min(ItemsExtent.Top, _viewportLocationBeforeScrolling.Value.Y) + VerticalOffset;
            ViewportLocation = new Point(locationX, locationY);
            EnsureExtentIsUpdated();
            _isScrolling = false;
        }

        private void EnsureExtentIsUpdated()
        {
            Rect extentWithItems = ItemsExtent;
            if (extentWithItems.IsEmpty)
            {
                extentWithItems = new Rect(ViewportLocation, ViewportSize);
            }
            else
            {
                extentWithItems = UnionRects(extentWithItems, new Rect(ViewportLocation, ViewportSize));
            }

            double scrollOffsetX = ViewportLocation.X - ItemsExtent.Left;
            double scrollOffsetY = ViewportLocation.Y - ItemsExtent.Top;

            if (_extentHeight + Math.Max(0, scrollOffsetY) <= extentWithItems.Height)
            {
                _extentHeight = extentWithItems.Height;
            }

            if (_extentWidth + Math.Max(0, scrollOffsetX) <= extentWithItems.Width)
            {
                _extentWidth = extentWithItems.Width;
            }
        }

        private void UpdateScrollbars()
        {
            if (!_isScrolling)
            {
                _viewportLocationBeforeScrolling = null;

                Rect extent = ItemsExtent;
                if (extent.IsEmpty)
                {
                    extent = new Rect(ViewportLocation, ViewportSize);
                }
                else
                {
                    extent = UnionRects(extent, new Rect(ViewportLocation, ViewportSize));
                }

                _extentHeight = extent.Height;
                _extentWidth = extent.Width;

                double scrollOffsetX = ViewportLocation.X - ItemsExtent.Left;
                double scrollOffsetY = ViewportLocation.Y - ItemsExtent.Top;

                _offsetX = Math.Max(0, scrollOffsetX);
                _offsetY = Math.Max(0, scrollOffsetY);
            }
        }

        /// <summary>
        /// Computes the union of two rectangles.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] WPF Rect had a Union() instance method. WinUI Rect does not, so we compute it manually.
        /// </remarks>
        private static Rect UnionRects(Rect a, Rect b)
        {
            if (a.IsEmpty) return b;
            if (b.IsEmpty) return a;

            double left = Math.Min(a.Left, b.Left);
            double top = Math.Min(a.Top, b.Top);
            double right = Math.Max(a.Right, b.Right);
            double bottom = Math.Max(a.Bottom, b.Bottom);
            return new Rect(left, top, right - left, bottom - top);
        }

        #endregion Internal Scroll Logic
    }
}
