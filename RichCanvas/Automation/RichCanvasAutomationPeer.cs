using System.Text.Json;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.Foundation;

using RichCanvas.Automation.ControlInformations;

namespace RichCanvas.Automation
{
    /// <summary>
    /// Exposes the <see cref="RichCanvas"/> to UI Automation.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Key changes:
    /// - Base class changed from SelectorAutomationPeer to ItemsControlAutomationPeer
    ///   since RichCanvas now inherits from ItemsControl instead of MultiSelector/Selector.
    /// - IScrollProvider implementation simplified (no longer wraps IScrollInfo).
    /// - Newtonsoft.Json replaced with System.Text.Json.
    /// - CreateItemAutomationPeer replaces item-level automation.
    /// </remarks>
    public class RichCanvasAutomationPeer : ItemsControlAutomationPeer,
        IValueProvider,
        IScrollProvider
    {
        /// <summary>
        /// Gets the <see cref="RichCanvas"/> that is associated with this <see cref="RichCanvasAutomationPeer"/>.
        /// </summary>
        protected RichCanvas OwnerRichCanvas => (RichCanvas)Owner;

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <summary>
        /// Gets the serialized json value of <see cref="RichCanvasData"/> containing data about the associated <see cref="RichCanvas"/>.
        /// </summary>
        public string Value => JsonSerializer.Serialize(new RichCanvasData
        {
            TranslateTransformX = OwnerRichCanvas.TranslateTransform.X,
            TranslateTransformY = OwnerRichCanvas.TranslateTransform.Y,
            ItemsExtent = OwnerRichCanvas.ItemsExtent,
            ScrollFactor = OwnerRichCanvas.ScrollFactor,
            ViewportLocation = OwnerRichCanvas.ViewportLocation,
            ViewportSize = OwnerRichCanvas.ViewportSize,
            ViewportExtent = new Size(OwnerRichCanvas.ExtentWidth, OwnerRichCanvas.ExtentHeight),
            ViewportZoom = OwnerRichCanvas.ViewportZoom,
            ScaleFactor = OwnerRichCanvas.ScaleFactor,
            MousePosition = OwnerRichCanvas.MousePosition,
            MaxZoom = OwnerRichCanvas.MaxScale,
            MinZoom = OwnerRichCanvas.MinScale
        });

        /// <summary>
        /// Always true.
        /// </summary>
        public bool HorizontallyScrollable => true;

        /// <summary>
        /// Gets associated <see cref="RichCanvas.HorizontalOffset"/> value.
        /// </summary>
        public double HorizontalScrollPercent => OwnerRichCanvas.HorizontalOffset;

        /// <summary>
        /// Gets associated <see cref="RichCanvas.ViewportSize"/>.Width value.
        /// </summary>
        public double HorizontalViewSize => OwnerRichCanvas.ViewportSize.Width;

        /// <summary>
        /// Always true.
        /// </summary>
        public bool VerticallyScrollable => true;

        /// <summary>
        /// Gets associated <see cref="RichCanvas.VerticalOffset"/> value.
        /// </summary>
        public double VerticalScrollPercent => OwnerRichCanvas.VerticalOffset;

        /// <summary>
        /// Gets associated <see cref="RichCanvas.ViewportSize"/>.Height value.
        /// </summary>
        public double VerticalViewSize => OwnerRichCanvas.ViewportSize.Height;

        /// <summary>
        /// Initializes a new <see cref="RichCanvasAutomationPeer"/>.
        /// </summary>
        public RichCanvasAutomationPeer(RichCanvas owner) : base(owner)
        {
        }

        /// <inheritdoc/>
        public void SetValue(string value)
        {
            throw new System.NotSupportedException("This control does not allow setting the value.");
        }

        /// <inheritdoc/>
        protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface switch
        {
            PatternInterface.Value => this,
            PatternInterface.Scroll => this,
            _ => base.GetPatternCore(patternInterface)
        };

        /// <inheritdoc/>
        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Custom;

        /// <inheritdoc/>
        protected override string GetClassNameCore() => Owner.GetType().Name;

        /// <summary>
        /// Creates an automation peer for the specified item.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] In WPF, CreateItemAutomationPeer was virtual on SelectorAutomationPeer.
        /// In Uno's ItemsControlAutomationPeer it is not virtual, so we use 'new' to provide our own.
        /// </remarks>
        protected new ItemAutomationPeer CreateItemAutomationPeer(object item)
            => new RichCanvasContainerAutomationPeer(item, this);

        /// <summary>
        /// Scrolls the canvas using the viewport location methods.
        /// </summary>
        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            if (verticalAmount == ScrollAmount.SmallIncrement)
            {
                OwnerRichCanvas.LineDown();
            }
            if (verticalAmount == ScrollAmount.SmallDecrement)
            {
                OwnerRichCanvas.LineUp();
            }
            if (verticalAmount == ScrollAmount.LargeIncrement)
            {
                OwnerRichCanvas.PageDown();
            }
            if (verticalAmount == ScrollAmount.LargeDecrement)
            {
                OwnerRichCanvas.PageUp();
            }

            if (horizontalAmount == ScrollAmount.SmallIncrement)
            {
                OwnerRichCanvas.LineLeft();
            }
            if (horizontalAmount == ScrollAmount.SmallDecrement)
            {
                OwnerRichCanvas.LineRight();
            }
            if (horizontalAmount == ScrollAmount.LargeIncrement)
            {
                OwnerRichCanvas.PageLeft();
            }
            if (horizontalAmount == ScrollAmount.LargeDecrement)
            {
                OwnerRichCanvas.PageRight();
            }
        }

        /// <summary>
        /// Sets the amount of vertical and horizontal offset.
        /// </summary>
        public void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            OwnerRichCanvas.SetVerticalOffset(OwnerRichCanvas.VerticalOffset + verticalPercent);
            OwnerRichCanvas.SetHorizontalOffset(OwnerRichCanvas.HorizontalOffset + horizontalPercent);
        }
    }
}
