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
    public class ObjectIsLocal : IMultiValueConverter
    {
        private readonly App App = App.Current as App;

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MangaObject manga_object = values.FirstOrDefault(o => o is MangaObject) as MangaObject;
            ChapterObject chapter_object = values.FirstOrDefault(o => o is ChapterObject) as ChapterObject;
            if (!MangaObject.Equals(manga_object, null))
            {
                if (!ChapterObject.Equals(chapter_object, null))
                { return chapter_object.IsLocal(System.IO.Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, manga_object.MangaFileName()), App.CHAPTER_ARCHIVE_EXTENSION); }
                return manga_object.IsLocal(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_EXTENSION);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
