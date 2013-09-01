using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension
{
    public interface ISiteExtension
    {
        MangaObject ParseMangaObject(String HTML);
        ChapterObject ParseChapterObject(String HTML);
        PageObject ParsePageObject(String HTML);

        Object CoverImage();
        Object Search();
    }
}
