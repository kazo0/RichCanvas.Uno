using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using RichCanvas.Automation;
using RichCanvas.CustomEventArgs;
using RichCanvas.Gestures;
using RichCanvas.Helpers;
using RichCanvas.States;

namespace RichCanvas
{
    /// <summary>
    /// ItemsControl hosting <see cref="RichCanvasPanel"/>
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Key changes:
    /// - Base class changed from MultiSelector (WPF-only) to ItemsControl.
    ///   Multi-selection is implemented manually via internal selected items list,
    ///   BeginSelectionTransaction/EndSelectionTransaction, and SelectionChanged event.
    /// - IScrollInfo removed (WPF-only). Scrolling is managed via custom methods that manipulate
    ///   ViewportLocation and extent tracking.
    /// - RoutedEvents (DrawingEnded, Zooming) replaced with CLR events since EventManager.RegisterRoutedEvent
    ///   is not available in WinUI.
    /// - DependencyPropertyKey / RegisterReadOnly not available. Read-only properties use regular DPs
    ///   with internal setters.
    /// - FrameworkPropertyMetadata replaced with PropertyMetadata (no coerce/affectsrender flags).
    /// - CoerceValueCallback implemented in PropertyChangedCallback.
    /// - Mouse events replaced with Pointer events.
    /// - Mouse.Capture/Release replaced with CapturePointer/ReleasePointerCapture.
    /// - Preview* tunneling events not available; simulated by checking gesture matches before normal handling.
    /// - BitmapCache/CacheMode removed (not available in WinUI the same way).
    /// - DispatcherTimer uses Microsoft.UI.Xaml.DispatcherTimer (no priority parameter).
    /// </remarks>
    [TemplatePart(Name = DrawingPanelName, Type = typeof(Panel))]
    [TemplatePart(Name = SelectionRectangleName, Type = typeof(Rectangle))]
    public partial class RichCanvas : ItemsControl
    {
        #region Constants

        private const string DrawingPanelName = "PART_Panel";
        private const string SelectionRectangleName = "PART_SelectionRectangle";

        #endregion Constants

        #region Private Fields

        internal readonly ScaleTransform ScaleTransform = new ScaleTransform();
        internal readonly TranslateTransform TranslateTransform = new TranslateTransform();
        private RichCanvasPanel? _mainPanel;
        private DispatcherTimer? _autoPanTimer;
        private Stack<CanvasState> _states;
        private uint? _capturedPointerId;

        // Selection management (replaces WPF MultiSelector)
        private readonly List<object> _internalSelectedItems = new List<object>();
#pragma warning disable CS0414 // Field is used for batch selection tracking
        private bool _isUpdatingSelection;
#pragma warning restore CS0414

        #endregion Private Fields

        #region Properties API

        /// <summary>
        /// Gets the current state telling the action that happens on <see cref="RichCanvas"/>.
        /// </summary>
        public CanvasState CurrentState => _states.Peek();

        /// <summary>
        /// Identifies the <see cref="MousePosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MousePositionProperty = DependencyProperty.Register(
            nameof(MousePosition), typeof(Point), typeof(RichCanvas),
            new PropertyMetadata(default(Point)));

