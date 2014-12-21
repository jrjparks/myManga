using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MangaUpdatesBakaUpdates
{
    [IDatabaseExtensionDescription(
        "MangaUpdatesBakaUpdates",
        "mangaupdates.com",
        "http://www.mangaupdates.com/",
        RootUrl = "http://www.mangaupdates.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaUpdatesBakaUpdates : IDatabaseExtension
    {
        protected Int32 PageCount = 30;
        protected IDatabaseExtensionDescriptionAttribute _DatabaseExtensionDescriptionAttribute;
        public IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute
        { get { return _DatabaseExtensionDescriptionAttribute ?? (_DatabaseExtensionDescriptionAttribute = GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(string searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/series.html?stype=title&search={1}&perpage={2}", DatabaseExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm), PageCount),
                Method = SearchMethod.GET,
                Referer = DatabaseExtensionDescriptionAttribute.RefererHeader,
            };
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            HtmlNode NameNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//span[contains(@class,'releasestitle')]");
            HtmlNodeCollection sCatNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//div[contains(@class,'sCat')]"),
                sContentNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//div[contains(@class,'sContent')]");

            Dictionary<String, HtmlNode> ContentNodes = sCatNodes.Zip(sContentNodes, (sCategory, sContent) => new { Category = sCategory.FirstChild.InnerText, Content = sContent }).ToDictionary(item => item.Category, item => item.Content);

            HtmlNode AssociatedNamesNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Associated Names")).Value,
                CoverNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Image")).Value,
                YearNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Year")).Value;

            List<String> AssociatedNames = (from HtmlNode TextNode in AssociatedNamesNode.ChildNodes where TextNode.Name.Equals("#text") && !TextNode.InnerText.Trim().Equals(String.Empty) && !TextNode.InnerText.Trim().Equals("N/A") select HtmlEntity.DeEntitize(TextNode.InnerText.Trim())).ToList<String>();
            
            List<String> Covers = new List<String>();
            if (CoverNode != null && CoverNode.SelectSingleNode(".//img") != null)
                Covers.Add(CoverNode.SelectSingleNode(".//img").Attributes["src"].Value);

            Match DatabaseObjectIdMatch = Regex.Match(content, @"id=(?<DatabaseObjectId>\d+)&");
            Int32 DatabaseObjectId = Int32.Parse(DatabaseObjectIdMatch.Groups["DatabaseObjectId"].Value),
                ReleaseYear = 0;
            Int32.TryParse(YearNode.FirstChild.InnerText, out ReleaseYear);
            return new DatabaseObject()
            {
                Name = HtmlEntity.DeEntitize(NameNode.InnerText),
                Covers = Covers,
                AlternateNames = AssociatedNames,
                Description = HtmlEntity.DeEntitize(ContentNodes.FirstOrDefault(item => item.Key.Equals("Description")).Value.InnerText.Trim()),
                Locations = { new LocationObject() { 
                    ExtensionName = DatabaseExtensionDescriptionAttribute.Name,
                    Url = String.Format("{0}/series.html?id={1}", DatabaseExtensionDescriptionAttribute.RootUrl, DatabaseObjectId) } },
                ReleaseYear = ReleaseYear
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            if (!content.Contains("There are no series in the database"))
            {
                DatabaseObjectDocument.LoadHtml(content);
                HtmlWeb HtmlWeb = new HtmlWeb();
                HtmlNode TableSeriesNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//table[contains(@class,'series_rows_table')]");
                return (from HtmlNode MangaNode in TableSeriesNode.SelectNodes(".//tr[not(@valign='top')]").Skip(2).Take(PageCount) where MangaNode.SelectSingleNode(".//td[1]/a") != null select ParseDatabaseObject(HtmlWeb.Load(MangaNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value).DocumentNode.OuterHtml)).ToList();
            }
            return new List<DatabaseObject>();
        }
    }
}
