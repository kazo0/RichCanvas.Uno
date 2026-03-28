using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RichCanvas
{
    /// <summary>
    /// ItemsHost of <see cref="RichCanvas"/>
    /// </summary>
    /// <remarks>
    /// [WPF Migration] In WPF, this panel was used with IsItemsHost=True in the template.
    /// In WinUI/Uno, the panel is set via ItemsPanel template on ItemsControl.
    /// The panel is also referenced directly from the control template and assigned to the control via OnApplyTemplate.
    /// </remarks>
    public class RichCanvasPanel : Panel
    {
        private RichCanvas? _itemsOwner;

        internal RichCanvas ItemsOwner
        {
            get => _itemsOwner ?? throw new InvalidOperationException("RichCanvas not initialized");
            set => _itemsOwner = value;
        }

        /// <summary>
        /// Identifies the <see cref="Extent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtentProperty = DependencyProperty.Register(
            nameof(Extent), typeof(Rect), typeof(RichCanvasPanel), new PropertyMetadata(Rect.Empty));

        /// <summary>The area covered by the children of this panel.</summary>
        public Rect Extent
        {
            get => (Rect)GetValue(ExtentProperty);
            set => SetValue(ExtentProperty, value);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            if (_itemsOwner != null && (_itemsOwner.IsSelecting || _itemsOwner.IsDragging))
            {
                return default;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                UIElement child = Children[i];
                if (child is RichCanvasContainer container)
                {
                    container.Measure(constraint);
                }
            }

            return default;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            for (int i = 0; i < Children.Count; i++)
            {
                UIElement child = Children[i];
                if (child is RichCanvasContainer container)
                {
                    child.Arrange(new Rect(new Point(container.Left, container.Top), child.DesiredSize));

                    if (container.IsValid())
                    {
                        container.CalculateBoundingBox();

                        minX = Math.Min(minX, container.BoundingBox.Left);
                        minY = Math.Min(minY, container.BoundingBox.Top);
                        maxX = Math.Max(maxX, container.BoundingBox.Right);
                        maxY = Math.Max(maxY, container.BoundingBox.Bottom);
                    }
                }
            }
            Extent = minX == double.MaxValue
                ? Rect.Empty
                : new Rect(minX, minY, maxX - minX, maxY - minY);

            return arrangeSize;
        }
    }
}
