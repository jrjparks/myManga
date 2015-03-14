using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System.Text;

namespace MangaTraders
{
    [ISiteExtensionDescription(
        "MangaTraders",
        "mangatraders.org",
        "http://mangatraders.org/",
        RootUrl = "http://mangatraders.org",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public sealed class MangaTraders : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute _SiteExtensionDescriptionAttribute;
        public ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute
        { get { return _SiteExtensionDescriptionAttribute ?? (_SiteExtensionDescriptionAttribute = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/advanced-search/result.php?seriesName={1}", SiteExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = SiteExtensionDescriptionAttribute.RefererHeader
            };
        }

        public MangaObject ParseMangaObject(String content)
        {
            Int32 MangaInformationContentStart = content.IndexOf("<!-- Intro Series -->"),
                MangaInformationContentEnd = content.IndexOf("<!-- **END: Intro Series -->", MangaInformationContentStart);
            String MangaInformationContent = content.Substring(MangaInformationContentStart, MangaInformationContentEnd - MangaInformationContentStart);

            Int32 MangaChaptersContentStart = content.IndexOf("<!-- Main Content -->"),
                MangaChaptersContentEnd = content.IndexOf("<!-- **END: Main Content -->", MangaChaptersContentStart);
            String MangaChaptersContent = content.Substring(MangaChaptersContentStart, MangaChaptersContentEnd - MangaChaptersContentStart);

            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(MangaInformationContent);

            HtmlNode MangaObjectNode = MangaObjectDocument.DocumentNode.SelectSingleNode(".//div/div");

            String MangaName = String.Empty;

            foreach (HtmlNode DetailNode in MangaObjectNode.SelectNodes(".//div[2]/div"))
            {
                HtmlNode DetailTypeNode = DetailNode.SelectSingleNode(".//div[1]/b");
                String DetailType = (DetailTypeNode != null) ? DetailTypeNode.InnerText.Trim().TrimEnd(':') : "MangaName",
                    DetailValue = (DetailTypeNode != null) ? DetailNode.SelectSingleNode(".//div[1]/#text[2]").InnerText.Trim() : DetailNode.SelectSingleNode(".//div[1]/h1").InnerText.Trim();

                switch (DetailType)
                {
                    default: break;
                    case "MangaName": MangaName = DetailValue; break;
                }
            }


            String Cover = SiteExtensionDescriptionAttribute.RootUrl + MangaObjectNode.SelectSingleNode(".//div[1]/img/@src").Attributes["src"].Value,
                Desciption = HtmlEntity.DeEntitize(MangaObjectNode.SelectSingleNode(".//div[2]/div[6]/div/div").InnerText);
            String[] AlternateNames = { },
                Authors = (from HtmlNode AuthorNode in MangaObjectNode.SelectNodes(".//div[2]/div[3]/div/a") select AuthorNode.InnerText.Trim()).ToArray(),
                Genres = (from HtmlNode GenreNode in MangaObjectNode.SelectNodes(".//div[2]/div[6]/div/a") select GenreNode.InnerText.Trim()).ToArray();

            List<ChapterObject> Chapters = new List<ChapterObject>();
            MangaObjectDocument.LoadHtml(MangaChaptersContent);
            HtmlNodeCollection RawChapterList = MangaObjectDocument.DocumentNode.SelectNodes(".//div[contains(@class,'row')]");
            foreach (HtmlNode RawChapterNode in RawChapterList)
            {
                HtmlNode ChapterNumberNode = RawChapterNode.SelectSingleNode(".//div[1]/a"),
                    ReleaseDate = RawChapterNode.SelectSingleNode(".//div[2]/time");
                String[] ChapterSub = ChapterNumberNode.InnerText.Substring("Chapter".Length).Trim().Split('.');

                ChapterObject Chapter = new ChapterObject()
                {
                    Name = HtmlEntity.DeEntitize(RawChapterNode.SelectSingleNode(".//div[1]/gray").InnerText),
                    Chapter = UInt32.Parse(ChapterSub[0]),
                    Locations =
                    {
                        new LocationObject(){
                            ExtensionName = SiteExtensionDescriptionAttribute.Name, 
                            Url = SiteExtensionDescriptionAttribute.RootUrl + ChapterNumberNode.Attributes["href"].Value
                        },
                    },
                    Released = ReleaseDate.InnerText.ToLower().StartsWith("today") ? DateTime.Today : ReleaseDate.InnerText.ToLower().StartsWith("yesterday") ? DateTime.Today.AddDays(-1) : DateTime.Parse(ReleaseDate.InnerText.ToLower())
                };
                if (ChapterSub.Length == 2)
                    Chapter.SubChapter = UInt32.Parse(ChapterSub[1]);
                Chapters.Add(Chapter);
            }

            MangaObject MangaObj = new MangaObject()
            {
                Name = MangaName,
                Description = Desciption,
                AlternateNames = AlternateNames.ToList(),
                Covers = { Cover },
                Authors = Authors.ToList(),
                Artists = Authors.ToList(),
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
            throw new NotImplementedException();
        }

        public PageObject ParsePageObject(string content)
        {
            throw new NotImplementedException();
        }

        public List<SearchResultObject> ParseSearch(string content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();

            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);
            HtmlWeb HtmlWeb = new HtmlWeb();
            // TODO: Complete this
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class,'mainContainer')]/div/div/div[contains(@class,'well')]");

            return SearchResults;
        }
    }
}
