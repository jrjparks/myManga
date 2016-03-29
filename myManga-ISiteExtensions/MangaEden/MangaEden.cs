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
using System.Runtime.Caching;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MangaEden
{
    [IExtensionDescription(
        Name = "MangaEden",
        URLFormat = "mangaeden.com",
        RefererHeader = "https://www.mangaeden.com/",
        RootUrl = "https://www.mangaeden.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English",
        RequiresAuthentication = false)]
    public class MangaEden : ISiteExtension
    {
        #region MangaEden
        protected readonly MemoryCache ApiCache;

        public MangaEden()
        {
            ApiCache = new MemoryCache("MangaEden-myManga");
        }

        protected String ApiMangaListCacheKey
        { get { return String.Format("ApiMangaList-{0}", LanguageId); } }

        /// <summary>
        /// Get ApiMangaList from cache or load from web
        /// </summary>
        protected MangaList ApiMangaList
        {
            get
            {
                if (!ApiCache.Contains(ApiMangaListCacheKey))
                {
                    // Cache API calls for 30 minutes
                    using (WebClient client = new WebClient())
                    {
                        DataContractJsonSerializer apiMangaListJsonSerializer = new DataContractJsonSerializer(typeof(MangaList));
                        using (MemoryStream ms = new MemoryStream(client.DownloadData(String.Format("{0}/api/list/{1}/", ExtensionDescriptionAttribute.RootUrl, LanguageId))))
                        { ApiCache.Set(ApiMangaListCacheKey, apiMangaListJsonSerializer.ReadObject(ms), DateTimeOffset.Now.AddMinutes(30)); }
                    }
                }

                return ApiCache.Get(ApiMangaListCacheKey) as MangaList;
            }
        }

        protected String LanguageUrl(String alias)
        { return String.Format("/{0}-manga/{1}/", LanguageChars, alias); }

        protected String LanguageChars
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

        protected Int32 LanguageId
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

        protected SearchResultObject ToSearchResultObject(MangaItem item)
        {
            if (Equals(item, null)) return null;
            SearchResultObject SearchResultObject = item.ToSearchResultObject();
            SearchResultObject.ExtensionName = SearchResultObject.Cover.ExtensionName = ExtensionDescriptionAttribute.Name;
            SearchResultObject.ExtensionLanguage = SearchResultObject.Cover.ExtensionLanguage = ExtensionDescriptionAttribute.Language;
            return SearchResultObject;
        }

        protected MangaObject ToMangaObject(MangaDetails details)
        {
            if (Equals(details, null)) return null;
            MangaItem mangaItem = ApiMangaList.Manga.FirstOrDefault(m => Equals(m.Alias, details.Alias));
            MangaObject MangaObject = details.ToMangaObject();
            MangaObject.Locations = new List<LocationObject> { new LocationObject() {
                Enabled = true,
                ExtensionName = ExtensionDescriptionAttribute.Name,
                ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                Url = String.Format("https://www.mangaeden.com/api/manga/{0}", mangaItem.Id)
            } };
            foreach (ChapterObject ChapterObject in MangaObject.Chapters)
            {
                LocationObject LocObj = ChapterObject.Locations.First();
                LocObj.ExtensionName = ExtensionDescriptionAttribute.Name;
                LocObj.ExtensionLanguage = ExtensionDescriptionAttribute.Language;
            }
            return MangaObject;
        }

        protected ChapterObject ToChapterObject(ChapterDetail details)
        {
            if (Equals(details, null)) return null;
            ChapterObject ChapterObject = details.ToChapterObject();
            foreach (PageObject Page in ChapterObject.Pages)
            { Page.Url = ExtensionDescriptionAttribute.RootUrl; }
            return ChapterObject;
        }
        #endregion

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

        public MangaObject ParseMangaObject(String Content)
        {
            DataContractJsonSerializer mangaDetailJsonSerializer = new DataContractJsonSerializer(typeof(MangaDetails));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(Content)))
            { return ToMangaObject(mangaDetailJsonSerializer.ReadObject(ms) as MangaDetails); }
        }

        public ChapterObject ParseChapterObject(String Content)
        {
            DataContractJsonSerializer chapterDetailJsonSerializer = new DataContractJsonSerializer(typeof(ChapterDetail));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(Content)))
            { return ToChapterObject(chapterDetailJsonSerializer.ReadObject(ms) as ChapterDetail); }
        }

        public PageObject ParsePageObject(String Content)
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
            // Parse search json
            DataContractJsonSerializer searchResponseJsonSerializer = new DataContractJsonSerializer(typeof(SearchResponse[]));
            SearchResponse[] SearchResponses;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(Content)))
            { SearchResponses = searchResponseJsonSerializer.ReadObject(ms) as SearchResponse[]; }

            return (from mangaItem in ApiMangaList.Manga
                    where SearchResponses.Count(sr => Equals(sr.URL, LanguageUrl(mangaItem.Alias))) > 0
                    select ToSearchResultObject(mangaItem)).ToList();
        }
    }
}
