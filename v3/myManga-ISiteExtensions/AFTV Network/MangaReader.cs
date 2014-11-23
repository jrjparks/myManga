using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Core.Other;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;

namespace AFTV_Network
{
    [ISiteExtensionDescription(
        "MangaReader",
        "mangareader.net",
        "http://www.mangareader.net/",
        RootUrl = "http://www.mangareader.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaReader : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute isea;
        protected virtual ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject() { Url = String.Format("{0}/search/?w={1}", ISEA.RootUrl, Uri.EscapeUriString(searchTerm)), Method = SearchMethod.GET, Referer = ISEA.RefererHeader };
        }

        public MangaObject ParseMangaObject(String content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            String MangaCoverPrime = MangaObjectDocument.GetElementbyId("mangaimg").SelectSingleNode(".//img").Attributes["src"].Value;
            Regex MangaCoverRegex = new Regex(@"(\d+)\.jpg");
            Int32 MangaCoverInt = Int32.Parse(MangaCoverRegex.Match(MangaCoverPrime).Groups[1].Value);
            List<String> MangaCovers = new List<String>(MangaCoverInt + 1);
            for (Int32 mcI = 0; mcI <= MangaCoverInt; ++mcI)
                MangaCovers.Add(MangaCoverRegex.Replace(MangaCoverPrime, String.Format("{0}.jpg", mcI)));
            MangaCovers.TrimExcess();

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
                Genres = (from HtmlNode GenreNode in MangaProperties.SelectSingleNode(".//tr[8]/td[2]").SelectNodes(".//span[contains(@class,'genretags')]") select GenreNode.InnerText).ToArray();

            ChapterObject[] Chapters = (from HtmlNode ChapterNode in ChapterListing.SelectNodes(".//tr[not(contains(@class,'table_head'))]")
                                        select new ChapterObject()
                                        {
                                            MangaName = MangaName,
                                            Name = ChapterNode.SelectSingleNode(".//td[1]").LastChild.InnerText.Substring(3).Trim(),
                                            Chapter = Int32.Parse(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.Substring(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.LastIndexOf(' ') + 1)),
                                            Locations = { 
                                                    new LocationObject() { 
                                                        ExtensionName = ISEA.Name, 
                                                        Url = String.Format("{0}{1}", ISEA.RootUrl, ChapterNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value) } 
                                                },
                                            Released = DateTime.Parse(ChapterNode.SelectSingleNode(".//td[2]").InnerText)
                                        }).ToArray();

            return new MangaObject()
            {
                Name = MangaName,
                MangaType = MangaType,
                PageFlowDirection = PageFlowDirection,
                Description = HtmlEntity.DeEntitize(Desciption),
                AlternateNames = AlternateNames.ToList(),
                Covers = MangaCovers,
                Authors = Authors.ToList(),
                Artists = Artists.ToList(),
                Genres = Genres.ToList(),
                Released = DateTime.Parse(Release),
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
                             Url = String.Format("{0}{1}", ISEA.RootUrl, PageNode.Attributes["value"].Value),
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

            return new PageObject()
            {
                Name = ImageLink.Segments.Last(),
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText),
                Url = String.Format("{0}{1}", ISEA.RootUrl, PageNode.Attributes["value"].Value),
                NextUrl = (NextNode != null) ? String.Format("{0}{1}", ISEA.RootUrl, NextNode.Attributes["value"].Value) : null,
                PrevUrl = (PrevNode != null) ? String.Format("{0}{1}", ISEA.RootUrl, PrevNode.Attributes["value"].Value) : null,
                ImgUrl = ImageLink.ToString()
            };
        }

        public List<SearchResultObject> ParseSearch(String content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();

            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes("//div[contains(@class,'mangaresultitem')]");
            if (HtmlSearchResults != null)
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    HtmlNode NameLink = SearchResultNode.SelectSingleNode(".//div[contains(@class,'manga_name')]/div[1]/h3[1]/a[1]");
                    String Name = NameLink.InnerText,
                        Link = NameLink.Attributes["href"].Value,
                        CoverUrl = SearchResultNode.SelectSingleNode(".//div[contains(@class,'imgsearchresults')]").Style()["background-image"].Slice(5, -2);
                    Int32 Id; if (!Int32.TryParse(Link.Slice(1, Link.IndexOf('/', 1)), out Id)) Id = -1;
                    SearchResults.Add(new SearchResultObject()
                    {
                        CoverUrl = new Regex(@"r(\d+)\.jpg").Replace(CoverUrl, "l$1.jpg"),
                        ExtensionName = ISEA.Name,
                        Name = Name,
                        Url = String.Format("{0}{1}", ISEA.RootUrl, Link),
                        Id = Id,
                        Rating = -1,
                        Artists = null,
                        Authors = null
                    });
                }

            return SearchResults;
        }
    }
}
