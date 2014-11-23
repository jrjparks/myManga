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
using myMangaSiteExtension.Utilities;

namespace Batoto
{
    [ISiteExtensionDescription(
        "Batoto",
        "bato.to",
        "http://bato.to/",
        RootUrl = "http://bato.to",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class Batoto : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute isea;
        protected virtual ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(string searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/search?name={1}", ISEA.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = ISEA.RefererHeader
            };
        }

        public MangaObject ParseMangaObject(string content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            HtmlNode InformationNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'ipsBox')]/div");
            String MangaCover = InformationNode.SelectSingleNode(".//div[1]/img").Attributes["src"].Value;

            HtmlNode MangaProperties = InformationNode.SelectSingleNode(".//table[contains(@class,'ipb_table')]"),
                ChapterListing = MangaObjectDocument.DocumentNode.SelectSingleNode("//table[contains(@class,'chapters_list')]");

            String MangaName = HtmlEntity.DeEntitize(MangaObjectDocument.DocumentNode.SelectSingleNode("//h1[contains(@class,'ipsType_pagetitle')]").InnerText.Trim()),
                MangaTypeProp = HtmlEntity.DeEntitize(MangaProperties.SelectSingleNode(".//tr[5]/td[2]").InnerText),
                Desciption = HtmlEntity.DeEntitize(MangaProperties.SelectSingleNode(".//tr[7]/td[2]").InnerText.Replace("<br>", "\n"));
            MangaObjectType MangaType = MangaObjectType.Unknown;
            FlowDirection PageFlowDirection = FlowDirection.RightToLeft;
            switch (MangaTypeProp.ToLower())
            {
                default:
                    MangaType = MangaObjectType.Unknown;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "manga (japanese)":
                    MangaType = MangaObjectType.Manga;
                    PageFlowDirection = FlowDirection.RightToLeft;
                    break;

                case "manhwa (korean)":
                    MangaType = MangaObjectType.Manhwa;
                    PageFlowDirection = FlowDirection.LeftToRight;
                    break;

                case "manhua (chinese)":
                    MangaType = MangaObjectType.Manhua;
                    PageFlowDirection = FlowDirection.LeftToRight;
                    break;
            }

            HtmlNodeCollection AlternateNameNodes = MangaProperties.SelectSingleNode(".//tr[1]/td[2]").SelectNodes(".//span"),
                GenreNodes = MangaProperties.SelectSingleNode(".//tr[4]/td[2]").SelectNodes(".//a/span");
            String[] AlternateNames = { },
                Authors = { HtmlEntity.DeEntitize(MangaProperties.SelectSingleNode(".//tr[2]/td[2]/a").InnerText) },
                Artists = { HtmlEntity.DeEntitize(MangaProperties.SelectSingleNode(".//tr[3]/td[2]/a").InnerText) },
                Genres = { };
            if (AlternateNameNodes != null && AlternateNameNodes.Count > 0)
                AlternateNames = (from HtmlNode AltNameNode in AlternateNameNodes select HtmlEntity.DeEntitize(AltNameNode.InnerText.Trim())).ToArray();
            if (GenreNodes != null && GenreNodes.Count > 0)
                Genres = (from HtmlNode GenreNode in GenreNodes select HtmlEntity.DeEntitize(GenreNode.InnerText.Trim())).ToArray();

