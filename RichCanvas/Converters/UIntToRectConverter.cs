using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;

namespace RichCanvas.Converters
{
    /// <summary>
    /// Represents the converter that converts <see cref="uint"/> values to a <see cref="Rect"/>.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Converter signature changed: CultureInfo parameter replaced with string language parameter.
    /// The original WPF version was used with DrawingBrush for grid patterns.
    /// DrawingBrush is not available in WinUI/Uno; this converter is retained for potential custom grid implementations.
    /// </remarks>
    public class UIntToRectConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            uint size = System.Convert.ToUInt32(value);
            return new Rect(0, 0, size, size);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
