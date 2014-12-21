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

namespace MangaHelpers
{
    [IDatabaseExtensionDescription(
        "MangaHelpers",
        "mangahelpers.com",
        "http://mangahelpers.com/",
        RootUrl = "http://mangahelpers.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaHelpers : IDatabaseExtension
    {
        protected IDatabaseExtensionDescriptionAttribute _DatabaseExtensionDescriptionAttribute;
        public IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute
        { get { return _DatabaseExtensionDescriptionAttribute ?? (_DatabaseExtensionDescriptionAttribute = GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/manga/browse/?q={1}", DatabaseExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = DatabaseExtensionDescriptionAttribute.RefererHeader,
            };
        }

        public DatabaseObject ParseDatabaseObject(String content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            HtmlNode ContentNode = DatabaseObjectDocument.GetElementbyId("content"),
                InformationNode = ContentNode.SelectSingleNode(".//div[contains(@class,'subtabbox')]"),
                CoverNode = InformationNode.SelectSingleNode(".//div[2]/img"),
                DetailsNode = InformationNode.SelectSingleNode(".//table[contains(@class,'details')]"),
                SummaryNode = DatabaseObjectDocument.GetElementbyId("summary").SelectSingleNode(".//p"),
                TagsNode = DatabaseObjectDocument.GetElementbyId("tags").SelectSingleNode(".//p"),
                LocationNode = ContentNode.SelectSingleNode(".//div[contains(@class,'tab selected')]/a");

            String Name = InformationNode.SelectSingleNode(".//h1").InnerText;
            List<String> Genres = (from String Tag in TagsNode.InnerText.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) select Tag.Trim()).ToList(),
                AlternateNames = new List<String>(),
                Staff = new List<String>();
            Int32 ReleaseYear = 0;

            LocationObject Location = new LocationObject()
            {
                ExtensionName = DatabaseExtensionDescriptionAttribute.Name,
                Url = String.Format("{0}{1}", DatabaseExtensionDescriptionAttribute.RootUrl, LocationNode.Attributes["href"].Value),
            };

            foreach (HtmlNode DetailNode in DetailsNode.SelectNodes(".//tr"))
            {
                String DetailType = DetailNode.SelectSingleNode(".//td[1]").InnerText.Trim().TrimEnd(':'),
                    DetailValue = DetailNode.SelectSingleNode(".//td[2]").InnerText.Trim();
                switch (DetailType)
                {
                    default:
                    case "":
                        break;

                    case "Original title":
                    case "Alternative Titles":
                        foreach (String value in DetailValue.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            if (!AlternateNames.Contains(value)) AlternateNames.Add(value);
                        break;

                    case "Writer(s)":
                    case "Artist(s)":
                        foreach (String value in DetailValue.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            if (!Staff.Contains(value)) Staff.Add(value);
                        break;

                    case "Year":
                        Int32.TryParse(DetailValue, out ReleaseYear);
                        break;
                }
            }

            List<String> Covers = new List<String>();
            if (CoverNode != null) Covers.Add(String.Format("{0}{1}", DatabaseExtensionDescriptionAttribute.RootUrl, CoverNode.Attributes["src"].Value));

            return new DatabaseObject()
            {
                Name = Name,
                AlternateNames = AlternateNames,
                Covers = Covers,
                Description = SummaryNode.InnerText,
                Genres = Genres,
                Locations = { Location },
                Staff = Staff,
                ReleaseYear = ReleaseYear,
            };
        }

        public List<DatabaseObject> ParseSearch(String content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);
            HtmlWeb HtmlWeb = new HtmlWeb();
            HtmlNode TableResultsNode = DatabaseObjectDocument.GetElementbyId("results");
            if (TableResultsNode.InnerText.Contains("No results")) return new List<DatabaseObject>();
            return (from HtmlNode MangaNode in TableResultsNode.SelectNodes(".//tr")
                    where MangaNode.SelectSingleNode(".//td[1]/a") != null
                    select ParseDatabaseObject(HtmlWeb.Load(String.Format("{0}{1}", DatabaseExtensionDescriptionAttribute.RootUrl, MangaNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value)).DocumentNode.OuterHtml)
                    ).ToList();
        }
    }
}