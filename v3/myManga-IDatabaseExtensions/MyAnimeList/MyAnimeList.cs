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

namespace MyAnimeList
{
    [IDatabaseExtension(
           "MyAnimeList",
           "myanimelist.net",
           "http://myanimelist.net/",
           RootUrl = "http://myanimelist.net",
           Author = "James Parks",
           Version = "0.0.1",
           Language = "English")]
    public class MyAnimeList : IDatabaseExtension
    {
        protected IDatabaseExtensionAttribute idea;
        protected virtual IDatabaseExtensionAttribute IDEA { get { return idea ?? (idea = GetType().GetCustomAttribute<IDatabaseExtensionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            return String.Format("{0}/manga.php?q={1}", IDEA.RootUrl, searchTerm);
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);
            HtmlNode ContentWrapperNode = DatabaseObjectDocument.GetElementbyId("contentWrapper"),
                ContentNode = DatabaseObjectDocument.GetElementbyId("content");

            return new DatabaseObject()
            {
                Name = ContentWrapperNode.SelectSingleNode("//h1/#text").InnerText,
                Covers = { ContentNode.SelectSingleNode("//table/tbody/tr/td[1]/div[1]/a/img").Attributes["src"].Value },
                AlternateNames = (from HtmlNode AltNameNode in ContentNode.SelectNodes("//table/tbody/tr/td[1]/div[contains(@class,'spaceit_pad')]") select AltNameNode.SelectSingleNode("//#text").InnerText.Trim()).ToList(),
                Genres = (from HtmlNode GenreNode in ContentNode.SelectNodes("//table/tbody/tr/td[1]/div[11]/a") select GenreNode.InnerText).ToList(),
                Locations = { },
                Staff = { },
                Description = ContentNode.SelectSingleNode("//table/tbody/tr/td[2]/div[2]/table/tbody/tr[1]/td").InnerText.Replace("<br>","\n").Trim()
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            throw new NotImplementedException();
        }
    }
}
