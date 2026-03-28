using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace RichCanvas.Helpers
{
    /// <summary>
    /// Provides bounds-based hit testing to replace WPF's GeometryHitTestParameters.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] WPF's VisualTreeHelper.HitTest with GeometryHitTestParameters is not available in WinUI/Uno.
    /// This helper performs manual bounds-intersection testing by walking the visual tree
    /// and checking each child's transformed bounds against a target rectangle.
    /// </remarks>
    internal static class HitTestHelper
    {
        /// <summary>
        /// Finds all <see cref="RichCanvasContainer"/> children of <paramref name="panel"/> whose
        /// bounding boxes intersect with <paramref name="area"/>.
        /// </summary>
        internal static List<RichCanvasContainer> FindContainersInArea(RichCanvasPanel panel, Rect area)
        {
            var results = new List<RichCanvasContainer>();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(panel); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(panel, i);
                if (child is RichCanvasContainer container)
                {
                    if (container.IsValid())
                    {
                        // Use the pre-calculated bounding box if available, otherwise compute from position/size
                        Rect containerBounds = container.BoundingBox;
                        if (containerBounds.Width == 0 && containerBounds.Height == 0)
                        {
                            // Fallback: compute bounds from Left/Top/Width/Height
                            double w = double.IsNaN(container.Width) ? container.ActualWidth : container.Width;
                            double h = double.IsNaN(container.Height) ? container.ActualHeight : container.Height;
                            containerBounds = new Rect(container.Left, container.Top, w, h);
                        }

                        if (RectsIntersect(area, containerBounds))
                        {
                            results.Add(container);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Checks if two rectangles intersect. Works even when one or both rects may have zero area.
        /// </summary>
        private static bool RectsIntersect(Rect a, Rect b)
        {
            if (a.IsEmpty || b.IsEmpty)
                return false;

            return !(a.Left > b.Right || a.Right < b.Left || a.Top > b.Bottom || a.Bottom < b.Top);
        }
    }
}
