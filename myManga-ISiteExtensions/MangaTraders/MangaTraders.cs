using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MangaTraders
{
    [IExtensionDescription(
        Name = "MangaTraders",
        URLFormat = "mangatraders.org",
        RefererHeader = "http://mangatraders.org/",
        RootUrl = "http://mangatraders.org",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public sealed class MangaTraders : ISiteExtension
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
            // DO NOT RETURN TRUE IF `IsAuthenticated`
            // ALLOW USERS TO REAUTHENTICATE
            // if (IsAuthenticated) return true;

            CookieContainer cookieContainer = new CookieContainer();
            HttpWebRequest request = HttpWebRequest.CreateHttp("http://mangatraders.org/login/process.php");
            request.Method = WebRequestMethods.Http.Post;

            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(5);
            ct.ThrowIfCancellationRequested();

            StringBuilder loginData = new StringBuilder();
            loginData.AppendUrlEncoded("email_Login", credentials.UserName, true);
            loginData.AppendUrlEncoded("password_Login", credentials.Password);

            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(10);
            ct.ThrowIfCancellationRequested();

            Byte[] loginDataBytes = Encoding.UTF8.GetBytes(loginData.ToString());
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = loginDataBytes.Length;
            request.CookieContainer = cookieContainer;

            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(30);
            ct.ThrowIfCancellationRequested();

            using (Stream requestStream = request.GetRequestStream())
            { requestStream.Write(loginDataBytes, 0, loginDataBytes.Length); }

            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(50);
            ct.ThrowIfCancellationRequested();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            String responseContent = null;
            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(75);
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            { responseContent = streamReader.ReadToEnd(); }

            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(90);
            ct.ThrowIfCancellationRequested();
            Cookies = response.Cookies;

            IsAuthenticated = responseContent.IndexOf("username or password incorrect", StringComparison.OrdinalIgnoreCase) < 0;
            if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
            return IsAuthenticated;
        }

        public void Deauthenticate()
        {
            if (!IsAuthenticated) return;
            CookieContainer cookieContainer = new CookieContainer();
            HttpWebRequest request = HttpWebRequest.CreateHttp("http://mangatraders.org/logout.php");
            request.Method = WebRequestMethods.Http.Get;
            request.CookieContainer = cookieContainer;
            request.CookieContainer.Add(Cookies);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
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

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/advanced-search/result.php?seriesName={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader
            };
        }

        public MangaObject ParseMangaObject(String content)
        {
            Int32 MangaInformationContentStart = content.IndexOf("<!-- Intro Series -->"),
                MangaInformationContentEnd = content.IndexOf("<!-- **END: Intro Series -->", MangaInformationContentStart);
            String MangaInformationContent = content.Substring(MangaInformationContentStart, MangaInformationContentEnd - MangaInformationContentStart);

            Int32 MangaChaptersContentStart = content.IndexOf("<!-- Main Content -->"),
                MangaChaptersContentEnd = content.IndexOf("<!-- **END: Main Content -->", MangaChaptersContentStart);
            String MangaChaptersContent = content.Substring(MangaChaptersContentStart, MangaChaptersContentEnd - MangaChaptersContentStart);

            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(MangaInformationContent);

            HtmlNode MangaObjectNode = MangaObjectDocument.DocumentNode.SelectSingleNode(".//div/div");

            String MangaName = String.Empty,
                Description = String.Empty;
            List<String> AlternateNames = new List<String>(),
                AuthorsArtists = new List<String>(),
                Genres = new List<String>();

            foreach (HtmlNode DetailNode in MangaObjectNode.SelectNodes(".//div[2]/div[contains(@class,'row')]"))
            {
                HtmlNode DetailTypeNode = DetailNode.SelectSingleNode(".//div[1]/b[1] | .//div[1]/strong[1]"),
                    DetailTextNode = (DetailTypeNode != null) ? DetailTypeNode.NextSibling : null,
                    DetailDescriptionNode = (DetailTextNode != null) ? DetailTextNode.NextSibling : null,
                    MangaNameNode = DetailNode.SelectSingleNode(".//div[1]/h1");
                HtmlNodeCollection DetailLinkNodes = DetailNode.SelectNodes(".//div[1]/a");
                String DetailType = (DetailTypeNode != null) ? DetailTypeNode.InnerText.Trim().TrimEnd(':') : "MangaName",
                    DetailValue = String.Empty;
                String[] DetailValues = { };
                if (DetailLinkNodes != null)
                {
                    DetailValues = (from HtmlNode LinkNode in DetailLinkNodes select LinkNode.InnerText).ToArray();
                }
                else if (MangaNameNode != null)
                {
                    DetailValue = HtmlEntity.DeEntitize(MangaNameNode.InnerText.Trim());
                }
                else if (DetailDescriptionNode != null)
                {
                    DetailValue = HtmlEntity.DeEntitize(DetailDescriptionNode.InnerText.Trim());
                }
                else if (DetailTextNode != null)
                {
                    DetailValue = HtmlEntity.DeEntitize(DetailTextNode.InnerText.Trim());
                }

                switch (DetailType)
                {
                    default: break;
                    case "MangaName": MangaName = DetailValue; break;
                    case "Alternate Names": AlternateNames = (from String AltName in DetailValue.Split(',') select AltName.Trim()).ToList(); break;
                    case "Author": AuthorsArtists = DetailValues.ToList(); break;
                    case "Genre": Genres = DetailValues.ToList(); break;
                    case "Description": Description = DetailValue; break;
                }
            }


            String Cover = ExtensionDescriptionAttribute.RootUrl + MangaObjectNode.SelectSingleNode(".//div[1]/img/@src").Attributes["src"].Value;

            List<ChapterObject> Chapters = new List<ChapterObject>();
            MangaObjectDocument.LoadHtml(MangaChaptersContent);
            HtmlNodeCollection RawChapterList = MangaObjectDocument.DocumentNode.SelectNodes(".//div[contains(@class,'row')]");
            foreach (HtmlNode RawChapterNode in RawChapterList.Skip(1))
            {
                HtmlNode ChapterNumberNode = RawChapterNode.SelectSingleNode(".//div[1]/a"),
                    ReleaseDate = RawChapterNode.SelectSingleNode(".//div[2]/time");
                String ChapterNumber = Regex.Match(ChapterNumberNode.InnerText, @"\d+(\.\d+)?").Value;
                String[] ChapterSub = ChapterNumber.Trim().Split('.');


                DateTime Released = DateTime.Now;
                String ReleasedTxt = ReleaseDate.InnerText.ToLower();
                if (ReleasedTxt.StartsWith("today"))
                    Released = DateTime.Today;
                else if (ReleasedTxt.StartsWith("yesterday"))
                    Released = DateTime.Today.AddDays(-1);
                else if (ReleasedTxt.EndsWith("hours ago"))
                {
                    Int32 hours = 0;
                    Int32.TryParse(ReleasedTxt.Split(' ')[0], out hours);
                    Released = DateTime.Now.AddHours(0 - hours);
                }
                else
                    Released = DateTime.Parse(ReleasedTxt);

                ChapterObject Chapter = new ChapterObject()
                {
                    Name = HtmlEntity.DeEntitize(RawChapterNode.SelectSingleNode(".//div[1]/gray").InnerText),
                    Chapter = UInt32.Parse(ChapterSub[0]),
                    Locations =
                    {
                        new LocationObject(){
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                            Url = ExtensionDescriptionAttribute.RootUrl + ChapterNumberNode.Attributes["href"].Value
                        },
                    },
                    Released = Released
                };
                if (ChapterSub.Length == 2)
                    Chapter.SubChapter = UInt32.Parse(ChapterSub[1]);
                Chapters.Add(Chapter);
            }
            Chapters.Reverse();
            MangaObject MangaObj = new MangaObject()
            {
                Name = MangaName,
                Description = Description,
                AlternateNames = AlternateNames.ToList(),
                CoverLocations = { new LocationObject() {
                    Url = Cover,
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    ExtensionLanguage = ExtensionDescriptionAttribute.Language } },
                Authors = AuthorsArtists.ToList(),
                Artists = AuthorsArtists.ToList(),
                Genres = Genres.ToList(),
                Released = Chapters.First().Released,
                Chapters = Chapters
            };
            MangaObj.AlternateNames.RemoveAll(an => an.ToLower().Equals("none"));
            MangaObj.Genres.RemoveAll(g => g.ToLower().Equals("none"));
            return MangaObj;
        }

        public ChapterObject ParseChapterObject(String content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(content);

            String ChapterUrl = ChapterObjectDocument.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value;
            ChapterUrl = ChapterUrl.Substring(0, ChapterUrl.LastIndexOf('/') + 1);
            return new ChapterObject()
            {
                Pages = (from HtmlNode PageNode in ChapterObjectDocument.GetElementbyId("changePageSelect").SelectNodes(".//option")
                         select new PageObject()
                         {
                             Url = ChapterUrl + PageNode.Attributes["value"].Value,
                             PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText.Substring("Page ".Length).Trim())
                         }).ToList()
            };
        }

        public PageObject ParsePageObject(String content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            String ChapterUrl = PageObjectDocument.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value;
            ChapterUrl = ChapterUrl.Substring(0, ChapterUrl.LastIndexOf('/') + 1);
            String[] ChapterUrlSections = ChapterUrl.Substring(ExtensionDescriptionAttribute.RootUrl.Length).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            HtmlNode PageNode = PageObjectDocument.GetElementbyId("changePageSelect").SelectSingleNode(".//option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option"),
                ImgNode = PageObjectDocument.DocumentNode.SelectSingleNode(String.Format("//a[contains(@href, '{0}')]/img", ChapterUrlSections[1]));

            String ImgSrc = ImgNode.Attributes["src"].Value;
            String Name = ImgSrc.Split('/').Last();

            return new PageObject()
            {
                Name = Name,
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText.Substring("Page ".Length).Trim()),
                Url = ChapterUrl + PageNode.Attributes["value"].Value,
                NextUrl = (NextNode != null) ? ChapterUrl + NextNode.Attributes["value"].Value : null,
                PrevUrl = (PrevNode != null) ? ChapterUrl + PrevNode.Attributes["value"].Value : null,
                ImgUrl = ImgSrc
            };
        }

        public List<SearchResultObject> ParseSearch(String content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();

            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);

            HtmlNode MainContainer = SearchResultDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'mainContainer')]");
            HtmlNodeCollection SearchResultNodes = MainContainer.SelectNodes(".//div[contains(@class,'well')]/div[contains(@class,'row') and (contains(@class,'available') or contains(@class,'unavailable'))]");

            if (!Equals(SearchResultNodes, null))
            {
                foreach (HtmlNode SearchResultNode in SearchResultNodes)
                {
                    String ImgUrl = ExtensionDescriptionAttribute.RootUrl + SearchResultNode.SelectSingleNode(".//img").Attributes["src"].Value.Substring(2),
                        Name = String.Empty,
                        Link = String.Empty;
                    LocationObject Cover = new LocationObject() {
                        Url = ImgUrl,
                        ExtensionName = ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = ExtensionDescriptionAttribute.Language
                    };
                    List<String> AlternateNames = new List<String>(),
                        AuthorsArtists = new List<String>(),
                        Genres = new List<String>();

                    foreach (HtmlNode DetailNode in SearchResultNode.SelectNodes(".//div[2]/div[contains(@class,'row')]"))
                    {
                        HtmlNode DetailTypeNode = DetailNode.SelectSingleNode(".//div[1]/b[1] | .//div[1]/strong[1]"),
                            DetailTextNode = (DetailTypeNode != null) ? DetailTypeNode.NextSibling : null,
                            DetailDescriptionNode = (DetailTextNode != null) ? DetailTextNode.NextSibling : null,
                            MangaNameNode = DetailNode.SelectSingleNode(".//div[1]/h1/a");
                        HtmlNodeCollection DetailLinkNodes = DetailNode.SelectNodes(".//div[1]/a");
                        String DetailType = (DetailTypeNode != null) ? DetailTypeNode.InnerText.Trim().TrimEnd(':') : "MangaName",
                            DetailValue = String.Empty;
                        String[] DetailValues = { };
                        if (DetailLinkNodes != null)
                        {
                            DetailValues = (from HtmlNode LinkNode in DetailLinkNodes select LinkNode.InnerText).ToArray();
                        }
                        else if (MangaNameNode != null)
                        {
                            DetailValue = HtmlEntity.DeEntitize(MangaNameNode.InnerText.Trim());
                        }
                        else if (DetailDescriptionNode != null)
                        {
                            DetailValue = HtmlEntity.DeEntitize(DetailDescriptionNode.InnerText.Trim());
                        }
                        else if (DetailTextNode != null)
                        {
                            DetailValue = HtmlEntity.DeEntitize(DetailTextNode.InnerText.Trim());
                        }

                        switch (DetailType)
                        {
                            default: break;
                            case "MangaName":
                                Name = DetailValue;
                                Link = MangaNameNode.Attributes["href"].Value;
                                if (Link.StartsWith("../manga/?series="))
                                    Link = Link.Substring("../manga/?series=".Length);
                                else if (Link.StartsWith("../read-online/"))
                                    Link = Link.Substring("../read-online/".Length);
                                else
                                    Link = Name.Replace(" ", String.Empty);
                                break;
                            case "Alternate Names": AlternateNames = (from String AltName in DetailValue.Split(',') select AltName.Trim()).ToList(); break;
                            case "Author": AuthorsArtists = DetailValues.ToList(); break;
                            case "Genre": Genres = DetailValues.ToList(); break;
                        }
                    }

                    SearchResults.Add(new SearchResultObject()
                    {
                        Cover = Cover,
                        Name = Name,
                        Url = String.Format("{0}/read-online/{1}", ExtensionDescriptionAttribute.RootUrl, Link),
                        ExtensionName = ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                        Rating = -1,
                        Artists = AuthorsArtists,
                        Authors = AuthorsArtists
                    });
                }
            }

            return SearchResults;
        }
    }
}
