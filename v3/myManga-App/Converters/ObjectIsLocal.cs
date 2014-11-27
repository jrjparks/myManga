using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(Object), typeof(Boolean))]
    public class ObjectIsLocal : IValueConverter
    {
        private readonly App App = App.Current as App;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MangaObject)
            { return (value as MangaObject).IsLocal(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_EXTENSION); }
            else if (value is ChapterObject)
            {
                if (!(parameter is MangaObject)) throw new Exception("When looking up a ChapterObject parameter must be a MangaObject");
                return (value as ChapterObject).IsLocal(System.IO.Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, (parameter as MangaObject).MangaFileName()), App.CHAPTER_ARCHIVE_EXTENSION);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
