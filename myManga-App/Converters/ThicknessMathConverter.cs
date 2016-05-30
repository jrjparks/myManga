using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(Thickness), typeof(Thickness))]
    public class ThicknessAdditionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness newThickness = new Thickness(0);
            foreach (Object value in values)
            {
                if (value is Thickness)
                {
                    Thickness currentThickness = (Thickness)value;
                    newThickness.Bottom += currentThickness.Bottom;
                    newThickness.Left += currentThickness.Left;
                    newThickness.Right += currentThickness.Right;
                    newThickness.Top += currentThickness.Top;
                }
                else continue;
            }
            return newThickness;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }
}
