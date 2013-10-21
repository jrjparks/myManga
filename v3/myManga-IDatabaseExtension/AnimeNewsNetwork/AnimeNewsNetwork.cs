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

namespace AnimeNewsNetwork
{
    [IDatabaseExtension(
        "AnimeNewsNetwork",
        "animenewsnetwork.com",
        "http://cdn.animenewsnetwork.com/",
        RootUrl = "http://cdn.animenewsnetwork.com",
        Author = "James Parks",
        Version = "0.0.1",
        Language = "English")]
    public class AnimeNewsNetwork : IDatabaseExtension
    {
        protected IDatabaseExtensionAttribute idea;
        protected virtual IDatabaseExtensionAttribute IDEA { get { return idea ?? (idea = GetType().GetCustomAttribute<IDatabaseExtensionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            return String.Format("{0}/encyclopedia/api.xml?manga=~{1}", IDEA.RootUrl, searchTerm);
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            HtmlNode NameNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Main title')]"),
                CoverNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Picture')]/img[last()]"),
                DescriptionNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Plot Summary')]");
            HtmlNodeCollection AlternateNameNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Alternative title')]"),
                GenreNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Genres')]"),
                StaffNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//staff/person");

            return new DatabaseObject()
            {
                Name = NameNode.InnerText,
                Covers = { CoverNode.Attributes["src"].Value },
                AlternateNames = (AlternateNameNodes != null) ? (from HtmlNode InfoNode in AlternateNameNodes select InfoNode.InnerText).ToList() : new List<String>(),
                Genres = (GenreNodes != null) ? (from HtmlNode InfoNode in GenreNodes select InfoNode.InnerText).ToList() : new List<String>(),
                Locations = { new LocationObject() { 
                    ExtensionName = "AnimeNewsNetwork", 
                    Url = String.Format("{0}/encyclopedia/api.xml?manga={1}", IDEA.RootUrl, DatabaseObjectDocument.DocumentNode.SelectSingleNode("//manga[@id]").Attributes["id"].Value) } },
                Staff = (StaffNodes != null) ? (from HtmlNode InfoNode in StaffNodes select InfoNode.InnerText).ToList() : new List<String>(),
                Description = (DescriptionNode != null) ? DescriptionNode.InnerText : String.Empty
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
