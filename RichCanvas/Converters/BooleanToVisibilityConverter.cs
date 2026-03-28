using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RichCanvas.Converters
{
    /// <summary>
    /// Converts a boolean value to a <see cref="Visibility"/> value.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] WPF has a built-in BooleanToVisibilityConverter. WinUI does not, so we provide our own.
    /// </remarks>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
            {
                return v == Visibility.Visible;
            }
            return false;
        }
    }
}
