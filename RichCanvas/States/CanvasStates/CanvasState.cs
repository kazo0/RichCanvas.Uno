using Microsoft.UI.Xaml.Input;

namespace RichCanvas.States
{
    /// <summary>The base class for <see cref="RichCanvas"/> states.</summary>
    /// <remarks>
    /// [WPF Migration] All Mouse*EventArgs replaced with PointerRoutedEventArgs.
    /// KeyEventArgs replaced with KeyRoutedEventArgs.
    /// </remarks>
    public abstract class CanvasState
    {
        /// <summary>The owner of the state.</summary>
        protected RichCanvas Parent { get; }

        /// <summary>
        /// Constructs a new <see cref="CanvasState "/>.
        /// </summary>
        /// <param name="parent">The owner of the state.</param>
        public CanvasState(RichCanvas parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Called whenever <see cref="RichCanvas.PushState(CanvasState)"/> is called (becomes the <see cref="RichCanvas.CurrentState"/>).
        /// <br />
        /// Note: <i>Used to initialize the State before any input is processed by it.</i>
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// Called whenever <see cref="RichCanvas.PopState()"/> is called.
        /// <br />
        /// Note: <i>Used whenever a state switch happens in order to update the state which was suspended.</i>
        /// </summary>
        public virtual void ReEnter() { }

        /// <summary>
        /// Called whenever <see cref="RichCanvas.PopState()"/> is called.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>Handles pointer pressed events.</summary>
        public virtual void HandlePointerPressed(PointerRoutedEventArgs e) { }

        /// <summary>Handles pointer move events.</summary>
        public virtual void HandlePointerMoved(PointerRoutedEventArgs e) { }

        /// <summary>Handles pointer released events.</summary>
        public virtual void HandlePointerReleased(PointerRoutedEventArgs e) { }

        /// <summary>Handles key down events.</summary>
        public virtual void HandleKeyDown(KeyRoutedEventArgs e) { }

        /// <summary>Handles key up events.</summary>
        public virtual void HandleKeyUp(KeyRoutedEventArgs e) { }

        /// <summary>Handles auto panning when mouse is outside the canvas.</summary>
        public virtual void HandleAutoPanning(PointerRoutedEventArgs? e) { }

        /// <summary>Pushes a new state into the stack.</summary>
        /// <param name="state">The new state.</param>
        public virtual void PushState(CanvasState state) => Parent.PushState(state);

        /// <summary>Pops the current state from the stack.</summary>
        public virtual void PopState() => Parent.PopState();

        /// <summary>
        /// Called by RichCanvas.OnPointerPressed to check if any state has priority over other controls handling the event.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Replaces MatchesPreviewMouseDownState. WPF's PreviewMouseDown (tunneling) is not available in WinUI.
        /// This method is now called before the normal pointer pressed handling to provide the same priority behavior.
        /// </remarks>
        public virtual bool MatchesPreviewPointerPressedState(PointerRoutedEventArgs e, out CanvasState? matchingState)
        {
            matchingState = null;
            return false;
        }
    }
}
