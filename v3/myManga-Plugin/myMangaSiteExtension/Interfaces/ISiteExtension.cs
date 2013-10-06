using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension
{
    public interface ISiteExtension
    {
        String GetMangaURI();
        String GetChapterURI();

        MangaObject ParseMangaObject(String HTML);
        ChapterObject ParseChapterObject(String HTML);
        PageObject ParsePageObject(String HTML);

        Object ParseCoverImage();
        Object GetSearchUri();
        Object ParseSearch();
    }
}
