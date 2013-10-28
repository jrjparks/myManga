using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;

namespace MangaHere
{
    [ISiteExtensionDescription(
        "MangaHere",
        "mangahere.com",
        "http://www.mangahere.com/",
        RootUrl = "http://www.mangahere.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaHere : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute isea;
        protected virtual ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            return String.Format("{0}/search.php?name={1}", ISEA.RootUrl, searchTerm);
        }

        public MangaObject ParseMangaObject(string content)
        {
            if (content.ToLower().Contains("has been licensed, it is not available in MangaHere.".ToLower()))
                return null;
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            HtmlNode TitleNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//h1[contains(@class,'title')]"),
                MangaDetailsNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'manga_detail')]"),
                MangaPropertiesNode = MangaDetailsNode.SelectSingleNode(".//div[1]"),
                MangaDesciptionNode = MangaObjectDocument.GetElementbyId("show"),
                AuthorsNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[5]"),
                ArtistsNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[6]"),
                GenresNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[4]");
            HtmlNodeCollection AuthorsNodeCollection = AuthorsNode.SelectNodes(".//a"),
                ArtistsNodeCollection = ArtistsNode.SelectNodes(".//a");
            String Desciption = MangaDesciptionNode != null ? MangaDesciptionNode.FirstChild.InnerText : String.Empty;

            String[] AlternateNames = MangaPropertiesNode.SelectSingleNode(".//ul/li[3]").LastChild.InnerText.Split(new String[] { "; " }, StringSplitOptions.RemoveEmptyEntries),
                Authors = { }, Artists = { },
                Genres = MangaPropertiesNode.SelectSingleNode(".//ul/li[4]").LastChild.InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (AuthorsNodeCollection != null)
                Authors = (from HtmlNode AuthorNode in AuthorsNodeCollection select AuthorNode.InnerText).ToArray();
            if (ArtistsNodeCollection != null)
                Artists = (from HtmlNode ArtistNode in ArtistsNodeCollection select ArtistsNode.InnerText).ToArray();

            List<ChapterObject> Chapters = new List<ChapterObject>();
            HtmlNodeCollection RawChapterList = MangaDetailsNode.SelectNodes(".//div[contains(@class,'detail_list')]/ul[1]/li");
            foreach (HtmlNode ChapterNode in RawChapterList)
            {
                String volNode = ChapterNode.SelectSingleNode(".//span[1]/span").InnerText;
                String[] volChapSub = { (volNode != null && volNode.Length > 0) ? volNode.Substring(4).Trim() : "0" };
                HtmlNode ChapterTitle = ChapterNode.SelectSingleNode(".//span[1]/a");
                String ChapterNumber = ChapterTitle.InnerText.Substring(ChapterTitle.InnerText.LastIndexOf(' ') + 1).Trim();
                volChapSub = volChapSub.Concat(ChapterNumber.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();

                ChapterObject Chapter = new ChapterObject()
                {
                    Name = ChapterTitle.InnerText.Trim(),
                    Volume = Int32.Parse(volChapSub[0]),
                    Chapter = Int32.Parse(volChapSub[1]),
                    Locations = { 
                        new LocationObject() { 
                                ExtensionName = ISEA.Name, 
                                Url = ChapterNode.SelectSingleNode(".//span[1]/a").Attributes["href"].Value } 
                        },
                    Released = ChapterNode.SelectSingleNode(".//span[2]").InnerText.ToLower().Equals("today") ? DateTime.Today : (ChapterNode.SelectSingleNode(".//span[2]").InnerText.ToLower().Equals("yesterday") ? DateTime.Today.AddDays(-1) : DateTime.Parse(ChapterNode.SelectSingleNode(".//span[2]").InnerText))
                };
                if (volChapSub.Length == 3)
                    Chapter.SubChapter = Int32.Parse(volChapSub[2]);
                Chapters.Add(Chapter);
            }
            Chapters.Reverse();

            MangaObject MangaObj = new MangaObject()
            {
                Name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(TitleNode.LastChild.InnerText.ToLower()),
                Description = Desciption,
                AlternateNames = AlternateNames.ToList(),
                Covers = { MangaPropertiesNode.SelectSingleNode(".//img[1]/@src").Attributes["src"].Value },
                Authors = Authors.ToList(),
                Artists = Artists.ToList(),
                Genres = Genres.ToList(),
                Released = Chapters.Last().Released,
                Chapters = Chapters
            };
            MangaObj.AlternateNames.RemoveAll(an => an.ToLower().Equals("none"));
            MangaObj.Genres.RemoveAll(g => g.ToLower().Equals("none"));
            return MangaObj;
        }

        public ChapterObject ParseChapterObject(string content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(content);

            return new ChapterObject()
            {
                Pages = (from HtmlNode PageNode in ChapterObjectDocument.DocumentNode.SelectNodes("//section[contains(@class,'readpage_top')]/div[contains(@class,'go_page')]/span/select/option")
                         select new PageObject()
                         {
                             Url = PageNode.Attributes["value"].Value,
                             PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText)
                         }).ToList()
            };
        }

        public PageObject ParsePageObject(string content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            HtmlNode PageNode = PageObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'go_page')]/span/select/option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option");

            String ImgSrc = PageObjectDocument.GetElementbyId("image").Attributes["src"].Value;
            ImgSrc = ImgSrc.Substring(0, ImgSrc.LastIndexOf('?'));
            Uri ImageLink = new Uri(ImgSrc);

            return new PageObject()
            {
                Name = ImageLink.Segments.Last(),
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText),
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

            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class,'result_search')]/dl");
            if (HtmlSearchResults != null && !HtmlSearchResults[0].InnerText.ToLower().Equals("No Manga Series".ToLower()))
            {
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    SearchResults.Add(new SearchResultObject()
                    {
                        Name = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["rel"].Value,
                        Url = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["href"].Value,
                        ExtensionName = ISEA.Name
                    });
                }
            }

            return SearchResults;
        }
    }
}
