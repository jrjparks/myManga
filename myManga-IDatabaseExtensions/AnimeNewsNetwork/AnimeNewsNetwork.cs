using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace AnimeNewsNetwork
{
    [IExtensionDescription(
        Name = "AnimeNewsNetwork",
        URLFormat = "animenewsnetwork.com",
        RefererHeader = "http://cdn.animenewsnetwork.com/",
        RootUrl = "http://cdn.animenewsnetwork.com",
        SupportedObjects = SupportedObjects.All,
        Author = "James Parks",
        Version = "0.0.1",
        Language = "English")]
    public sealed class AnimeNewsNetwork : IDatabaseExtension
    {
        #region IExtesion
        private IExtensionDescriptionAttribute EDA;
        public IExtensionDescriptionAttribute ExtensionDescriptionAttribute
        { get { return EDA ?? (EDA = GetType().GetCustomAttribute<IExtensionDescriptionAttribute>(false)); } }

        private Icon extensionIcon;
        public Icon ExtensionIcon
        {
            get
            {
                if (Equals(extensionIcon, null)) extensionIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                return extensionIcon;
            }
        }

        public CookieCollection Cookies
        { get; private set; }

        public Boolean IsAuthenticated
        { get; private set; }

        public bool Authenticate(NetworkCredential credentials, CancellationToken ct, IProgress<Int32> ProgressReporter)
        {
            if (IsAuthenticated) return true;
            throw new NotImplementedException();
        }

        public void Deauthenticate()
        {
            if (!IsAuthenticated) return;
            Cookies = null;
            IsAuthenticated = false;
        }

        public List<MangaObject> GetUserFavorites()
        {
            throw new NotImplementedException();
        }

        public bool AddUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }

        public bool RemoveUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }
        #endregion

        public SearchRequestObject GetSearchRequestObject(string searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/encyclopedia/api.xml?manga=~{1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader,
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
            List<LocationObject> Covers = new List<LocationObject>();
            if (CoverNode != null)
                Covers.Add(new LocationObject() { Url = CoverNode.Attributes["src"].Value, ExtensionName = ExtensionDescriptionAttribute.Name });

            return new DatabaseObject()
            {
                Name = HtmlEntity.DeEntitize(NameNode.InnerText),
                Covers = Covers,
                AlternateNames = (AlternateNameNodes != null) ? (from HtmlNode InfoNode in AlternateNameNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Genres = (GenreNodes != null) ? (from HtmlNode InfoNode in GenreNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Locations = { new LocationObject() {
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    Url = String.Format("{0}/encyclopedia/api.xml?manga={1}", ExtensionDescriptionAttribute.RootUrl, DatabaseObjectDocument.DocumentNode.SelectSingleNode("//manga[@id]").Attributes["id"].Value) } },
                Staff = (StaffNodes != null) ? (from HtmlNode InfoNode in StaffNodes select HtmlEntity.DeEntitize(InfoNode.InnerText.Trim())).ToList() : new List<String>(),
                Description = (DescriptionNode != null) ? HtmlEntity.DeEntitize(DescriptionNode.InnerText.Trim()) : String.Empty,
                ReleaseYear = Int32.Parse(ReleaseNode.FirstChild.InnerText.Substring(0, 4))
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            if (content.StartsWith("<ann>") && !content.Contains("warning"))
            {
                DatabaseObjectDocument.LoadHtml(content);
                return (from HtmlNode MangaNode 
                        in DatabaseObjectDocument.DocumentNode.SelectNodes("//manga")
                        select ParseDatabaseObject(MangaNode.OuterHtml)).ToList();
            }
            return new List<DatabaseObject>();
        }

        List<SearchResultObject> IExtension.ParseSearch(string Content)
        { throw new NotImplementedException("Database extensions return DatabaseObjects"); }
    }
}
