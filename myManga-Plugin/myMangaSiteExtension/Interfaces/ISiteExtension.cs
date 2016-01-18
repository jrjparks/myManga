using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension : IExtension
    {
        /// <summary>
        /// This should be populated with the ISiteExtensionDescriptionAttribute of the plugin.
        /// </summary>
        ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute { get; }

        /// <summary>
        /// Parse a MangaObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>MangaObject of parsed page.</returns>
        MangaObject ParseMangaObject(String content);

        /// <summary>
        /// Parse a ChapterObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>ChapterObject of parsed page.</returns>
        ChapterObject ParseChapterObject(String content);

        /// <summary>
        /// Parse a PageObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>PageObject of parsed page.</returns>
        PageObject ParsePageObject(String content);

        /// <summary>
        /// Search site by term.
        /// </summary>
        /// <param name="searchTerm">Search Term</param>
        /// <returns></returns>
        SearchRequestObject GetSearchRequestObject(String searchTerm);

        /// <summary>
        /// Parse search results.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>List of SearchResultObjects</returns>
        List<SearchResultObject> ParseSearch(String content);
    }
}
