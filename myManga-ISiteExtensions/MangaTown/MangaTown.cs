using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace MangaTown
{
    [IExtensionDescription(
        Name = "MangaTown",
        URLFormat = "mangatown.com",
        RefererHeader = "http://www.mangatown.com/",
        RootUrl = "http://www.mangatown.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaTown : ISiteExtension
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

        public SearchRequestObject GetSearchRequestObject(String SearchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/search.php?name={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(SearchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader
            };
        }

        public List<SearchResultObject> ParseSearch(String Content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();
            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(Content);

            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes("//ul[contains(@class,'manga_pic_list')]/li");
            if (!Equals(HtmlSearchResults, null))
            {
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    // Name & Link
                    HtmlNode NameLink = SearchResultNode.SelectSingleNode(".//p[contains(@class,'title')]/a[1]");
                    String Name = NameLink.InnerText.Trim(),
                        Link = NameLink.Attributes["href"].Value;

                    // Cover
                    HtmlNode CoverImg = SearchResultNode.SelectSingleNode(".//a[contains(@class,'manga_cover')]/img[1]");
                    String CoverUrl = CoverImg.GetAttributeValue("src", String.Format("{0/media/images/manga_cover.jpg", ExtensionDescriptionAttribute.RootUrl));

                    // Rating
                    HtmlNode RatingNode = SearchResultNode.SelectSingleNode(".//p[contains(@class,'score')]/b[1]");
                    Double Rating = -1;
                    Double.TryParse(RatingNode.InnerText, out Rating);

                    // Author
                    HtmlNode AuthorNode = SearchResultNode.SelectSingleNode(".//p[contains(@class,'view')][1]/a[1]");
                    String Author = AuthorNode.InnerText.Trim();

                    SearchResults.Add(new SearchResultObject()
                    {
                        Cover = new LocationObject()
                        {
                            Url = CoverUrl,
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language
                        },
                        ExtensionName = ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                        Name = Name,
                        Url = Link,
                        Rating = Rating,
                        Artists = null,
                        Authors = { Author }
                    });
                }
            }

            return SearchResults;
        }

        public MangaObject ParseMangaObject(String Content)
        {
            throw new NotImplementedException();
        }

        public ChapterObject ParseChapterObject(String Content)
        {
            throw new NotImplementedException();
        }

        public PageObject ParsePageObject(String Content)
        {
            throw new NotImplementedException();
        }
    }
}