        /// <summary>
        /// Gets or sets mouse position relative to <see cref="ItemsHost"/>.
        /// </summary>
        public Point MousePosition
        {
            get => (Point)GetValue(MousePositionProperty);
            set => SetValue(MousePositionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectionRectangle"/> dependency property.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was RegisterReadOnly with DependencyPropertyKey. Now a regular DP with internal setter.
        /// </remarks>
        public static readonly DependencyProperty SelectionRectangleProperty = DependencyProperty.Register(
            nameof(SelectionRectangle), typeof(Rect), typeof(RichCanvas),
            new PropertyMetadata(default(Rect)));

        /// <summary>
        /// Gets the selection area as <see cref="Rect"/>.
        /// </summary>
        public Rect SelectionRectangle
        {
            get => (Rect)GetValue(SelectionRectangleProperty);
            internal set => SetValue(SelectionRectangleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSelecting"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectingProperty = DependencyProperty.Register(
            nameof(IsSelecting), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets whether the operation in progress is selection.
        /// </summary>
        public bool IsSelecting
        {
            get => (bool)GetValue(IsSelectingProperty);
            internal set => SetValue(IsSelectingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AppliedTransform"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AppliedTransformProperty = DependencyProperty.Register(
            nameof(AppliedTransform), typeof(TransformGroup), typeof(RichCanvas),
            new PropertyMetadata(default(TransformGroup)));

        /// <summary>
        /// Gets the transform that is applied to all child controls.
        /// </summary>
        public TransformGroup AppliedTransform
        {
            get => (TransformGroup)GetValue(AppliedTransformProperty);
            internal set => SetValue(AppliedTransformProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableAutoPanning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableAutoPanningProperty = DependencyProperty.Register(
            nameof(EnableAutoPanning), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false, OnEnableAutoPanningChanged));

        /// <summary>
        /// Gets or sets whether Auto-Panning is enabled.
        /// Default is disabled.
        /// </summary>
        public bool EnableAutoPanning
        {
            get => (bool)GetValue(EnableAutoPanningProperty);
            set => SetValue(EnableAutoPanningProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoPanTickRate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPanTickRateProperty = DependencyProperty.Register(
            nameof(AutoPanTickRate), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(1.0, OnAutoPanTickRateChanged));

        /// <summary>
        /// Gets or sets <see cref="DispatcherTimer"/> interval value.
        /// Default is 1.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Changed type from float to double for consistency with WinUI DPs.
        /// </remarks>
        public double AutoPanTickRate
        {
            get => (double)GetValue(AutoPanTickRateProperty);
            set => SetValue(AutoPanTickRateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoPanSpeed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPanSpeedProperty = DependencyProperty.Register(
            nameof(AutoPanSpeed), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(1.0));

        /// <summary>
        /// Gets or sets the <see cref="ItemsHost"/> translate speed.
        /// Default is 1.
        /// </summary>
        public double AutoPanSpeed
        {
            get => (double)GetValue(AutoPanSpeedProperty);
            set => SetValue(AutoPanSpeedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GridSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridSpacingProperty = DependencyProperty.Register(
            nameof(GridSpacing), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(20.0));

        /// <summary>
        /// Gets or sets grid drawing viewport size.
        /// Default is 20.
        /// </summary>
        public double GridSpacing
        {
            get => (double)GetValue(GridSpacingProperty);
            set => SetValue(GridSpacingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ViewportLocation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewportLocationProperty = DependencyProperty.Register(
            nameof(ViewportLocation), typeof(Point), typeof(RichCanvas),
            new PropertyMetadata(default(Point), OnViewportLocationChanged));

        /// <summary>
        /// Gets or sets current viewport location.
        /// </summary>
        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableSnapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableSnappingProperty = DependencyProperty.Register(
            nameof(EnableSnapping), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether grid snap correction on <see cref="RichCanvasContainer"/> is applied.
        /// Default is disabled.
        /// </summary>
        public bool EnableSnapping
        {
            get => (bool)GetValue(EnableSnappingProperty);
            set => SetValue(EnableSnappingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectionRectangleStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionRectangleStyleProperty = DependencyProperty.Register(
            nameof(SelectionRectangleStyle), typeof(Style), typeof(RichCanvas),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets selection <see cref="Rectangle"/> style.
        /// </summary>
        public Style SelectionRectangleStyle
        {
            get => (Style)GetValue(SelectionRectangleStyleProperty);
            set => SetValue(SelectionRectangleStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ScrollFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollFactorProperty = DependencyProperty.Register(
            nameof(ScrollFactor), typeof(double), typeof(RichCanvas),
            new PropertyMetadata(10d, OnScrollFactorChanged));

        /// <summary>
        /// Gets or sets the scrolling factor applied when scrolling.
        /// Default is 10.
        /// </summary>
        public double ScrollFactor
        {
            get => (double)GetValue(ScrollFactorProperty);
            set => SetValue(ScrollFactorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems), typeof(IList), typeof(RichCanvas),
            new PropertyMetadata(default(IList), OnSelectedItemsSourceChanged));

        /// <summary>
        /// Gets or sets the items in the <see cref="RichCanvas"/> that are selected.
        /// </summary>
        public IList? SelectedItems
        {
            get => (IList?)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        /// <summary>
        /// Occurs whenever drawing operation finishes.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent (EventManager.RegisterRoutedEvent). Now a CLR event.
        /// The event args source is DrawEndedEventArgs (unchanged).
        /// </remarks>
        public event EventHandler<DrawEndedEventArgs>? DrawingEnded;

        /// <summary>
        /// Identifies the <see cref="DrawingEndedCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DrawingEndedCommandProperty = DependencyProperty.Register(
            nameof(DrawingEndedCommand), typeof(ICommand), typeof(RichCanvas),
            new PropertyMetadata(null));

        /// <summary>
        /// Invoked when drawing operation is completed. <br />
        /// Parameter is <see cref="Point"/>, representing the mouse position when drawing has finished.
        /// </summary>
        public ICommand? DrawingEndedCommand
        {
            get => (ICommand?)GetValue(DrawingEndedCommandProperty);
            set => SetValue(DrawingEndedCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDragging"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
            nameof(IsDragging), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets whether the operation in progress is dragging.
        /// </summary>
        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            internal set => SetValue(IsDraggingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RealTimeSelectionEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RealTimeSelectionEnabledProperty = DependencyProperty.Register(
            nameof(RealTimeSelectionEnabled), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether real-time selection is enabled.
        /// Default is <see langword="false"/>.
        /// </summary>
        public bool RealTimeSelectionEnabled
        {
            get => (bool)GetValue(RealTimeSelectionEnabledProperty);
            set => SetValue(RealTimeSelectionEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RealTimeDraggingEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RealTimeDraggingEnabledProperty = DependencyProperty.Register(
            nameof(RealTimeDraggingEnabled), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether real-time dragging is enabled.
        /// Default is <see langword="false"/>.
        /// </summary>
        public bool RealTimeDraggingEnabled
        {
            get => (bool)GetValue(RealTimeDraggingEnabledProperty);
            set => SetValue(RealTimeDraggingEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CanSelectMultipleItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanSelectMultipleItemsProperty = DependencyProperty.Register(
            nameof(CanSelectMultipleItems), typeof(bool), typeof(RichCanvas),
            new PropertyMetadata(true, OnCanSelectMultipleItemsChanged));

        /// <summary>
        /// Gets or sets whether you can select multiple elements or not.
        /// Default is <see langword="true"/>.
        /// </summary>
        public bool CanSelectMultipleItems
        {
            get => (bool)GetValue(CanSelectMultipleItemsProperty);
            set => SetValue(CanSelectMultipleItemsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ViewportSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewportSizeProperty = DependencyProperty.Register(
            nameof(ViewportSize), typeof(Size), typeof(RichCanvas),
            new PropertyMetadata(Size.Empty));

        /// <summary>
        /// Gets the size of the viewport.
        /// </summary>
        public Size ViewportSize
        {
            get => (Size)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemsExtent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsExtentProperty = DependencyProperty.Register(
            nameof(ItemsExtent), typeof(Rect), typeof(RichCanvas),
            new PropertyMetadata(Rect.Empty, OnItemsExtentChanged));

        /// <summary>
        /// The area covered by the <see cref="RichCanvasContainer"/>s present on <see cref="RichCanvas"/>.
        /// </summary>
        public Rect ItemsExtent
        {
            get => (Rect)GetValue(ItemsExtentProperty);
            set => SetValue(ItemsExtentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(RichCanvas),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the currently selected item (for single selection mode).
        /// </summary>
        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Occurs when the selection changes.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] WPF's Selector base class provided this event.
        /// Now it's a standard CLR event raised manually when items are added/removed from selection.
        /// </remarks>
        public event SelectionChangedEventHandler? SelectionChanged;

        #endregion Properties API

        #region Internal Properties

        internal RichCanvasPanel ItemsHost => _mainPanel ?? throw new InvalidOperationException("RichCanvasPanel has not been initialized yet. Ensure the control template has been applied.");
        internal bool IsZooming { get; set; }

        /// <summary>
        /// Internal list of selected items, replaces WPF's MultiSelector.SelectedItems (base.SelectedItems).
        /// </summary>
        internal IList InternalSelectedItems => _internalSelectedItems;

        internal List<int> CurrentDrawingIndexes { get; } = new List<int>();

        /// <summary>
        /// Stores the last known pointer position relative to the control.
        /// Used by PanningState as a replacement for Mouse.GetPosition(Parent).
        /// </summary>
        internal Point LastPointerPosition { get; private set; }

        #endregion Internal Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="RichCanvas"/>
        /// </summary>
        public RichCanvas()
        {
            DefaultStyleKey = typeof(RichCanvas);

            AppliedTransform = new TransformGroup()
            {
                Children = { ScaleTransform, TranslateTransform }
            };

            _states = new Stack<CanvasState>();
            _states.Push(GetDefaultState());

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #endregion Constructors

        #region Override Methods

        /// <summary>
        /// Used to returns the implementation of a <see cref="CanvasState"/> used to orchestrate interactions between all defined states.
        /// <br/>
        /// Note: <i>This state is always present on the states stack.</i>
        /// </summary>
        /// <returns>A new <see cref="CanvasState"/></returns>
        public virtual CanvasState GetDefaultState() => new DefaultState(this);

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // [WPF Migration] In WPF, the panel used IsItemsHost=True in the template.
            // In WinUI, items are placed into the ItemsPanel (set in the default Style).
            // The panel is found by walking the visual tree from the ItemsPresenter.
            // We defer finding the panel until after layout, as the ItemsPresenter
            // may not have created its child panel yet.
            FindItemsPanel();
        }

        private void FindItemsPanel()
        {
            // Try to find the RichCanvasPanel that was created by the ItemsPanel template
            if (_mainPanel != null) return;

            // The panel might not be available immediately after OnApplyTemplate.
            // Use DispatcherQueue to defer until after the layout pass.
            DispatcherQueue.TryEnqueue(() =>
            {
                _mainPanel = FindDescendant<RichCanvasPanel>(this);
                if (_mainPanel != null)
                {
                    _mainPanel.ItemsOwner = this;

                    // Set up the Extent property synchronization
                    // [WPF Migration] The WPF version used OneWayToSource binding on Extent.
                    // WinUI doesn't support OneWayToSource. We register a callback on the panel's Extent property.
                    _mainPanel.RegisterPropertyChangedCallback(RichCanvasPanel.ExtentProperty, OnPanelExtentChanged);
                }
            });
        }

        private void OnPanelExtentChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is RichCanvasPanel panel)
            {
                ItemsExtent = panel.Extent;
            }
        }

        private static T? FindDescendant<T>(DependencyObject parent) where T : class
        {
            int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;

                T? result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <inheritdoc/>
        protected override AutomationPeer OnCreateAutomationPeer()
            => new RichCanvasAutomationPeer(this);

        /// <inheritdoc/>
        protected override bool IsItemItsOwnContainerOverride(object item) => item is RichCanvasContainer;

        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride() => new RichCanvasContainer
        {
            RenderTransform = new TransformGroup
            {
                Children = { new ScaleTransform(), new TranslateTransform() }
            }
        };

        /// <inheritdoc/>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is RichCanvasContainer container)
            {
                container.Host = this;
            }
        }

        /// <inheritdoc/>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            // Container host is cleared automatically when removed from visual tree
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            bool hasCapturedPointer = _capturedPointerId != null;

            if (!hasCapturedPointer && e.HasAnyButtonPressed())
            {
                // Check "preview" state first (replaces OnPreviewMouseDown)
                if (CurrentState.MatchesPreviewPointerPressedState(e, out CanvasState? matchingState))
                {
                    if (CapturePointer(e.Pointer))
                    {
                        _capturedPointerId = e.Pointer.PointerId;
                        PushState(matchingState!);
                        e.Handled = true;
                        return;
                    }
                }

                // Normal pointer pressed handling (replaces OnMouseDown)
                if (CapturePointer(e.Pointer))
                {
                    _capturedPointerId = e.Pointer.PointerId;
                    CurrentState.HandlePointerPressed(e);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            if (_mainPanel != null)
            {
                MousePosition = e.GetCurrentPoint(_mainPanel).Position;
            }
            LastPointerPosition = e.GetCurrentPoint(this).Position;
            if (_capturedPointerId == e.Pointer.PointerId)
            {
                CurrentState.HandlePointerMoved(e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            if (_capturedPointerId == e.Pointer.PointerId)
            {
                CurrentState.HandlePointerReleased(e);
                PopState();
                if (e.HasAllButtonsReleased())
                {
                    ReleasePointerCapture(e.Pointer);
                    _capturedPointerId = null;
                }
            }
            Focus(FocusState.Programmatic);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            // Handle zoom commands
            if (RichCanvasGestures.ZoomIn.Matches(this, e))
            {
                ZoomIn();
                e.Handled = true;
                return;
            }
            if (RichCanvasGestures.ZoomOut.Matches(this, e))
            {
                ZoomOut();
                e.Handled = true;
                return;
            }

            CurrentState.HandleKeyDown(e);
        }

        /// <inheritdoc/>
        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            CurrentState.HandleKeyUp(e);
            PopState();
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(object e)
        {
            // [WPF Migration] WPF's OnItemsChanged provided NotifyCollectionChangedEventArgs directly.
            // In WinUI, OnItemsChanged receives the event args from the Items collection.
            // We subscribe to the Items.VectorChanged event separately for detailed change tracking.
            // The base implementation handles the visual tree updates.
            base.OnItemsChanged(e);
        }

        /// <summary>
        /// Handles the size changed event to update viewport size and scrollbars.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Replaces OnRenderSizeChanged. In WinUI, use the SizeChanged event.
        /// </remarks>
        private void OnSizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            ViewportSize = new Size(ActualWidth / ViewportZoom, ActualHeight / ViewportZoom);
            UpdateScrollbars();
        }

        #endregion Override Methods

        #region Public Api

        /// <summary>
        /// Get or set whether panning is currently in progress.
        /// </summary>
        public bool IsPanning { get; internal set; }

        /// <summary>Pushes a new state into the stack.</summary>
        /// <param name="state">The new state.</param>
        public void PushState(CanvasState state)
        {
            _states.Push(state);
            state.Enter();
        }

        /// <summary>Pops the current state from the stack without removing the default one defined by <see cref="GetDefaultState()"/> method.</summary>
        public void PopState()
        {
            // Never remove the default state
            if (_states.Count > 1)
            {
                CanvasState prev = _states.Pop();
                prev.Exit();
                CurrentState.ReEnter();
            }
        }

        #endregion Public Api

        #region Properties Callbacks

        private static void OnEnableAutoPanningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((RichCanvas)d).OnEnableAutoPanningChanged((bool)e.NewValue);

        private static void OnAutoPanTickRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RichCanvas)d).UpdateTimerInterval();

        private static void OnScrollFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // [WPF Migration] CoerceValueCallback replaced with property changed callback coercion.
            var canvas = (RichCanvas)d;
            if ((double)e.NewValue == 0)
            {
                canvas.ScrollFactor = 10d;
            }
        }

        private static void OnCanSelectMultipleItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RichCanvas)d).CanSelectMultipleItemsUpdated((bool)e.NewValue);

        private static void OnItemsExtentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (RichCanvas)d;
            editor.UpdateScrollbars();
        }

        #endregion Properties Callbacks

        #region Selection

        /// <summary>
        /// Unselects all currently selected items.
        /// </summary>
        public void UnselectAll()
        {
            _isUpdatingSelection = true;
            var removed = new List<object>(_internalSelectedItems);
            _internalSelectedItems.Clear();

            // Unselect containers
            for (int i = 0; i < removed.Count; i++)
            {
                var container = ContainerFromItem(removed[i]);
                if (container != null)
                {
                    container.IsSelected = false;
                }
            }

            _isUpdatingSelection = false;

            if (removed.Count > 0)
            {
                RaiseSelectionChanged(new List<object>(), removed);
            }
        }

        internal void BeginSelectionTransaction()
        {
            _isUpdatingSelection = true;
        }

        internal void EndSelectionTransaction()
        {
            _isUpdatingSelection = false;
            // Sync external SelectedItems with internal list
            SyncSelectedItems();
        }

        /// <summary>
        /// Returns the elements that intersect with <paramref name="area"/>
        /// </summary>
        /// <remarks>
        /// [WPF Migration] WPF used VisualTreeHelper.HitTest with GeometryHitTestParameters.
        /// Replaced with bounds-intersection testing via HitTestHelper.
        /// </remarks>
        public List<object> GetElementsInArea(Rect area)
        {
            var intersectedElements = new List<object>();
            if (_mainPanel == null) return intersectedElements;
            var containers = HitTestHelper.FindContainersInArea(_mainPanel, area);

            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i].DataContext != null)
                {
                    intersectedElements.Add(containers[i].DataContext);
                }
            }

            return intersectedElements;
        }

        private static void OnSelectedItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((RichCanvas)d).OnSelectedItemsSourceChanged((IList?)e.OldValue, (IList?)e.NewValue);

        private void OnSelectedItemsSourceChanged(IList? oldValue, IList? newValue)
        {
            if (oldValue is INotifyCollectionChanged oc)
            {
                oc.CollectionChanged -= OnSelectedItemsChanged;
            }

            if (newValue is INotifyCollectionChanged nc)
            {
                nc.CollectionChanged += OnSelectedItemsChanged;
            }

            if (CanSelectMultipleItems)
            {
                _isUpdatingSelection = true;
                _internalSelectedItems.Clear();
                if (newValue != null)
                {
                    for (int i = 0; i < newValue.Count; i++)
                    {
                        if (newValue[i] != null)
                            _internalSelectedItems.Add(newValue[i]!);
                    }
                }
                _isUpdatingSelection = false;
            }
        }

        private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (CanSelectMultipleItems)
                    {
                        _internalSelectedItems.Clear();
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                    if (CanSelectMultipleItems)
                    {
                        IList? newItems = e.NewItems;
                        if (newItems != null)
                        {
                            for (int i = 0; i < newItems.Count; i++)
                            {
                                if (newItems[i] != null)
                                    _internalSelectedItems.Add(newItems[i]!);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (CanSelectMultipleItems)
                    {
                        IList? oldItems = e.OldItems;
                        if (oldItems != null)
                        {
                            for (int i = 0; i < oldItems.Count; i++)
                            {
                                if (oldItems[i] != null)
                                    _internalSelectedItems.Remove(oldItems[i]!);
                            }
                        }
                    }
                    break;
            }
        }

        internal void UpdateSingleSelectedItem(RichCanvasContainer selectedContainer)
        {
            if (SelectedItem == null)
            {
                selectedContainer.IsSelected = true;
            }
            else
            {
                SelectedItem = null;
                selectedContainer.IsSelected = true;
            }
        }

        private void SyncSelectedItems()
        {
            IList? selected = SelectedItems;
            if (selected != null && CanSelectMultipleItems)
            {
                // Add items in internal list that are not in the external list
                for (int i = 0; i < _internalSelectedItems.Count; i++)
                {
                    if (!selected.Contains(_internalSelectedItems[i]))
                    {
                        selected.Add(_internalSelectedItems[i]);
                    }
                }

                // Remove items in external list that are not in the internal list
                var toRemove = new List<object>();
                for (int i = 0; i < selected.Count; i++)
                {
                    if (!_internalSelectedItems.Contains(selected[i]!))
                    {
                        toRemove.Add(selected[i]!);
                    }
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    selected.Remove(toRemove[i]);
                }
            }
        }

        private void RaiseSelectionChanged(List<object> addedItems, List<object> removedItems)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(removedItems, addedItems));
        }

        #endregion Selection

        #region Container Lookup

        /// <summary>
        /// Gets the <see cref="RichCanvasContainer"/> for the specified item.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Replaces ItemContainerGenerator.ContainerFromItem.
        /// In WinUI, ItemsControl.ContainerFromItem is a direct method on the control.
        /// Using 'new' to return the strongly-typed RichCanvasContainer.
        /// </remarks>
        public new RichCanvasContainer? ContainerFromItem(object item)
        {
            return base.ContainerFromItem(item) as RichCanvasContainer;
        }

        /// <summary>
        /// Gets the <see cref="RichCanvasContainer"/> at the specified index.
        /// </summary>
        public new RichCanvasContainer? ContainerFromIndex(int index)
        {
            return base.ContainerFromIndex(index) as RichCanvasContainer;
        }

        #endregion Container Lookup

        #region Items Changed Tracking

        /// <summary>
        /// Subscribes to items source collection changed for tracking drawing indexes.
        /// Called when the control is loaded.
        /// </summary>
        private void SubscribeToItemsSourceChanges()
        {
            if (ItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            SizeChanged += OnSizeChangedHandler;
        }

        private void UnsubscribeFromItemsSourceChanges()
        {
            if (ItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            SizeChanged -= OnSizeChangedHandler;
        }

        private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                CurrentDrawingIndexes.Clear();
                if (CanSelectMultipleItems)
                {
                    _internalSelectedItems.Clear();
                    SelectedItems?.Clear();
                }
                else
                {
                    SelectedItem = null;
                }
            }
            else if (e.NewStartingIndex != -1 && e.Action == NotifyCollectionChangedAction.Add)
            {
                // Defer the container check since the container may not be created yet
                DispatcherQueue.TryEnqueue(() =>
                {
                    var container = ContainerFromIndex(e.NewStartingIndex);
                    if (container != null && !container.IsValid())
                    {
                        CurrentDrawingIndexes.Add(e.NewStartingIndex);
                    }
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                CurrentDrawingIndexes.Remove(e.OldStartingIndex);
                for (int i = e.OldStartingIndex; i < CurrentDrawingIndexes.Count; i++)
                {
                    CurrentDrawingIndexes[i]--;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                if (e.OldStartingIndex < CurrentDrawingIndexes.Count)
                {
                    int oldValue = CurrentDrawingIndexes[e.OldStartingIndex];
                    CurrentDrawingIndexes.Remove(oldValue);
                    CurrentDrawingIndexes.Insert(e.NewStartingIndex, oldValue);
                }
            }
        }

        #endregion Items Changed Tracking

        #region Handlers And Private Methods

        private void CanSelectMultipleItemsUpdated(bool value)
        {
            if (value)
            {
                if (SelectedItem != null)
                {
                    SelectedItem = null;
                }
            }
            else
            {
                if (SelectedItems?.Count > 1)
                {
                    SelectedItems?.Clear();
                    SelectedItem = null;
                }
                else if (SelectedItems?.Count == 1)
                {
                    SelectedItem = SelectedItems?[0];
                }
            }
        }

        private static void OnViewportLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (RichCanvas)d;
            var translate = (Point)e.NewValue;

            host.TranslateTransform.X = -translate.X * host.ViewportZoom;
            host.TranslateTransform.Y = -translate.Y * host.ViewportZoom;

            host.UpdateScrollbars();
        }

        private void OnEnableAutoPanningChanged(bool enableAutoPanning)
        {
            if (enableAutoPanning)
            {
                if (_autoPanTimer == null)
                {
                    // [WPF Migration] WPF DispatcherTimer accepted DispatcherPriority parameter.
                    // WinUI DispatcherTimer does not have priority. Interval is set directly.
                    _autoPanTimer = new DispatcherTimer();
                    _autoPanTimer.Interval = TimeSpan.FromMilliseconds(AutoPanTickRate);
                    _autoPanTimer.Tick += HandleAutoPanning;
                    _autoPanTimer.Start();
                }
                else
                {
                    _autoPanTimer.Interval = TimeSpan.FromMilliseconds(AutoPanTickRate);
                    _autoPanTimer.Start();
                }
            }
            else
            {
                _autoPanTimer?.Stop();
            }
        }

        private void HandleAutoPanning(object? sender, object e)
        {
            // [WPF Migration] WPF used Mouse.LeftButton, Mouse.Captured, IsMouseOver static properties.
            // In WinUI, we track pointer capture state and last pointer position instead.
            if (_capturedPointerId != null && !IsPanning)
            {
                Point mousePosition = LastPointerPosition;
                double x = ViewportLocation.X;
                double y = ViewportLocation.Y;

                if (mousePosition.Y <= 0)
                {
                    y -= AutoPanSpeed;
                }
                else if (mousePosition.Y >= ViewportHeight)
                {
                    y += AutoPanSpeed;
                }

                if (mousePosition.X <= 0)
                {
                    x -= AutoPanSpeed;
                }
                else if (mousePosition.X >= ViewportWidth)
                {
                    x += AutoPanSpeed;
                }

                ViewportLocation = new Point(x, y);

                CurrentState.HandleAutoPanning(null);
            }
        }

        private void UpdateTimerInterval()
        {
            if (_autoPanTimer != null)
            {
                _autoPanTimer.Interval = TimeSpan.FromMilliseconds(AutoPanTickRate);
            }
        }

        internal void RaiseDrawEndedEvent(object context, Point mousePosition)
        {
            DrawingEnded?.Invoke(this, new DrawEndedEventArgs(context, mousePosition));
        }

        #endregion Handlers And Private Methods

        #region Lifecycle

        /// <summary>
        /// Called when the control is loaded into the visual tree.
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeToItemsSourceChanges();
        }

        /// <summary>
        /// Called when the control is unloaded from the visual tree.
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromItemsSourceChanges();
            _autoPanTimer?.Stop();
        }

        #endregion Lifecycle
    }
}
