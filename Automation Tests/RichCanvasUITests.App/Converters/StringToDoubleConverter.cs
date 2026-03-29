using System;
using Microsoft.UI.Xaml.Data;

namespace RichCanvasUITests.App.Converters
{
    public class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (double.TryParse(value?.ToString(), out var result))
            {
                return result;
            }
            return 0d;
        }
    }
}
