using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension
    {
        ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute { get; }

        SearchRequestObject GetSearchRequestObject(String searchTerm);

        MangaObject ParseMangaObject(String content);
        ChapterObject ParseChapterObject(String content);
        PageObject ParsePageObject(String content);
        List<SearchResultObject> ParseSearch(String content);
    }
}
