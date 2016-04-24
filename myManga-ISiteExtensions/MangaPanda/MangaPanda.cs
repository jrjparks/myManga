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

namespace MangaPanda
{
    [IExtensionDescription(
        Name = "MangaPanda",
        URLFormat = "mangapanda.com",
        RefererHeader = "http://www.mangapanda.com/",
        RootUrl = "http://www.mangapanda.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaPanda : ISiteExtension
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

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject() { Url = String.Format("{0}/search/?w={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)), Method = SearchMethod.GET, Referer = ExtensionDescriptionAttribute.RefererHeader };
        }

        public MangaObject ParseMangaObject(String content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            String MangaCoverPrime = MangaObjectDocument.GetElementbyId("mangaimg").SelectSingleNode(".//img").Attributes["src"].Value;
            Regex MangaCoverRegex = new Regex(@"(\d+)\.jpg");
            Int32 MangaCoverInt = Int32.Parse(MangaCoverRegex.Match(MangaCoverPrime).Groups[1].Value);
            List<String> MangaCovers = new List<String>(MangaCoverInt + 1);
            List<LocationObject> Covers = new List<LocationObject>();
            for (Int32 mcI = 0; mcI <= MangaCoverInt; ++mcI)
                Covers.Add(new LocationObject()
                {
                    Url = MangaCoverRegex.Replace(MangaCoverPrime, String.Format("{0}.jpg", mcI)),
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    ExtensionLanguage = ExtensionDescriptionAttribute.Language
                });
            Covers.TrimExcess();

            HtmlNode MangaProperties = MangaObjectDocument.GetElementbyId("mangaproperties").SelectSingleNode(".//table"),
                ChapterListing = MangaObjectDocument.GetElementbyId("listing"),
                MangaDesciption = MangaObjectDocument.GetElementbyId("readmangasum").SelectSingleNode(".//p");

            String MangaName = HtmlEntity.DeEntitize(MangaProperties.SelectSingleNode(".//tr[1]/td[2]/h2").InnerText),
                ReadDirection = MangaProperties.SelectSingleNode(".//tr[7]/td[2]").InnerText,
                ReleaseYear = Regex.Match(MangaProperties.SelectSingleNode(".//tr[3]/td[2]").InnerText, @"\d+").Value,
                Release = String.Format("01/01/{0}", String.IsNullOrWhiteSpace(ReleaseYear) ? "0001" : ReleaseYear),
                Desciption = MangaDesciption != null ? MangaDesciption.InnerText : String.Empty;
            MangaObjectType MangaType = MangaObjectType.Unknown;
            FlowDirection PageFlowDirection = FlowDirection.RightToLeft;
            switch (ReadDirection.ToLower())
            {
                default:
                    MangaType = MangaObjectType.Unknown;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "right to left":
                    MangaType = MangaObjectType.Manga;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "left to right":
                    MangaType = MangaObjectType.Manhwa;
                    PageFlowDirection = FlowDirection.LeftToRight;
                    break;
            }

            String[] AlternateNames = MangaProperties.SelectSingleNode(".//tr[2]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Authors = MangaProperties.SelectSingleNode(".//tr[5]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Artists = MangaProperties.SelectSingleNode(".//tr[6]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Genres = (from HtmlNode GenreNode in MangaProperties.SelectSingleNode(".//tr[8]/td[2]").SelectNodes(".//span[contains(@class,'genretags')]") select HtmlEntity.DeEntitize(GenreNode.InnerText)).ToArray();

            ChapterObject[] Chapters = (from HtmlNode ChapterNode in ChapterListing.SelectNodes(".//tr[not(contains(@class,'table_head'))]")
                                        select new ChapterObject()
                                        {
                                            Name = HtmlEntity.DeEntitize(ChapterNode.SelectSingleNode(".//td[1]").LastChild.InnerText.Substring(3).Trim()),
                                            Chapter = UInt32.Parse(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.Substring(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.LastIndexOf(' ') + 1)),
                                            Locations = {
                                                    new LocationObject() {
                                                        ExtensionName = ExtensionDescriptionAttribute.Name,
                                                        ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                                                        Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, ChapterNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value) }
                                                },
                                            Released = DateTime.ParseExact(ChapterNode.SelectSingleNode(".//td[2]").InnerText, "MM/dd/yyyy", CultureInfo.InvariantCulture)
                                        }).ToArray();

            return new MangaObject()
            {
                Name = HtmlEntity.DeEntitize(MangaName),
                MangaType = MangaType,
                PageFlowDirection = PageFlowDirection,
                Description = HtmlEntity.DeEntitize(Desciption),
                AlternateNames = AlternateNames.ToList(),
                CoverLocations = Covers,
                Authors = (from Author in Authors select HtmlEntity.DeEntitize(Author)).ToList(),
                Artists = (from Artist in Artists select HtmlEntity.DeEntitize(Artist)).ToList(),
                Genres = Genres.ToList(),
                Released = DateTime.ParseExact(Release, "MM/dd/yyyy", CultureInfo.InvariantCulture),
                Chapters = Chapters.ToList()
            };
        }

        public ChapterObject ParseChapterObject(String content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(content);
            return new ChapterObject()
            {
                Pages = (from HtmlNode PageNode in ChapterObjectDocument.GetElementbyId("pageMenu").SelectNodes(".//option")
                         select new PageObject()
                         {
                             Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, PageNode.Attributes["value"].Value),
                             PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText)
                         }).ToList()
            };
        }

        public PageObject ParsePageObject(String content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            HtmlNode PageNode = PageObjectDocument.GetElementbyId("pageMenu").SelectSingleNode(".//option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option");

            Uri ImageLink = new Uri(PageObjectDocument.GetElementbyId("img").Attributes["src"].Value);
            String Name = ImageLink.ToString().Split('/').Last();

            return new PageObject()
            {
                Name = Name,
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText),
                Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, PageNode.Attributes["value"].Value),
                NextUrl = (NextNode != null) ? String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, NextNode.Attributes["value"].Value) : null,
                PrevUrl = (PrevNode != null) ? String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, PrevNode.Attributes["value"].Value) : null,
                ImgUrl = ImageLink.ToString()
            };
        }

        public List<SearchResultObject> ParseSearch(String content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();

            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes("//div[contains(@class,'mangaresultitem')]");
            if (!Equals(HtmlSearchResults, null))
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    HtmlNode NameLink = SearchResultNode.SelectSingleNode(".//div[contains(@class,'manga_name')]/div[1]/h3[1]/a[1]");
                    String Name = NameLink.InnerText,
                        Link = NameLink.Attributes["href"].Value,
                        CoverUrl = SearchResultNode.SelectSingleNode(".//div[contains(@class,'imgsearchresults')]").Style()["background-image"].Slice(5, -2);
                    Int32 Id; if (!Int32.TryParse(Link.Slice(1, Link.IndexOf('/', 1)), out Id)) Id = -1;
                    SearchResults.Add(new SearchResultObject()
                    {
                        Cover = new LocationObject()
                        {
                            Url = new Regex(@"r(\d+)\.jpg").Replace(CoverUrl, "l$1.jpg"),
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language
                        },
                        Name = Name,
                        Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, Link),
                        ExtensionName = ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                        Id = Id.ToString(),
                        Rating = -1,
                        Artists = null,
                        Authors = null
                    });
                }

            return SearchResults;
        }
    }
}
