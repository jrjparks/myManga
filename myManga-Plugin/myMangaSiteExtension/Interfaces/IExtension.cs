using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;

namespace myMangaSiteExtension.Interfaces
{
    public interface IExtension
    {
        #region Description

        /// <summary>
        /// Extension descriptor
        /// </summary>
        IExtensionDescriptionAttribute ExtensionDescriptionAttribute { get; }

        /// <summary>
        /// Icon used for display.
        /// </summary>
        Icon ExtensionIcon { get; }

        #endregion

        #region Authentication

        /// <summary>
        /// CookieCollection used to store cookies after authentication.
        /// </summary>
        CookieCollection Cookies { get; }

        /// <summary>
        /// CookieCollection used to store cookies after authentication.
        /// </summary>
        Boolean IsAuthenticated { get; }

        /// <summary>
        /// Used to authenticate a user on a manga site.
        /// This method should store cookies for later use.
        /// </summary>
        /// <param name="Credentials">User credentials</param>
        /// <returns>Authentication Success</returns>
        Boolean Authenticate(NetworkCredential Credentials, CancellationToken CT, IProgress<Int32> ProgressReporter);

        /// <summary>
        /// Used to deauthenticate a user on a manga site.
        /// This method should clear cookies for later use.
        /// </summary>
        /// <returns>Authentication Success</returns>
        void Deauthenticate();

        #endregion

        #region Remote library

        /// <summary>
        /// Get user favorites from the site.
        /// </summary>
        /// <returns>A list of MangaObjects</returns>
        List<MangaObject> GetUserFavorites();

        /// <summary>
        /// Add user favorites from the site.
        /// </summary>
        /// <param name="MangaObject">MangaObject to add.</param>
        /// <returns>Add Success</returns>
        Boolean AddUserFavorites(MangaObject MangaObject);

        /// <summary>
        /// Remove user favorites from the site.
        /// </summary>
        /// <param name="MangaObject">MangaObject to remove.</param>
        /// <returns>Remove Success</returns>
        Boolean RemoveUserFavorites(MangaObject MangaObject);

        #endregion

        #region Search

        /// <summary>
        /// Generate search request.
        /// </summary>
        /// <param name="SearchTerm">Search term.</param>
        /// <returns>SearchRequestObject</returns>
        SearchRequestObject GetSearchRequestObject(String SearchTerm);

        /// <summary>
        /// Use extension to parse search page.
        /// </summary>
        /// <param name="SearchTerm">Search term to send.</param>
        /// <returns>List of SearchResultObjects</returns>
        List<SearchResultObject> ParseSearch(String Content);

        #endregion
    }
}
