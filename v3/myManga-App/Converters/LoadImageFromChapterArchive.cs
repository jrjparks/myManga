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
                    filename = Path.GetFileName(mo.SelectedCover);
                BitmapImage bitmapImage = new BitmapImage();
                using (Stream image_stream = Singleton<ZipStorage>.Instance.Read(archive_filename, filename))
                {
                    bitmapImage.BeginInit();
                    if (image_stream != null && image_stream.Length > 0)
                        bitmapImage.StreamSource = image_stream;            // Load from local zip
                    else
                        bitmapImage.UriSource = new Uri(mo.SelectedCover);  // Load from web

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
            throw new NotSupportedException("There is no way I'm writing a reverse image look-up...");
        }
    }
}
