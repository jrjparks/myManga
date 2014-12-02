using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myManga_App.Objects
{
    public sealed class ReadChapterRequestObject
    {
        public MangaObject MangaObject { get; private set; }
        public ChapterObject ChapterObject { get; private set; }

        public ReadChapterRequestObject(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;
        }
    }
}
