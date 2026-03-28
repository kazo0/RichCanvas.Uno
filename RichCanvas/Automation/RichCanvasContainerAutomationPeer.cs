using System.Text.Json;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

using RichCanvas.Automation.ControlInformations;

namespace RichCanvas.Automation
{
    /// <summary>
    /// Exposes the <see cref="RichCanvasContainer"/> to UI Automation.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Key changes:
    /// - Base class changed from SelectorItemAutomationPeer to ItemAutomationPeer
    ///   since RichCanvas now inherits from ItemsControl instead of Selector.
    /// TODO: [WPF Migration] ItemAutomationPeer.ItemsControlAutomationPeer, Item, and the constructor
    /// are not fully implemented in Uno Platform. This peer will compile but may not function
    /// on all Uno targets. Consider simplifying to FrameworkElementAutomationPeer if automation
    /// is not required on non-Windows platforms.
    /// - Newtonsoft.Json replaced with System.Text.Json.
    /// </remarks>
    public class RichCanvasContainerAutomationPeer : ItemAutomationPeer, IValueProvider
    {
        /// <summary>
        /// Gets the <see cref="RichCanvas"/> that is associated with this peer.
        /// </summary>
        protected RichCanvas OwnerRichCanvas => (RichCanvas)ItemsControlAutomationPeer.Owner;

        /// <summary>
        /// Gets the <see cref="RichCanvasContainer"/> that is associated with this peer.
        /// </summary>
        protected RichCanvasContainer? Container => OwnerRichCanvas.ContainerFromItem(Item);

        /// <summary>
        /// Gets the serialized json value of <see cref="RichCanvasContainerData"/> containing data about the associated <see cref="RichCanvasContainer"/>.
        /// </summary>
        public string Value
        {
            get
            {
                var container = Container;
                if (container == null) return "{}";

                return JsonSerializer.Serialize(new RichCanvasContainerData
                {
                    Top = container.Top,
                    Left = container.Left,
                    IsSelected = container.IsSelected,
                    ScaleX = container.ScaleTransform?.ScaleX ?? -1,
                    ScaleY = container.ScaleTransform?.ScaleY ?? -1,
                    DataContextType = container.DataContext?.GetType()
                });
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <summary>
        /// Initializes a new <see cref="RichCanvasContainerAutomationPeer"/>.
        /// </summary>
        /// <param name="item">The data item associated with a <see cref="RichCanvasContainer"/>.</param>
        /// <param name="itemsControlAutomationPeer">Owner <see cref="RichCanvasAutomationPeer"/>.</param>
        public RichCanvasContainerAutomationPeer(object item, ItemsControlAutomationPeer itemsControlAutomationPeer) : base(item, itemsControlAutomationPeer)
        {
        }

        /// <inheritdoc/>
        protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface switch
        {
            PatternInterface.Value => this,
            _ => base.GetPatternCore(patternInterface)
        };

        /// <inheritdoc/>
        public void SetValue(string value)
        {
            throw new System.NotSupportedException("This control does not allow setting the value.");
        }

        /// <inheritdoc/>
        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Custom;

        /// <inheritdoc/>
        protected override string GetClassNameCore() => nameof(RichCanvasContainer);
    }
}
