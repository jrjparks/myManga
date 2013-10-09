using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Core.Other;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Objects;

namespace MangaReader
{
    [ISiteExtensionDescription("MangaReader", "mangareader.net", "http://www.mangareader.net/", Author = "James Parks", Version = "0.0.1", SupportedObjects = SupportedObjects.Search)]
    public class MangaReader : ISiteExtension
    {
        public String GetSearchUri(String searchTerm)
        {
            return String.Format("http://www.mangareader.net/search/?w={0}", Uri.EscapeUriString(searchTerm));
        }

        public MangaObject ParseMangaObject(String content)
        {
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            String MangaCover = MangaObjectDocument.GetElementbyId("mangaimg").SelectSingleNode(".//img").Attributes["src"].Value;
            HtmlNode MangaProperties = MangaObjectDocument.GetElementbyId("mangaproperties").SelectSingleNode(".//table");
            HtmlNode ChapterListing = MangaObjectDocument.GetElementbyId("listing");

            String Name = MangaProperties.SelectSingleNode(".//tr[1]/td[2]/h2").InnerText,
                Release = String.Format("01/01/{0}", MangaProperties.SelectSingleNode(".//tr[3]/td[2]").InnerText);
            String[] AlternateNames = MangaProperties.SelectSingleNode(".//tr[2]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Authors = MangaProperties.SelectSingleNode(".//tr[5]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Artists = MangaProperties.SelectSingleNode(".//tr[6]/td[2]").InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                Genres = (from HtmlNode GenreNode in MangaProperties.SelectSingleNode(".//tr[8]/td[2]").SelectNodes(".//span[contains(@class,'genretags')]") select GenreNode.InnerText).ToArray();

            ChapterObject[] Chapters = (from HtmlNode ChapterNode in ChapterListing.SelectNodes(".//tr[not(contains(@class,'table_head'))]")
                                        select new ChapterObject()
                                            {
                                                Name = ChapterNode.SelectSingleNode(".//td[1]").LastChild.InnerText.Substring(3).Trim(),
                                                Chapter = Int32.Parse(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.Substring(ChapterNode.SelectSingleNode(".//td[1]/a").InnerText.LastIndexOf(' ') + 1)),
                                                Locations = { 
                                                    new LocationObject() { 
                                                        SiteExtensionName = "MangaReader", 
                                                        Url = String.Format("http://www.mangareader.net{0}", ChapterNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value) } 
                                                },
                                                Released = DateTime.Parse(ChapterNode.SelectSingleNode(".//td[2]").InnerText)
                                            }).ToArray();

            return new MangaObject()
            {
                Name = Name,
                AlternateNames = AlternateNames.ToList(),
                Covers = { MangaCover },
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
            };
        }

        public PageObject ParsePageObject(String content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            return new PageObject()
            {
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
                        CoverUrl = CoverUrl,
                        Name = Name,
                        Url = String.Format("http://www.mangareader.net{0}", Link),
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