            List<ChapterObject> Chapters = new List<ChapterObject>();
            HtmlNodeCollection ChapterNodes = ChapterListing.SelectNodes(String.Format(".//tr[contains(@class,'lang_{0} chapter_row')]", ISEA.Language));
            if (ChapterNodes != null && ChapterNodes.Count > 0)
            {
                foreach (HtmlNode ChapterNode in ChapterNodes)
                {
                    HtmlNode VolChapNameNode = ChapterNode.SelectSingleNode("td[1]/a");
                    Match VolChapMatch = Regex.Match(VolChapNameNode.InnerText, @"(Vol\.(?<Volume>\d+)\s)?(Ch\.(?<Chapter>\d+))(\.(?<SubChapter>\d+))?");
                    String ChapterName = VolChapNameNode.InnerText.Substring(VolChapMatch.Length + 2).Trim(),
                        ReleaseData = ReleaseData = ChapterNode.SelectSingleNode("td[5]").InnerText;
                    ChapterObject PrevChapter = Chapters.LastOrDefault();
                    Int32 Volume = 0, Chapter = 0, SubChapter = 0;
                    if (VolChapMatch.Groups["Volume"].Success)
                        Int32.TryParse(VolChapMatch.Groups["Volume"].Value, out Volume);
                    if (VolChapMatch.Groups["Chapter"].Success)
                        Int32.TryParse(VolChapMatch.Groups["Chapter"].Value, out Chapter);
                    if (VolChapMatch.Groups["SubChapter"].Success)
                        Int32.TryParse(VolChapMatch.Groups["SubChapter"].Value, out SubChapter);

                    DateTime Released = DateTime.MinValue;
                    if (ReleaseData.Contains("-"))
                        ReleaseData = ReleaseData.Split(new String[] { " - " }, StringSplitOptions.RemoveEmptyEntries)[0];
                    DateTime.TryParse(ReleaseData, out Released);
                    ChapterObject chapterObject = new ChapterObject()
                    {
                        MangaName = MangaName,
                        Name = HtmlEntity.DeEntitize(ChapterName),
                        Volume = Volume,
                        Chapter = Chapter,
                        SubChapter = SubChapter,
                        Released = Released,
                        Locations = {
                            new LocationObject() { 
                                ExtensionName = ISEA.Name,
                                Url = VolChapNameNode.Attributes["href"].Value }
                        }
                    };
                    if (!Chapters.Any(o => o.Chapter == chapterObject.Chapter && (o.SubChapter - chapterObject.SubChapter).InRange(-4, 4)))
                        Chapters.Add(chapterObject);
                    else
                        Chapters.Find(o => o.Chapter == chapterObject.Chapter && (o.SubChapter - chapterObject.SubChapter).InRange(-4, 4)).Merge(chapterObject);
                }
            }
            Chapters.Reverse();

            Double Rating = -1;
            try
            {
                HtmlNode RatingNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'rating')]");
                String RatingText = new String(RatingNode.InnerText.Trim().Substring(1, 4).Where(IsValidRatingChar).ToArray());
                Double.TryParse(RatingText, out Rating);
            }
            catch { }

            return new MangaObject()
            {
                Name = MangaName,
                MangaType = MangaType,
                PageFlowDirection = PageFlowDirection,
                Description = HtmlEntity.DeEntitize(Desciption),
                AlternateNames = AlternateNames.ToList(),
                Covers = new List<String>(new String[] { MangaCover }),
                Authors = Authors.ToList(),
                Artists = Artists.ToList(),
                Genres = Genres.ToList(),
                Released = (Chapters.FirstOrDefault() ?? new ChapterObject()).Released,
                Chapters = Chapters,
                Rating = Rating
            };
        }

        protected Boolean IsValidRatingChar(Char c)
        {
            return Char.IsDigit(c) || c.Equals('.');
        }

        public ChapterObject ParseChapterObject(string content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(content);
            return new ChapterObject()
            {
                Pages = (from HtmlNode PageNode in ChapterObjectDocument.GetElementbyId("page_select").SelectNodes(".//option")
                         select new PageObject()
                             {
                                 Url = PageNode.Attributes["value"].Value,
                                 PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText.Substring(5))
                             }).ToList()
            };
        }

        public PageObject ParsePageObject(string content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            HtmlNode PageNode = PageObjectDocument.GetElementbyId("page_select").SelectSingleNode(".//option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option");

            Uri ImageLink = new Uri(PageObjectDocument.GetElementbyId("comic_page").Attributes["src"].Value);

            return new PageObject()
            {
                Name = ImageLink.Segments.Last(),
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText.Substring(5)),
                Url = PageNode.Attributes["value"].Value,
                NextUrl = (NextNode != null) ? NextNode.Attributes["value"].Value : null,
                PrevUrl = (PrevNode != null) ? PrevNode.Attributes["value"].Value : null,
                ImgUrl = ImageLink.ToString()
            };
        }

        public List<SearchResultObject> ParseSearch(string content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();

            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes("//table[contains(@class,'ipb_table chapters_list')]/tbody/tr[not(contains(@class,'header'))]");
            if (HtmlSearchResults != null)
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    HtmlNode NameLink = SearchResultNode.SelectSingleNode(".//td[1]/strong/a");
                    if (NameLink != null)
                    {
                        String Name = HtmlEntity.DeEntitize(NameLink.InnerText).Trim(),
                            Link = NameLink.Attributes["href"].Value;
                        String[] Author_Artists = { SearchResultNode.SelectSingleNode(".//td[2]").InnerText.Trim() };
                        SearchResults.Add(new SearchResultObject()
                        {
                            CoverUrl = null,
                            ExtensionName = ISEA.Name,
                            Name = Name,
                            Url = Link,
                            Id = -1,
                            Rating = Double.Parse(SearchResultNode.SelectSingleNode(".//td[3]/div").Attributes["title"].Value.Substring(0, 4)),
                            Artists = Author_Artists.ToList(),
                            Authors = Author_Artists.ToList()
                        });
                    }
                }
            return SearchResults;
        }
    }
}
