using System;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(Object), typeof(String))]
    public class CommandParameterStringFormat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && parameter != null)
                return String.Format(parameter as String, value);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
