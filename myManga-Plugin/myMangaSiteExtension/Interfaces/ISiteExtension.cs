using System;
using System.Collections.Generic;
using System.IO;
using Core.IO;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Attributes;

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
