using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.Other.Singleton;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(MangaObject), typeof(ImageSource))]
    public class LoadImageFromChapterArchive : IValueConverter
    {
        private readonly App App = App.Current as App;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MangaObject mo = value as MangaObject;
            try
            {
                String archive_filename = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, mo.MangaArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)),
                    filename = parameter as String;
                BitmapImage bitmap_image = new BitmapImage();

                Stream image_stream;
                bitmap_image.BeginInit();

                if (Singleton<ZipStorage>.Instance.TryRead(archive_filename, filename, out image_stream) && image_stream.Length > 0)
                { bitmap_image.StreamSource = image_stream; }                // Load from local zip
                else { bitmap_image.UriSource = new Uri(mo.SelectedCover); } // Load from web

                bitmap_image.CacheOption = BitmapCacheOption.OnLoad;
                bitmap_image.EndInit();
                if (bitmap_image.StreamSource != null) { bitmap_image.StreamSource.Close(); /* Close bitmapImage.StreamSource if used */ }
                if (image_stream != null) { image_stream.Close(); /* Close image_stream if used */ }

                return bitmap_image;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("There is no way I'm writing a reverse image look-up...");
        }
    }
}
