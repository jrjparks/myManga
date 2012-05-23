using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Media;

namespace myManga.UI.Code
{
    /// <summary>
    /// This converter facilitates a couple of requirements around images. Firstly, it automatically disposes of image streams as soon as images
    /// are loaded, thus avoiding file access exceptions when attempting to delete images. Secondly, it allows images to be decoded to specific
    /// widths and / or heights, thus allowing memory to be saved where images will be scaled down from their original size.
    /// </summary>
    public sealed class BitmapFrameConverter : IValueConverter
    {
        //doubles purely to facilitate easy data binding
        private double _decodePixelWidth;
        private double _decodePixelHeight;

        public double DecodePixelWidth
        {
            get
            {
                return _decodePixelWidth;
            }
            set
            {
                _decodePixelWidth = value;
            }
        }

        public double DecodePixelHeight
        {
            get
            {
                return _decodePixelHeight;
            }
            set
            {
                _decodePixelHeight = value;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;

            if (path != null && File.Exists(path))
            {
                //create new stream and create bitmap frame
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
                bitmapImage.DecodePixelWidth = (int)_decodePixelWidth;
                bitmapImage.DecodePixelHeight = (int)_decodePixelHeight;
                //load the image now so we can immediately dispose of the stream
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                //clean up the stream to avoid file access exceptions when attempting to delete images
                bitmapImage.StreamSource.Dispose();

                return bitmapImage;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public sealed class ObjectToString : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as String;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    public class BorderClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[0] is double && values[1] is double && values[2] is CornerRadius)
            {
                var width = (double)values[0];
                var height = (double)values[1];

                if (width < Double.Epsilon || height < Double.Epsilon)
                {
                    return Geometry.Empty;
                }

                var radius = (CornerRadius)values[2];

                // Actually we need more complex geometry, when CornerRadius has different values.
                // But let me not to take this into account, and simplify example for a common value.
                var clip = new RectangleGeometry(new Rect(0, 0, width, height), radius.TopLeft, radius.TopLeft);
                clip.Freeze();

                return clip;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public partial class RectConvert : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Rect r = new Rect(0, 0, 0, 0);
            if (values.Length == 2)
            {
                if (values[0] is Double && values[1] is Double)
                {
                    r.Width = (Double)values[0];
                    r.Height = (Double)values[1];
                }
            }
            return r;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}