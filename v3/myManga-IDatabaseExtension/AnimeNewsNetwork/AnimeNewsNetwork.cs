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

            return new DatabaseObject()
            {
                Name = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Main title')]").InnerText,
                Covers = { DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Picture')]/img[last()]").Attributes["src"].Value },
                AlternateNames = (from HtmlNode InfoNode in DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Alternative title')]") select InfoNode.InnerText).ToList(),
                Genres = (from HtmlNode InfoNode in DatabaseObjectDocument.DocumentNode.SelectNodes("//info[contains(@type,'Genres')]") select InfoNode.InnerText).ToList(),
                Locations = { new LocationObject() { 
                    ExtensionName = "AnimeNewsNetwork", 
                    Url = String.Format("{0}/encyclopedia/api.xml?manga={1}", IDEA.RootUrl, DatabaseObjectDocument.DocumentNode.SelectSingleNode("//manga[@id]").Attributes["id"].Value) } },
                Staff = (from HtmlNode InfoNode in DatabaseObjectDocument.DocumentNode.SelectNodes("//staff/person") select InfoNode.InnerText).ToList(),
                Description = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//info[contains(@type,'Plot Summary')]").InnerText
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);
            return (from HtmlNode MangaNode in DatabaseObjectDocument.DocumentNode.SelectNodes("//manga") select ParseDatabaseObject(MangaNode.OuterHtml)).ToList();
        }
    }
}
