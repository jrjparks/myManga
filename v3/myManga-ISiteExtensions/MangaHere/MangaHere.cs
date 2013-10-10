using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Other;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
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
        ISiteExtensionDescriptionAttribute isea;
        private ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = this.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            return String.Format("{0}/search.php?name={1}", ISEA.RootUrl, searchTerm);
        }

        public MangaObject ParseMangaObject(string content)
        {
            throw new NotImplementedException();
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

            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class,'result_search')]/dl");
            if (HtmlSearchResults != null)
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    SearchResults.Add(new SearchResultObject()
                    {
                        Name = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["rel"].Value,
                        Url = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["href"].Value,
                    });
                }

            return SearchResults;
        }
    }
}
