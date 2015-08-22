using myMangaSiteExtension.Objects;

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
