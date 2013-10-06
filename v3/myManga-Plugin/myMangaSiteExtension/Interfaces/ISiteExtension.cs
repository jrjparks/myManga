using System;
using System.Collections.Generic;
using System.IO;
using Core.IO;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension
{
    public interface ISiteExtension
    {
        String GetMangaURI();
        String GetChapterURI();
        String GetSearchUri(String searchTerm);

        MangaObject ParseMangaObject(String content);
        ChapterObject ParseChapterObject(String content);
        PageObject ParsePageObject(String content);

        Stream ParseCoverImage(String content);
        Dictionary<String, SearchResultObject> ParseSearch(String content);
    }
}
