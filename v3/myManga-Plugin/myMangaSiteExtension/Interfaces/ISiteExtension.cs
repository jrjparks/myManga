using System;
using System.Collections.Generic;
using System.IO;
using Core.IO;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Enums;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension
    {
        SearchRequestObject GetSearchRequestObject(String searchTerm);

        MangaObject ParseMangaObject(String content);
        ChapterObject ParseChapterObject(String content);
        PageObject ParsePageObject(String content);
        List<SearchResultObject> ParseSearch(String content);
    }
}
