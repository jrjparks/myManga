using System;
using System.Windows;
using System.Windows.Data;
using Core;

namespace myManga_App.Resources.Theme.Default
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String Param = parameter as String;
            Double Value = (Double)value;
            Thickness Margin = new Thickness(0);
            if (Param != null)
            {
                if (Param.Equals(String.Empty))
                {
                    Margin = new Thickness(Value);
                }
                else
                {
                    String[] sSides = Param.Replace("x", Value.ToString()).Split(',');
                    Double[] dSides= new Double[sSides.Length];

                    for (int i = 0; i < sSides.Length; ++i)
                        dSides[i] = Parse.TryParse<Double>(sSides[i], 0);

                    switch (sSides.Length)
                    {
                        default: break;

                        case 2:
                            Margin = new Thickness(dSides[0], dSides[1], dSides[0], dSides[1]);
                            break;
                        case 4:
                            Margin = new Thickness(dSides[0], dSides[1], dSides[2], dSides[3]);
                            break;
                    }
                }
            }
            return Margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
