using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RichCanvasUITests.App.Models;

namespace RichCanvasUITests.App
{
    public sealed partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;

        public MainWindow()
        {
            this.InitializeComponent();

            _vm = new MainWindowViewModel();
            if (this.Content is FrameworkElement root)
            {
                root.DataContext = _vm;
            }

            _vm.PropertyChanged += OnViewModelPropertyChanged;

            // Set up the ItemTemplateSelector for RichCanvas since WinUI doesn't support implicit DataTemplates
            source.ItemTemplateSelector = new RichCanvasTemplateSelector();

            // Sync ViewportSize back from the canvas
            source.RegisterPropertyChangedCallback(RichCanvas.RichCanvas.ViewportSizeProperty, (s, e) =>
            {
                _vm.ViewportSize = source.ViewportSize;
            });

            // Update text displays when source properties change
            source.RegisterPropertyChangedCallback(RichCanvas.RichCanvas.MousePositionProperty, (s, e) =>
            {
                MousePositionText.Text = $"{source.MousePosition} mouse position";
            });

            UpdateDisplayTexts();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.SelectedItem):
                    UpdateSelectedItemTexts();
                    break;
                case nameof(MainWindowViewModel.ViewportLocation):
                    ViewportLocationText.Text = $"{_vm.ViewportLocation} viewport location";
                    break;
                case nameof(MainWindowViewModel.ViewportSize):
                    ViewportSizeText.Text = $"{_vm.ViewportSize} viewport size";
                    break;
            }
        }

        private void UpdateSelectedItemTexts()
        {
            if (_vm.SelectedItem != null)
            {
                SelectedItemHeightText.Text = $"{_vm.SelectedItem.Height} height";
                SelectedItemWidthText.Text = $"{_vm.SelectedItem.Width} width";
                SelectedItemTopText.Text = $"{_vm.SelectedItem.Top} top";
                SelectedItemLeftText.Text = $"{_vm.SelectedItem.Left} left";
            }
            else
            {
                SelectedItemHeightText.Text = "";
                SelectedItemWidthText.Text = "";
                SelectedItemTopText.Text = "";
                SelectedItemLeftText.Text = "";
            }
        }

        private void UpdateDisplayTexts()
        {
            ViewportLocationText.Text = $"{_vm.ViewportLocation} viewport location";
            ViewportSizeText.Text = $"{_vm.ViewportSize} viewport size";
            ZoomText.Text = "1 zoom";

            source.RegisterPropertyChangedCallback(RichCanvas.RichCanvas.ViewportZoomProperty, (s, e) =>
            {
                ZoomText.Text = $"{source.ViewportZoom} zoom";
            });
        }
    }

    public class RichCanvasTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is DrawingEndedRepresentation)
            {
                return CreateDrawingEndedTemplate();
            }
            else if (item is Line)
            {
                return CreateLineTemplate();
            }
            else if (item is RichItemContainerModel)
            {
                return CreateRectangleTemplate();
            }
            return base.SelectTemplateCore(item, container);
        }

        private static DataTemplate CreateRectangleTemplate()
        {
            // <Rectangle Fill="Red" />
            var xaml = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Rectangle Fill=""Red"" />
            </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        private static DataTemplate CreateLineTemplate()
        {
            // <Line Stroke="Red" StrokeThickness="3" X2="{Binding Width}" Y2="{Binding Height}" />
            var xaml = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Line Stroke=""Red"" StrokeThickness=""3"" X2=""{Binding Width}"" Y2=""{Binding Height}"" />
            </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        private static DataTemplate CreateDrawingEndedTemplate()
        {
            var xaml = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <TextBlock Text=""DRAWING ENDED"" AutomationProperties.AutomationId=""DrawingEndedTextBox"" />
            </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }
    }
}
