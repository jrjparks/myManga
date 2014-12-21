using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Enums;

namespace AnimeNewsNetwork
{
    [IDatabaseExtensionDescription(
        "AnimeNewsNetwork",
        "animenewsnetwork.com",
        "http://cdn.animenewsnetwork.com/",
        RootUrl = "http://cdn.animenewsnetwork.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class AnimeNewsNetwork : IDatabaseExtension
    {
        protected IDatabaseExtensionDescriptionAttribute _DatabaseExtensionDescriptionAttribute;
        public IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute
        { get { return _DatabaseExtensionDescriptionAttribute ?? (_DatabaseExtensionDescriptionAttribute = GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(string searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/encyclopedia/api.xml?manga=~{1}", DatabaseExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = DatabaseExtensionDescriptionAttribute.RefererHeader,
            };
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            HtmlNode NameNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Main title')]"),
                CoverNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Picture')]/img[last()]"),
                DescriptionNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Plot Summary')]"),
                ReleaseNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Vintage')]");
            HtmlNodeCollection AlternateNameNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Alternative title')]"),
                GenreNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Genres')]"),
                StaffNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//staff/person");
            List<String> Covers = new List<String>();
            if (CoverNode != null)
                Covers.Add(CoverNode.Attributes["src"].Value);

            return new DatabaseObject()
            {
                Name = HtmlEntity.DeEntitize(NameNode.InnerText),
                Covers = Covers,
                AlternateNames = (AlternateNameNodes != null) ? (from HtmlNode InfoNode in AlternateNameNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Genres = (GenreNodes != null) ? (from HtmlNode InfoNode in GenreNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Locations = { new LocationObject() { 
                    ExtensionName = DatabaseExtensionDescriptionAttribute.Name, 
                    Url = String.Format("{0}/encyclopedia/api.xml?manga={1}", DatabaseExtensionDescriptionAttribute.RootUrl, DatabaseObjectDocument.DocumentNode.SelectSingleNode("//manga[@id]").Attributes["id"].Value) } },
                Staff = (StaffNodes != null) ? (from HtmlNode InfoNode in StaffNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Description = (DescriptionNode != null) ? HtmlEntity.DeEntitize(DescriptionNode.InnerText.Trim()) : String.Empty,
                ReleaseYear = Int32.Parse(ReleaseNode.FirstChild.InnerText.Substring(0,4))
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            if (!content.Contains("warning"))
            {
                DatabaseObjectDocument.LoadHtml(content);
                return (from HtmlNode MangaNode in DatabaseObjectDocument.DocumentNode.SelectNodes("//manga") select ParseDatabaseObject(MangaNode.OuterHtml)).ToList();
            }
            return new List<DatabaseObject>();
        }
    }
}
