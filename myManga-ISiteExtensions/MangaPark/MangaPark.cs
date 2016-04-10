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

namespace MangaPark
{
    [IExtensionDescription(
        Name = "MangaPark",
        URLFormat = "mangapark.me",
        RefererHeader = "https://www.mangapark.me/",
        RootUrl = "https://www.mangapark.me",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English",
        RequiresAuthentication = false)]
    public class MangaPark : ISiteExtension
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

        #region ISiteExtension
        public SearchRequestObject GetSearchRequestObject(string SearchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/search?q={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(SearchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader
            };
        }

        public List<SearchResultObject> ParseSearch(string Content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();
            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(Content);
            HtmlNode noMatchNode = SearchResultDocument.DocumentNode.SelectSingleNode(".//div[contains(@class, 'manga-list')]/div[contains(@class, 'no-match')]");
            if (!Equals(noMatchNode, null)) return SearchResults;

            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class, 'manga-list')]/div[contains(@class, 'item')]");
            if (!Equals(HtmlSearchResults, null))
            {
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    try
                    {
                        LocationObject Cover = null;
                        HtmlNode TitleImgNode = SearchResultNode.SelectSingleNode(".//a[contains(@class, 'cover')]/img"),
                            ContentNode = SearchResultNode.SelectSingleNode(".//div[contains(@class, 'info')]");
                        HtmlNodeCollection AuthorsArtistsNodes = ContentNode.SelectNodes(".//div[2]/a[starts-with(@href, '/search?autart=')]");
                        String Name = TitleImgNode.GetAttributeValue("title", "Unknown"),
                            CoverUrl = TitleImgNode.GetAttributeValue("src", null);
                        if (!Equals(CoverUrl, null))
                        {
                            CoverUrl = Regex.Replace(CoverUrl, @"/\d\.jpg", ".jpg", RegexOptions.Singleline);
                            Cover = new LocationObject()
                            {
                                Enabled = true,
                                Url = CoverUrl,
                                ExtensionName = ExtensionDescriptionAttribute.Name,
                                ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                            };
                        }
                        List<String> AuthorsArtists = (from Node in AuthorsArtistsNodes
                                                       select HtmlEntity.DeEntitize(Node.InnerText)).ToList();

                        String Url = TitleImgNode.ParentNode.GetAttributeValue("href", null);
                        if (Equals(Url, null)) continue;
                        Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, Url);

                        SearchResults.Add(new SearchResultObject()
                        {
                            Name = HtmlEntity.DeEntitize(Name),
                            Cover = Cover,
                            Authors = AuthorsArtists,
                            Artists = AuthorsArtists,
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                            Url = Url
                        });
                    }
                    catch { }
                }
            }
            return SearchResults;
        }

        public MangaObject ParseMangaObject(string Content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(Content);

            HtmlNode MangaNode = MangaObjectDocument.DocumentNode.SelectSingleNode(".//section[contains(@class, 'manga')]");
            String Name = HtmlEntity.DeEntitize(MangaNode.SelectSingleNode(".//div/div[1]/h1/a").InnerText);
            Name = Name.Substring(0, Name.LastIndexOf(' '));
            HtmlNode AlternateNamesNode = MangaNode.SelectSingleNode(".//div/table/tr/td[2]/table/tr[4]/td");
            List<String> AlternateNames = (from AltName in HtmlEntity.DeEntitize(AlternateNamesNode.InnerText).Split(';')
                                           select AltName.Trim()).ToList();
            List<String> Authors = (from Node in MangaNode.SelectNodes(".//div/table/tr/td[2]/table/tr[5]/td/a")
                                    select HtmlEntity.DeEntitize(Node.InnerText)).ToList();
            List<String> Artists = (from Node in MangaNode.SelectNodes(".//div/table/tr/td[2]/table/tr[6]/td/a")
                                    select HtmlEntity.DeEntitize(Node.InnerText)).ToList();
            List<String> Genres = (from Node in MangaNode.SelectNodes(".//div/table/tr/td[2]/table/tr[7]/td/a")
                                   select HtmlEntity.DeEntitize(Node.InnerText)).ToList();

            // Detect type
            MangaObjectType MangaType = MangaObjectType.Unknown;
            String mType = MangaNode.SelectSingleNode(".//div/table/tr/td[2]/table/tr[8]/td").InnerText.ToLower();
            if (mType.Contains("japanese manga")) MangaType = MangaObjectType.Manga;
            else if (mType.Contains("korean manhwa")) MangaType = MangaObjectType.Manhwa;

            // Get description
            String Description = HtmlEntity.DeEntitize(MangaNode.SelectSingleNode(".//div/p").InnerText);

            // Chapters
            List<ChapterObject> Chapters = new List<ChapterObject>();
            foreach (HtmlNode ChapterVersionNode in MangaNode.SelectNodes(".//*[@id='list']/div[starts-with(@id, 'stream_')]"))
            {
                foreach (HtmlNode VolumeNode in ChapterVersionNode.SelectNodes(".//div[contains(@class, 'volume')]"))
                {
                    UInt32 Volume = 0;
                    HtmlNode VolumeNameNode = VolumeNode.SelectSingleNode(".//h4");
                    if (!Equals(VolumeNameNode, null))
                    {
                        String[] idParts = VolumeNameNode.GetAttributeValue("id", "v-1-").Split('-');
                        UInt32.TryParse(idParts[2], out Volume);
                    }

                    foreach (HtmlNode ChapterNode in ChapterVersionNode.SelectNodes(".//div/ul/li"))
                    {
                        HtmlNode InfoNode = ChapterNode.SelectSingleNode(".//a");
                        String ChapterName = HtmlEntity.DeEntitize(InfoNode.InnerText),
                            Url = InfoNode.GetAttributeValue("href", null);
                        UInt32 Chapter = 0, SubChapter = 0;

                        Match match = Regex.Match(ChapterName, @"(vol\.(?<Volume>\d+)\s)?ch\.(?<Chapter>\d+)(\.(?<SubChapter>\d+))?");
                        if (match.Success)
                        {
                            if (match.Groups["Volume"].Success)
                                UInt32.TryParse(match.Groups["Volume"].Value, out Volume);
                            if (match.Groups["Chapter"].Success)
                                UInt32.TryParse(match.Groups["Chapter"].Value, out Chapter);
                            if (match.Groups["SubChapter"].Success)
                                UInt32.TryParse(match.Groups["SubChapter"].Value, out SubChapter);
                        }

                        if (Equals(Url, null)) continue;
                        Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, Url);

                        ChapterObject NewChapterObject = new ChapterObject()
                        {
                            Name = ChapterName,
                            Volume = Volume,
                            Chapter = Chapter,
                            SubChapter = SubChapter,
                            Locations = {
                                new LocationObject() {
                                    Enabled = true,
                                    ExtensionName = ExtensionDescriptionAttribute.Name,
                                    ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                                    Url = Url
                                }
                            }
                        };
                        ChapterObject ExistingChapterObject = Chapters.FirstOrDefault(o =>
                        {
                            if (!Int32.Equals(o.Chapter, NewChapterObject.Chapter)) return false;
                            if (!Int32.Equals(o.SubChapter, NewChapterObject.SubChapter)) return false;
                            return true;
                        });
                        if (Equals(ExistingChapterObject, null)) { Chapters.Add(NewChapterObject); }
                        else { ExistingChapterObject.Merge(NewChapterObject); }
                    }
                }
            }
            Chapters = Chapters.OrderBy(c => c.Chapter).ThenBy(c => c.SubChapter).ThenBy(c => c.Volume).ToList();

            return new MangaObject()
            {
                Name = Name,
                AlternateNames = AlternateNames,
                Description = Description,
                Authors = Authors,
                Artists = Artists,
                Genres = Genres,
                MangaType = MangaType,
                Chapters = Chapters
            };
        }

        public ChapterObject ParseChapterObject(string Content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(Content);

            HtmlNodeCollection PageNodes = ChapterObjectDocument.DocumentNode.SelectNodes("/html/body/section[3]/div/div[10]/div/div[2]/p/a");
            return new ChapterObject()
            {
                Pages = (from PageNode in PageNodes
                         select new PageObject()
                         {
                             Url = String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, PageNode.GetAttributeValue("href", null)),
                             PageNumber = UInt32.Parse(PageNode.InnerText)
                         }).ToList()
            };
        }

        public PageObject ParsePageObject(string Content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(Content);

            // Book Link
            String bookLink = String.Empty;
            Match bookLinkMatch = Regex.Match(Content, @"_book_link\s=\s'(?<URL>[\w\d-/]+)';");
            if (bookLinkMatch.Success && bookLinkMatch.Groups["URL"].Success)
                bookLink = bookLinkMatch.Groups["URL"].Value;
            else throw new MissingFieldException("Unable to locate _book_link for page.", "_book_link");

            // Prev Link
            String prevLink = String.Empty;
            Match prevLinkMatch = Regex.Match(Content, @"_prev_link\s=\s'(?<URL>[\w\d-/]+)';");
            if (prevLinkMatch.Success && prevLinkMatch.Groups["URL"].Success)
                prevLink = prevLinkMatch.Groups["URL"].Value;
            else throw new MissingFieldException("Unable to locate _prev_link for page.", "_prev_link");

            // Next Link
            String nextLink = String.Empty;
            Match nextLinkMatch = Regex.Match(Content, @"_next_link\s=\s'(?<URL>[\w\d-/]+)';");
            if (nextLinkMatch.Success && nextLinkMatch.Groups["URL"].Success)
                nextLink = nextLinkMatch.Groups["URL"].Value;
            else throw new MissingFieldException("Unable to locate _next_link for page.", "_next_link");

            // PageNumber
            UInt32 PageNumber = 0;
            Match pageNumberMatch = Regex.Match(Content, @"_page_sn\s=\s'(?<PageNumber>\d+)';");
            if (pageNumberMatch.Success && pageNumberMatch.Groups["PageNumber"].Success)
                UInt32.TryParse(pageNumberMatch.Groups["PageNumber"].Value, out PageNumber);
            else throw new MissingFieldException("Unable to locate _page_sn for page.", "_page_sn");

            // Manga Url Name
            String mangaNameUrl = String.Empty;
            Match mangaNameUrlMatch = Regex.Match(Content, @"_manga_name\s=\s'(?<Name>[\w\d-]+)';");
            if (mangaNameUrlMatch.Success && mangaNameUrlMatch.Groups["Name"].Success)
                mangaNameUrl = mangaNameUrlMatch.Groups["Name"].Value;
            else throw new MissingFieldException("Unable to locate _manga_name for page.", "_manga_name");

            String ImgSrc = PageObjectDocument.DocumentNode.SelectSingleNode(".//img[@id='img-1']").GetAttributeValue("src", null);
            String Name = ImgSrc.ToString().Split('/').Last();

            return new PageObject()
            {
                Name = Name,
                ImgUrl = ImgSrc,
                Url = String.Format("{0}/manga/{1}{2}/{3}", ExtensionDescriptionAttribute.RootUrl, mangaNameUrl, bookLink, PageNumber),
                PageNumber = PageNumber,
                NextUrl = nextLink.Contains(bookLink) ? String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, nextLink): null,
                PrevUrl = nextLink.Contains(bookLink) ? String.Format("{0}{1}", ExtensionDescriptionAttribute.RootUrl, prevLink) : null
            };
        }
        #endregion
    }
}
