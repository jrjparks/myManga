using System;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool) return !(bool)value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { return Convert(value, targetType, parameter, culture); }
    }
}
