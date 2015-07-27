using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using myMangaSiteExtension.Objects;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(String[]), typeof(String))]
    public class LocationListToCSV : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ICollection)
                return String.Join(", ", (from val in (value as List<LocationObject>) select val.ExtensionName));
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
