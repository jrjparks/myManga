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
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
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
        private String LangChars
        {
            get
            {
                switch (ExtensionDescriptionAttribute.Language)
                {
                    default:
                    case "English":
                        return "en";

                    case "Italian":
                        return "it";
                }
            }
        }
        private Int32 LangId
        {
            get
            {
                switch (ExtensionDescriptionAttribute.Language)
                {
                    default:
                    case "English":
                        return 0;

                    case "Italian":
                        return 1;
                }
            }
        }

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

        private SearchResultObject SearchResponseToSearchResultObject(SearchResponse SearchResponse, ref HtmlWeb HtmlWeb)
        {
            String Url = String.Format("{0}/{1}", ExtensionDescriptionAttribute.RootUrl, SearchResponse.URL);
            HtmlDocument MangaDetailPage = HtmlWeb.Load(Url, WebRequestMethods.Http.Get);
            Regex MangaIdRegex = new Regex(@"window.manga_id2\s=\s");
            // String MangaId = MangaIdRegex.Match(MangaDetailPage.DocumentNode.InnerHtml);

            SearchResultObject SearchResultObject = new SearchResultObject()
            {
                Name = SearchResponse.Label.Substring(0, SearchResponse.Label.Length - 5),  // Remove end language ID
                Url = String.Format("{0}/{1}", ExtensionDescriptionAttribute.RootUrl, SearchResponse.URL)
            };
            return SearchResultObject;
        }

        public List<SearchResultObject> ParseSearch(String Content)
        {
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(SearchResponse[]));
            SearchResponse[] SearchResponses;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(Content)))
            { SearchResponses = jsonSerializer.ReadObject(ms) as SearchResponse[]; }
            HtmlWeb HtmlWeb = new HtmlWeb();
            HtmlWeb.PreRequest = new HtmlWeb.PreRequestHandler(req =>
            {
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(Cookies);
                return true;
            });
            HtmlDocument ApiMangaList = HtmlWeb.Load(
                String.Format("{0}/api/list/{1}/", ExtensionDescriptionAttribute.RootUrl, LangId),
                WebRequestMethods.Http.Get);

            return (from SearchResponse in SearchResponses
                    where SearchResponse.URL.StartsWith(String.Format("/{0}", LangChars))
                    select SearchResponseToSearchResultObject(SearchResponse, ref HtmlWeb)).ToList();
        }
    }
}
