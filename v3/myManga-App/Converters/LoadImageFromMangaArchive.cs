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
    public class LoadImageFromMangaArchive : IValueConverter
    {
        private readonly App App = App.Current as App;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MangaObject mo = value as MangaObject;
            try
            {
                String archive_filename = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, mo.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)),
                    filename = Path.GetFileName(mo.SelectedCover);
                BitmapImage bitmapImage = new BitmapImage();
                using (Stream cover_stream = Singleton<ZipStorage>.Instance.Read(archive_filename, filename))
                {
                    bitmapImage.BeginInit();
                    if (cover_stream != null && cover_stream.Length > 0)
                        bitmapImage.StreamSource = cover_stream;
                    else
                        bitmapImage.UriSource = new Uri(mo.SelectedCover);

                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    using (bitmapImage.StreamSource) { }
                }
                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
