using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(String[]), typeof(String))]
    public class StringArrayToCSV : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ICollection)
                return String.Join(", ", (from val in (value as List<String>) select val.ToString()));
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (from val in (value as String).Split(',') select val.Trim()).ToList();
        }
    }
}
