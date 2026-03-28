using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using RichCanvas.CustomEventArgs;
using RichCanvas.Helpers;
using RichCanvas.States.ContainerStates;

namespace RichCanvas
{
    /// <summary>
    /// Delegate used to notify when an <see cref="RichCanvasContainer"/> is dragged.
    /// </summary>
    /// <param name="newLocation">The new location.</param>
    public delegate void PreviewLocationChanged(Point newLocation);

    /// <summary>
    /// <see cref="RichCanvas"/> items container.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Key changes:
    /// - RoutedEvents (Selected, Unselected, TopChanged, LeftChanged, DragStarted, DragDelta, DragCompleted)
    ///   replaced with standard CLR events since WinUI does not support EventManager.RegisterRoutedEvent.
    /// - Selector.IsSelectedProperty.AddOwner replaced with a standard dependency property.
    /// - Selector.SelectedEvent/UnselectedEvent.AddOwner replaced with CLR events.
    /// - DependencyPropertyKey (read-only DPs) not available; using regular DPs with internal setters.
    /// - FrameworkPropertyMetadata replaced with PropertyMetadata.
    /// - FrameworkPropertyMetadataOptions.BindsTwoWayByDefault simulated by setting DefaultBindingMode on the DP registration.
    /// - Mouse events replaced with Pointer events.
    /// - Mouse.Captured/CaptureMouse/ReleaseMouseCapture replaced with pointer capture APIs.
    /// </remarks>
    [TemplatePart(Name = ContentPresenterName, Type = typeof(ContentPresenter))]
    public class RichCanvasContainer : ContentControl
    {
        private const string ContentPresenterName = "PART_ContentPresenter";
        private Stack<ContainerState> _states;

        // Pointer capture tracking
        private uint? _capturedPointerId;

        /// <summary>
        /// Default fallback value for container width used on drawing if the set value is 0.
        /// </summary>
        public const double DefaultWidth = 1d;

        /// <summary>
        /// Default fallback value for container height used on drawing if the set value is 0.
        /// </summary>
        public const double DefaultHeight = 1d;

        internal ScaleTransform? ScaleTransform => RenderTransform is TransformGroup group ? group.Children.OfType<ScaleTransform>().FirstOrDefault() : null;
        internal TranslateTransform? TranslateTransform => RenderTransform is TransformGroup group ? group.Children.OfType<TranslateTransform>().FirstOrDefault() : null;

        #region Properties API

        /// <summary>
        /// Identifies the <see cref="IsSelected"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(false, OnIsSelectedChanged));

        /// <summary>
        /// Gets or sets a value that indicates whether this item is selected.
        /// Can only be set if <see cref="IsSelectable"/> is true.
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Top"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TopProperty = DependencyProperty.Register(
            nameof(Top), typeof(double), typeof(RichCanvasContainer),
            new PropertyMetadata(0.0, OnPositionChanged));

        /// <summary>
        /// Gets or sets the Top position of this <see cref="RichCanvasContainer"/> on <see cref="RichCanvas.ItemsHost"/>
        /// </summary>
        public double Top
        {
            get => (double)GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Left"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftProperty = DependencyProperty.Register(
            nameof(Left), typeof(double), typeof(RichCanvasContainer),
            new PropertyMetadata(0.0, OnPositionChanged));

