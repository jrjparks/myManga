using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MangaEden
{
    [IExtensionDescription(
        Name = "MangaEden",
        URLFormat = "mangaeden.com",
        RefererHeader = "http://www.mangaeden.com/",
        RootUrl = "http://www.mangaeden.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English",
        RequiresAuthentication = false)]
    public class MangaEden : ISiteExtension
    {
        #region IExtesion
        private IExtensionDescriptionAttribute EDA;
        public IExtensionDescriptionAttribute ExtensionDescriptionAttribute
        { get { return EDA ?? (EDA = GetType().GetCustomAttribute<IExtensionDescriptionAttribute>(false)); } }

        private Icon extensionIcon;
        public Icon ExtensionIcon
        {
            get
            {
                if (Equals(extensionIcon, null)) extensionIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                return extensionIcon;
            }
        }

        public CookieCollection Cookies
        { get; private set; }

        public Boolean IsAuthenticated
        { get; private set; }

        public bool Authenticate(NetworkCredential credentials, CancellationToken ct, IProgress<Int32> ProgressReporter)
        {
            if (IsAuthenticated) return true;
            throw new NotImplementedException();
        }

        public void Deauthenticate()
        {
            if (!IsAuthenticated) return;
            Cookies = null;
            IsAuthenticated = false;
        }

        public List<MangaObject> GetUserFavorites()
        {
            throw new NotImplementedException();
        }

        public bool AddUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }

        public bool RemoveUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }
        #endregion

        public MangaObject ParseMangaObject(String content)
        {
            throw new NotImplementedException();
        }

        public ChapterObject ParseChapterObject(String content)
        {
            throw new NotImplementedException();
        }

        public PageObject ParsePageObject(String content)
        {
            throw new NotImplementedException();
        }

        public SearchRequestObject GetSearchRequestObject(String SearchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/ajax/search-manga/?term={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(SearchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader
            };
        }

        public List<SearchResultObject> ParseSearch(String Content)
        {
            throw new NotImplementedException();
        }
    }
}
