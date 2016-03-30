using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension : IExtension
    {
        /// <summary>
        /// Parse a MangaObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>MangaObject of parsed page.</returns>
        MangaObject ParseMangaObject(String Content);

        /// <summary>
        /// Parse a ChapterObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>ChapterObject of parsed page.</returns>
        ChapterObject ParseChapterObject(String Content);

        /// <summary>
        /// Parse a PageObject from a page.
        /// </summary>
        /// <param name="content">String of page HTML content.</param>
        /// <returns>PageObject of parsed page.</returns>
        PageObject ParsePageObject(String Content);
    }
}