        /// <summary>
        /// Gets or sets the Left position of this <see cref="RichCanvasContainer"/> on <see cref="RichCanvas.ItemsHost"/>
        /// </summary>
        public double Left
        {
            get => (double)GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectableProperty = DependencyProperty.Register(
            nameof(IsSelectable), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether this <see cref="RichCanvasContainer"/> can be selected.
        /// True by default
        /// </summary>
        public bool IsSelectable
        {
            get => (bool)GetValue(IsSelectableProperty);
            set => SetValue(IsSelectableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDraggable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.Register(
            nameof(IsDraggable), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether this <see cref="RichCanvasContainer"/> can be dragged on <see cref="RichCanvas.ItemsHost"/>
        /// True by default
        /// </summary>
        public bool IsDraggable
        {
            get => (bool)GetValue(IsDraggableProperty);
            set => SetValue(IsDraggableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HasCustomBehavior"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasCustomBehaviorProperty = DependencyProperty.Register(
            nameof(HasCustomBehavior), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether this <see cref="RichCanvasContainer"/> has custom behavior handled out of dragging.
        /// This tells <see cref="RichCanvas"/> to stop handling mouse interaction when manipulating this <see cref="RichCanvasContainer"/>
        /// </summary>
        public bool HasCustomBehavior
        {
            get => (bool)GetValue(HasCustomBehaviorProperty);
            set => SetValue(HasCustomBehaviorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShouldBringIntoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShouldBringIntoViewProperty = DependencyProperty.Register(
            nameof(ShouldBringIntoView), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(false, OnBringIntoViewChanged));

        /// <summary>
        /// Gets or sets whether this <see cref="RichCanvasContainer"/> should be centered inside <see cref="RichCanvas"/> viewport.
        /// </summary>
        public bool ShouldBringIntoView
        {
            get => (bool)GetValue(ShouldBringIntoViewProperty);
            set => SetValue(ShouldBringIntoViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Scale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale), typeof(Point), typeof(RichCanvasContainer),
            new PropertyMetadata(new Point(1, 1), OnScaleChanged));

        /// <summary>
        /// Gets or sets this <see cref="RichCanvasContainer"/> ScaleTransform in order to get direction.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] This property hides UIElement.Scale (a Vector3 in WinUI). Use the 'new' keyword.
        /// The WPF original used Point; the WinUI UIElement.Scale is a System.Numerics.Vector3.
        /// </remarks>
        public new Point Scale
        {
            get => (Point)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowScaleChangeToUpdatePosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowScaleChangeToUpdatePositionProperty = DependencyProperty.Register(
            nameof(AllowScaleChangeToUpdatePosition), typeof(bool), typeof(RichCanvasContainer),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether this <see cref="RichCanvasContainer"/> Left and Top can be updated while Drawing if the <see cref="Scale"/> is changed.
        /// </summary>
        public bool AllowScaleChangeToUpdatePosition
        {
            get => (bool)GetValue(AllowScaleChangeToUpdatePositionProperty);
            set => SetValue(AllowScaleChangeToUpdatePositionProperty, value);
        }

        /// <summary>
        /// Apply transforms on <see cref="RichCanvasContainer"/>
        /// </summary>
        public static readonly DependencyProperty ApplyTransformProperty = DependencyProperty.RegisterAttached(
            "ApplyTransform", typeof(Transform), typeof(RichCanvasContainer),
            new PropertyMetadata(default(Transform), OnApplyTransformChanged));

        /// <summary>
        /// Sets a property value that tells what <see cref="Transform"/> should be applied on <see cref="RichCanvasContainer"/>.RenderTransform property.
        /// </summary>
        public static void SetApplyTransform(UIElement element, Transform value) => element.SetValue(ApplyTransformProperty, value);

        /// <summary>
        /// Gets the <see cref="RichCanvasContainer"/>.ApplyTransform attached property value.
        /// </summary>
        public static Transform GetApplyTransform(UIElement element) => (Transform)element.GetValue(ApplyTransformProperty);

        /// <summary>
        /// Occurs whenever this <see cref="RichCanvasContainer"/> is selected.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent (Selector.SelectedEvent.AddOwner). Now a CLR event.
        /// </remarks>
        public event EventHandler? Selected;

        /// <summary>
        /// Occurs when this <see cref="RichCanvasContainer"/> is unselected.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent (Selector.UnselectedEvent.AddOwner). Now a CLR event.
        /// </remarks>
        public event EventHandler? Unselected;

        /// <summary>
        /// Occurs whenever <see cref="Top"/> changes.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent (EventManager.RegisterRoutedEvent). Now a CLR event.
        /// </remarks>
        public event EventHandler? TopChanged;

        /// <summary>
        /// Occurs whenever <see cref="Left"/> changes.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent. Now a CLR event.
        /// </remarks>
        public event EventHandler? LeftChanged;

        /// <summary>
        /// Occurs when this <see cref="RichCanvasContainer"/> is the instigator of a drag operation.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent with DragStartedEventHandler. Now a CLR event with ContainerDragStartedEventArgs.
        /// </remarks>
        public event EventHandler<ContainerDragStartedEventArgs>? DragStarted;

        /// <summary>
        /// Occurs when this <see cref="RichCanvasContainer"/> is being dragged.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent with DragDeltaEventHandler. Now a CLR event with ContainerDragDeltaEventArgs.
        /// </remarks>
        public event EventHandler<ContainerDragDeltaEventArgs>? DragDelta;

        /// <summary>
        /// Occurs when this <see cref="RichCanvasContainer"/> completed the drag operation.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] Was a RoutedEvent with DragCompletedEventHandler. Now a CLR event with ContainerDragCompletedEventArgs.
        /// </remarks>
        public event EventHandler<ContainerDragCompletedEventArgs>? DragCompleted;

        #endregion Properties API

        static RichCanvasContainer()
        {
            // [WPF Migration] DefaultStyleKeyProperty.OverrideMetadata replaced with setting DefaultStyleKey in constructor.
        }

        /// <summary>
        /// Gets this <see cref="RichCanvasContainer"/> TransformBounds.
        /// </summary>
        public Rect BoundingBox { get; private set; }

        /// <summary>
        /// Current state of <see cref="RichCanvasContainer"/>.
        /// </summary>
        public ContainerState CurrentState => _states.Peek();

        private RichCanvas? _host;

        /// <summary>
        /// The <see cref="RichCanvas"/> that owns this <see cref="RichCanvasContainer"/>.
        /// </summary>
        /// <remarks>
        /// [WPF Migration] WPF used ItemsControl.ItemsControlFromItemContainer(this).
        /// In WinUI/Uno, this is set explicitly by the RichCanvas when preparing the container.
        /// </remarks>
        public RichCanvas Host
        {
            get => _host ?? throw new System.InvalidOperationException("Container has not been associated with a RichCanvas host.");
            internal set => _host = value;
        }

        internal bool TopPropertyInitalized { get; private set; }
        internal bool LeftPropertyInitialized { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RichCanvasContainer"/> class.
        /// </summary>
        public RichCanvasContainer()
        {
            DefaultStyleKey = typeof(RichCanvasContainer);

            _states = new Stack<ContainerState>();
            _states.Push(GetDefaultState());
        }

        /// <summary>
        /// Calculates <see cref="RichCanvasContainer"/> bounding box based on applied transforms.
        /// </summary>
        public void CalculateBoundingBox()
        {
            if (_host == null) return;

            GeneralTransform transform = TransformToVisual(Host.ItemsHost);
            if (double.IsNaN(Width) || double.IsNaN(Height))
            {
                Rect actualBounds = transform.TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
                BoundingBox = actualBounds;
                return;
            }
            Rect bounds = transform.TransformBounds(new Rect(0, 0, Width, Height));
            BoundingBox = bounds;
        }

        /// <summary>
        /// Used to returns the implementation of a <see cref="ContainerState"/> used to orchestrate interactions between all defined states.
        /// <br/>
        /// Note: <i>This state is always present on the states stack.</i>
        /// </summary>
        /// <returns>A new <see cref="ContainerState"/></returns>
        protected virtual ContainerState GetDefaultState() => new ContainerDefaultState(this);

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            Focus(FocusState.Programmatic);
            bool hasCapturedPointer = _capturedPointerId != null;
            if (!hasCapturedPointer)
            {
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
                if (e.HasAllButtonsReleased())
                {
                    CurrentState.HandlePointerReleased(e);
                    PopState();
                    ReleasePointerCapture(e.Pointer);
                    _capturedPointerId = null;
                }
            }
        }

        /// <summary>
        /// Occurs when the <see cref="RichCanvasContainer"/> is being dragged.
        /// </summary>
        public event PreviewLocationChanged? PreviewLocationChanged;

        /// <summary>
        /// Raises the <see cref="PreviewLocationChanged"/> event.
        /// </summary>
        /// <param name="location">The new location.</param>
        protected internal void OnPreviewLocationChanged(Point location)
        {
            PreviewLocationChanged?.Invoke(location);
        }

        /// <summary>Pushes a new state into the stack.</summary>
        /// <param name="state">The new state.</param>
        public void PushState(ContainerState state)
        {
            _states.Push(state);
            state.Enter();
        }

        /// <summary>Pops the current state from the stack without removing the default one.</summary>
        public void PopState()
        {
            // Never remove the default state
            if (_states.Count > 1)
            {
                ContainerState prev = _states.Pop();
                prev.Exit();
                CurrentState.ReEnter();
            }
        }

        internal bool IsValid()
        {
            return (Height != 0 || ActualHeight != 0) && (Width != 0 || ActualWidth != 0)
                && (!double.IsNaN(Height) || !double.IsNaN(ActualHeight)) && (!double.IsNaN(Width) || !double.IsNaN(ActualWidth));
        }

        internal void RaiseDragStartedEvent(Point position)
        {
            DragStarted?.Invoke(this, new ContainerDragStartedEventArgs(position.X, position.Y));
        }

        internal void RaiseDragDeltaEvent(Point position)
        {
            DragDelta?.Invoke(this, new ContainerDragDeltaEventArgs(position.X, position.Y));
        }

        internal void RaiseDragCompletedEvent(Point position)
        {
            DragCompleted?.Invoke(this, new ContainerDragCompletedEventArgs(position.X, position.Y));
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RichCanvasContainer)d).OverrideScale((Point)e.NewValue);

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RichCanvasContainer)d).UpdatePosition(e.Property);

        private static void OnApplyTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichCanvasContainer container = VisualHelper.GetParentContainer(d);
            if (container != null)
            {
                container.ApplyTransformValue((Transform)e.NewValue);
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var elem = (RichCanvasContainer)d;
            bool result = elem.IsSelectable && (bool)e.NewValue;
            elem.OnSelectedChanged(result);
            if (result != (bool)e.NewValue)
            {
                elem.IsSelected = result;
            }
        }

        private static void OnBringIntoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                // [WPF Migration] WPF's BringIntoView() is not available.
                // In WinUI, use StartBringIntoView() instead.
                ((RichCanvasContainer)d).StartBringIntoView();
            }
        }

        private void UpdatePosition(DependencyProperty prop)
        {
            if (prop == TopProperty && !TopPropertyInitalized)
            {
                TopPropertyInitalized = true;
            }
            if (prop == LeftProperty && !LeftPropertyInitialized)
            {
                LeftPropertyInitialized = true;
            }
            TopChanged?.Invoke(this, EventArgs.Empty);
            LeftChanged?.Invoke(this, EventArgs.Empty);
            if (_host != null)
            {
                Host.ItemsHost.InvalidateArrange();
            }
        }

        private void OnSelectedChanged(bool value)
        {
            if (_host == null) return;
            // Raise event after the selection operation ended
            if (!Host.IsSelecting || Host.RealTimeSelectionEnabled)
            {
                if (value)
                {
                    Selected?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Unselected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ApplyTransformValue(Transform apply)
        {
            // [WPF Migration] WPF used Transform.Clone(). In WinUI, transforms are not Freezable
            // and don't have Clone(). We create a new transform based on the values.
            // For a general Transform, we copy the matrix.
            if (apply is MatrixTransform mt)
            {
                RenderTransform = new MatrixTransform { Matrix = mt.Matrix };
            }
            else if (apply is TransformGroup tg)
            {
                var newGroup = new TransformGroup();
                foreach (var t in tg.Children)
                {
                    newGroup.Children.Add(CopyTransform(t));
                }
                RenderTransform = newGroup;
            }
            else
            {
                RenderTransform = CopyTransform(apply);
            }

            if (IsValid() && _host != null)
            {
                Host.ItemsHost.InvalidateArrange();
            }
        }

        private static Transform CopyTransform(Transform t)
        {
            if (t is ScaleTransform st)
                return new ScaleTransform { ScaleX = st.ScaleX, ScaleY = st.ScaleY, CenterX = st.CenterX, CenterY = st.CenterY };
            if (t is TranslateTransform tt)
                return new TranslateTransform { X = tt.X, Y = tt.Y };
            if (t is RotateTransform rt)
                return new RotateTransform { Angle = rt.Angle, CenterX = rt.CenterX, CenterY = rt.CenterY };
            if (t is SkewTransform skt)
                return new SkewTransform { AngleX = skt.AngleX, AngleY = skt.AngleY, CenterX = skt.CenterX, CenterY = skt.CenterY };
            if (t is MatrixTransform mt)
                return new MatrixTransform { Matrix = mt.Matrix };
            return t;
        }

        private void OverrideScale(Point value)
        {
            if (ScaleTransform != null)
            {
                ScaleTransform.ScaleX = value.X;
                ScaleTransform.ScaleY = value.Y;
            }
        }
    }
}
