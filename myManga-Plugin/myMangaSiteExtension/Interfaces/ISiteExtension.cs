using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Net;

namespace myMangaSiteExtension.Interfaces
{
    public interface ISiteExtension
    {
        /// <summary>
        /// This should be populated with the ISiteExtensionDescriptionAttribute of the plugin.
        /// </summary>
        ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute { get; }

        /// <summary>
        /// CookieCollection used to store cookies after authentication.
        /// </summary>
        CookieCollection Cookies { get; }

        /// <summary>
        /// Used to authenticate a user on a manga site.
        /// This method should store cookies for later use.
        /// </summary>
        /// <param name="credentials">User credentials</param>
        /// <returns>Authentication Success</returns>
        Boolean Authenticate(NetworkCredential credentials);

        /// <summary>
        /// Used to deauthenticate a user on a manga site.
        /// This method should clear cookies for later use.
        /// </summary>
        /// <returns>Authentication Success</returns>
        void Deauthenticate();

        /// <summary>
        /// Get user favorites from the site.
        /// </summary>
        /// <returns>A list of MangaObjects</returns>
        List<MangaObject> GetUserFavorites();

        /// <summary>
        /// Add user favorites from the site.
        /// </summary>
        /// <param name="mangaObject">MangaObject to add.</param>
        /// <returns>Add Success</returns>
        Boolean AddUserFavorites(MangaObject mangaObject);

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
