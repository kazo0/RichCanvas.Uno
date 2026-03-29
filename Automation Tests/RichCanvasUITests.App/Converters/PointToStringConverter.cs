using System;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace RichCanvasUITests.App.Converters
{
    public class PointToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var point = (Point)value;
            return $"{point.X}, {point.Y}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
