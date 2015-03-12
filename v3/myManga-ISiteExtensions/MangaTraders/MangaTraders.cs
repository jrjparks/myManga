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
            HtmlWeb HtmlWeb = new HtmlWeb();
            // TODO: Complete this
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class,'mainContainer')]/div/div/div[contains(@class,'well')]");

            return SearchResults;
        }
    }
}
