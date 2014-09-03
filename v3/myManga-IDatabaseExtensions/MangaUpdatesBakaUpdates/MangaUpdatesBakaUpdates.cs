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

namespace MangaUpdatesBakaUpdates
{
    [IDatabaseExtensionDescription(
        "AnimeNewsNetwork",
        "animenewsnetwork.com",
        "http://www.mangaupdates.com/",
        RootUrl = "http://www.mangaupdates.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public class MangaUpdatesBakaUpdates
    {
        protected IDatabaseExtensionDescriptionAttribute idea;
        protected virtual IDatabaseExtensionDescriptionAttribute IDEA { get { return idea ?? (idea = GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            return String.Format("{0}/series.html?stype=title&search={1}", IDEA.RootUrl, searchTerm);
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            return null;
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
