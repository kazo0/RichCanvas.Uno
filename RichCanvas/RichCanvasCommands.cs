using System.Windows.Input;

namespace RichCanvas
{
    /// <summary>
    /// Holds pre-defined <see cref="RichCanvas"/> commands.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] WPF's RoutedUICommand and CommandManager.RegisterClassCommandBinding are not available in WinUI/Uno.
    /// Replaced with simple ICommand implementations (RelayCommand pattern).
    /// The commands are now bound to specific RichCanvas instances rather than being class-level routed commands.
    /// ZoomIn/ZoomOut keyboard accelerators are handled in the RichCanvas.Zooming.cs OnKeyDown handler.
    /// </remarks>
    public class RichCanvasCommands
    {
        /// <summary>
        /// Zoom in relative to the canvas current mouse position.
        /// </summary>
        public static RichCanvasCommand ZoomIn { get; } = new RichCanvasCommand(
            "Zoom in",
            canvas => canvas.ZoomIn());

        /// <summary>
        /// Zoom out relative to the canvas current mouse position.
        /// </summary>
        public static RichCanvasCommand ZoomOut { get; } = new RichCanvasCommand(
            "Zoom out",
            canvas => canvas.ZoomOut());
    }

    /// <summary>
    /// A simple command implementation for <see cref="RichCanvas"/> operations.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces RoutedUICommand. Consumers can bind these commands
    /// and set the CommandParameter to a RichCanvas instance, or use them directly.
    /// </remarks>
    public class RichCanvasCommand : ICommand
    {
        private readonly string _description;
        private readonly System.Action<RichCanvas> _execute;

        /// <summary>Creates a new command.</summary>
        public RichCanvasCommand(string description, System.Action<RichCanvas> execute)
        {
            _description = description;
            _execute = execute;
        }

        /// <inheritdoc/>
        public event System.EventHandler? CanExecuteChanged;

        /// <inheritdoc/>
        public bool CanExecute(object? parameter) => parameter is RichCanvas;

        /// <inheritdoc/>
        public void Execute(object? parameter)
        {
            if (parameter is RichCanvas canvas)
            {
                _execute(canvas);
            }
        }

        /// <summary>Raises the <see cref="CanExecuteChanged"/> event.</summary>
        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
    }
}
