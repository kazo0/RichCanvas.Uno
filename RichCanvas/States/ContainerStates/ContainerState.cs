using Microsoft.UI.Xaml.Input;

namespace RichCanvas.States.ContainerStates
{
    /// <summary>The base class for <see cref="RichCanvasContainer"/> states.</summary>
    /// <remarks>
    /// [WPF Migration] Mouse events replaced with Pointer events (PointerRoutedEventArgs).
    /// </remarks>
    public abstract class ContainerState
    {
        /// <summary>The owner of the state.</summary>
        protected RichCanvasContainer Container { get; }

        /// <summary>
        /// Constructs a new <see cref="ContainerState "/>.
        /// </summary>
        /// <param name="container">The owner of the state.</param>
        public ContainerState(RichCanvasContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Called whenever <see cref="RichCanvasContainer.PushState(ContainerState)"/> is called.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// Called whenever <see cref="RichCanvasContainer.PopState()"/> is called for re-entry.
        /// </summary>
        public virtual void ReEnter() { }

        /// <summary>
        /// Called whenever <see cref="RichCanvasContainer.PopState()"/> is called.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>Handles pointer pressed events.</summary>
        public virtual void HandlePointerPressed(PointerRoutedEventArgs e) { }

        /// <summary>Handles pointer move events.</summary>
        public virtual void HandlePointerMoved(PointerRoutedEventArgs e) { }

        /// <summary>Handles pointer released events.</summary>
        public virtual void HandlePointerReleased(PointerRoutedEventArgs e) { }

        /// <summary>Pushes a new state into the stack.</summary>
        /// <param name="state">The new state.</param>
        public virtual void PushState(ContainerState state) => Container.PushState(state);

        /// <summary>Pops the current state from the stack.</summary>
        public virtual void PopState() => Container.PopState();
    }
}
