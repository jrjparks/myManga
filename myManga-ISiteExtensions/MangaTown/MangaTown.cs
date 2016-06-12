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

        private String GetInnerText(HtmlNode Node) => HtmlEntity.DeEntitize(Node.InnerText).Trim();
        private String GetAttributeText(HtmlNode Node, String Name, String Default = "") => HtmlEntity.DeEntitize(Node.GetAttributeValue(Name, Default)).Trim();
        private Uri UriStripQuery(Uri Uri) => new UriBuilder(Uri.Scheme, Uri.Host, Uri.Port, Uri.AbsolutePath).Uri;
        private Uri UriStripQuery(String Uri) => UriStripQuery(new Uri(Uri));

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
                    HtmlNode NameLinkNode = SearchResultNode.SelectSingleNode(".//p[contains(@class,'title')]/a[1]");
                    String Name = GetAttributeText(NameLinkNode, "title"),
                        Link = GetAttributeText(NameLinkNode, "href");

                    // Cover
                    HtmlNode CoverImg = SearchResultNode.SelectSingleNode(".//a[contains(@class,'manga_cover')]/img[1]");
                    String CoverUrl = CoverImg.GetAttributeValue("src", String.Format("{0}/media/images/manga_cover.jpg", ExtensionDescriptionAttribute.RootUrl));

                    // Rating
                    HtmlNode RatingNode = SearchResultNode.SelectSingleNode(".//p[contains(@class,'score')]/b[1]");
                    Double Rating = -1;
                    Double.TryParse(GetInnerText(RatingNode), out Rating);

                    // Author
                    HtmlNode AuthorNode = SearchResultNode.SelectSingleNode(".//p[contains(@class,'view')][1]/a[1]");
                    List<String> Authors = new List<String> { GetInnerText(AuthorNode) };

                    SearchResults.Add(new SearchResultObject()
                    {
                        Cover = new LocationObject()
                        {
                            Url = UriStripQuery(new Uri(CoverUrl)).ToString(),
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language
                        },
                        ExtensionName = ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                        Name = Name,
                        Url = Link,
                        Rating = Rating,
                        Authors = Authors
                    });
                }
            }

            return SearchResults;
        }

        public MangaObject ParseMangaObject(String Content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(Content);

            // The HTML generated by MangaTown for the manga view is poorly written. The body is in the head...
            HtmlNode MangaContentNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//html/head/body/section/article/div"),
                MangaInfoNode = MangaContentNode.SelectSingleNode(".//div[contains(@class,'detail_info')]");

            // Name(s)
            String Name = GetInnerText(MangaContentNode.SelectSingleNode(".//h1"));
            List<String> AlternateNames = GetInnerText(MangaInfoNode.SelectSingleNode(".//ul/li[3]/text()")).Split(';').Select(an => an.Trim()).ToList();

            // Rating
            HtmlNode RatingNode = MangaInfoNode.SelectSingleNode(".//span[contains(@class,'scores')]");
            Double Rating;
            Double.TryParse(GetInnerText(RatingNode), out Rating);

            // Cover
            HtmlNode CoverImg = MangaContentNode.SelectSingleNode(".//img");
            String CoverUrl = GetAttributeText(CoverImg, "src", String.Format("{0}/media/images/manga_cover.jpg", ExtensionDescriptionAttribute.RootUrl));
            LocationObject Cover = new LocationObject()
            {
                Url = UriStripQuery(new Uri(CoverUrl)).ToString(),
                ExtensionName = ExtensionDescriptionAttribute.Name,
                ExtensionLanguage = ExtensionDescriptionAttribute.Language
            };

            // Type and Page Direction
            MangaObjectType MangaType = MangaObjectType.Unknown;
            FlowDirection PageFlowDirection = FlowDirection.RightToLeft;
            HtmlNode TypeNode = MangaInfoNode.SelectSingleNode(".//ul/li[10]/a");
            switch (GetInnerText(TypeNode).ToLower())
            {
                case "manhwa":
                    MangaType = MangaObjectType.Manhwa;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "manhua":
                    MangaType = MangaObjectType.Manhua;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "manga":
                    MangaType = MangaObjectType.Manga;
                    PageFlowDirection = FlowDirection.LeftToRight;
                    break;
            }

            // Description
            HtmlNode DescriptionNode = MangaInfoNode.SelectSingleNode(".//*[@id='show']/text()");
            String DescriptionText = GetInnerText(DescriptionNode);

            // Genres
            List<String> Genres = MangaInfoNode.SelectNodes(".//ul/li[5]/a").Select(GetInnerText).ToList();

            // Authors
            List<String> Authors = MangaInfoNode.SelectNodes(".//ul/li[6]/a").Select(GetInnerText).ToList();

            // Artists
            List<String> Artists = MangaInfoNode.SelectNodes(".//ul/li[7]/a").Select(GetInnerText).ToList();

            // Chapters
            HtmlNodeCollection ChapterNodes = MangaContentNode.SelectNodes(".//ul[contains(@class,'chapter_list')]/li");
            List<ChapterObject> Chapters = ChapterNodes.Select(ChapterNode =>
            {
                // Location Url
                HtmlNode LinkNode = ChapterNode.SelectSingleNode(".//a");
                String Url = LinkNode.GetAttributeValue("href", null);
                if (Equals(Url, null)) return null;

                // Chapter and SubChapter
                String[] ChapterStringParts = GetInnerText(LinkNode).Split(' ').Last().Split('.');
                UInt32 Chapter = 0, SubChapter = 0;
                UInt32.TryParse(ChapterStringParts.First(), out Chapter);
                if (ChapterStringParts.Length > 1) UInt32.TryParse(ChapterStringParts.Last(), out SubChapter);

                // Released
                DateTime Released = DateTime.Parse(GetInnerText(ChapterNode.SelectSingleNode(".//span[contains(@class,'time')]")));

                return new ChapterObject()
                {
                    Name = GetInnerText(ChapterNode.SelectSingleNode(".//span[1]")),
                    Chapter = Chapter,
                    SubChapter = SubChapter,
                    Released = Released,
                    Locations = {
                        new LocationObject() {
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                            Url = Url
                        }
                }
                };
            }).Where(Chapter => !Equals(Chapter, null)).Reverse().ToList();

            return new MangaObject()
            {
                Name = Name,
                MangaType = MangaType,
                PageFlowDirection = PageFlowDirection,
                Description = DescriptionText,
                AlternateNames = AlternateNames,
                CoverLocations = { Cover },
                Authors = Authors,
                Artists = Artists,
                Genres = Genres,
                Released = Chapters.First().Released,
                Chapters = Chapters.ToList(),
                Rating = Rating
            };
        }

        public ChapterObject ParseChapterObject(String Content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(Content);

            HtmlNodeCollection PageNodes = ChapterObjectDocument.DocumentNode.SelectNodes("//html/head/body/section/div/div[2]/div/select/option");

            return new ChapterObject()
            {
                Pages = PageNodes.Select(PageNode => new PageObject()
                {
                    Url = GetAttributeText(PageNode, "value", null),
                    PageNumber = UInt32.Parse(GetInnerText(PageNode.NextSibling)),
                }).ToList(),
            };
        }

        public PageObject ParsePageObject(String Content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(Content);

            HtmlNode PageNode = PageObjectDocument.DocumentNode.SelectSingleNode(".//div[contains(@class,'page_select')]/select/option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option");

            Uri ImageLink = UriStripQuery(GetAttributeText(PageObjectDocument.GetElementbyId("image"), "src"));
            String Name = ImageLink.Segments.Last();

            return new PageObject()
            {
                Name = Name,
                PageNumber = UInt32.Parse(GetInnerText(PageNode.NextSibling)),
                Url = GetAttributeText(PageNode, "value", null),
                NextUrl = Equals(NextNode, null) ? null : GetAttributeText(NextNode, "value", null),
                PrevUrl = Equals(PrevNode, null) ? null : GetAttributeText(PrevNode, "value", null),
                ImgUrl = ImageLink.ToString()
            };
        }
    }
}
