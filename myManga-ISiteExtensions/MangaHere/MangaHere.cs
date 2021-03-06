﻿using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MangaHere
{
    [IExtensionDescription(
        Name = "MangaHere",
        URLFormat = "mangahere.co",
        RefererHeader = "http://www.mangahere.co/",
        RootUrl = "http://www.mangahere.co",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public sealed class MangaHere : ISiteExtension
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

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/search.php?name={1}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader
            };
        }

        public MangaObject ParseMangaObject(string content)
        {
            if (content.ToLower().Contains("has been licensed, it is not available in MangaHere.".ToLower()))
                return null;
            HtmlDocument MangaObjectDocument = new HtmlDocument();
            MangaObjectDocument.LoadHtml(content);

            HtmlNode TitleNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//h1[contains(@class,'title')]"),
                MangaDetailsNode = MangaObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'manga_detail')]"),
                MangaPropertiesNode = MangaDetailsNode.SelectSingleNode(".//div[1]"),
                MangaDesciptionNode = MangaObjectDocument.GetElementbyId("show"),
                AuthorsNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[5]"),
                ArtistsNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[6]"),
                GenresNode = MangaPropertiesNode.SelectSingleNode(".//ul/li[4]");
            HtmlNodeCollection AuthorsNodeCollection = AuthorsNode.SelectNodes(".//a"),
                ArtistsNodeCollection = ArtistsNode.SelectNodes(".//a");
            String Desciption = MangaDesciptionNode != null ? MangaDesciptionNode.FirstChild.InnerText : String.Empty,
                MangaName = HtmlEntity.DeEntitize(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(TitleNode.LastChild.InnerText.ToLower()));

            String[] AlternateNames = MangaPropertiesNode.SelectSingleNode(".//ul/li[3]").LastChild.InnerText.Split(new String[] { "; " }, StringSplitOptions.RemoveEmptyEntries),
                Authors = { }, Artists = { },
                Genres = MangaPropertiesNode.SelectSingleNode(".//ul/li[4]").LastChild.InnerText.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (AuthorsNodeCollection != null)
                Authors = (from HtmlNode AuthorNode in AuthorsNodeCollection select HtmlEntity.DeEntitize(AuthorNode.InnerText)).ToArray();
            if (ArtistsNodeCollection != null)
                Artists = (from HtmlNode ArtistNode in ArtistsNodeCollection select HtmlEntity.DeEntitize(ArtistNode.InnerText)).ToArray();

            List<ChapterObject> Chapters = new List<ChapterObject>();
            HtmlNodeCollection RawChapterList = MangaDetailsNode.SelectNodes(".//div[contains(@class,'detail_list')]/ul[1]/li");
            foreach (HtmlNode ChapterNode in RawChapterList)
            {
                String volNode = ChapterNode.SelectSingleNode(".//span[1]/span").InnerText;
                String[] volChapSub = { (volNode != null && volNode.Length > 0) ? volNode.Substring(4).Trim() : "0" };
                String ChapterTitle = ChapterNode.SelectSingleNode(".//span[1]/a").InnerText.Trim();
                String ChapterNumber = ChapterTitle.Substring(ChapterTitle.LastIndexOf(' ') + 1).Trim();
                volChapSub = volChapSub.Concat(ChapterNumber.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();

                DateTime Released = DateTime.Now;
                String ReleasedData = ChapterNode.SelectSingleNode(".//span[2]").InnerText;
                if (ReleasedData.ToLower().Equals("today"))
                { Released = DateTime.Today; }
                else if (ReleasedData.ToLower().Equals("yesterday"))
                { Released = DateTime.Today.AddDays(-1); }
                else
                { Released = DateTime.ParseExact(ReleasedData, "MMM d, yyyy", CultureInfo.InvariantCulture); }

                ChapterObject Chapter = new ChapterObject()
                {
                    Name = ChapterTitle,
                    Volume = UInt32.Parse(volChapSub[0]),
                    Chapter = UInt32.Parse(volChapSub[1]),
                    Locations = {
                        new LocationObject() {
                                ExtensionName = ExtensionDescriptionAttribute.Name,
                                ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                                Url = ChapterNode.SelectSingleNode(".//span[1]/a").Attributes["href"].Value }
                        },
                    Released = Released
                };
                if (volChapSub.Length == 3)
                    Chapter.SubChapter = UInt32.Parse(volChapSub[2]);
                Chapters.Add(Chapter);
            }
            Chapters.Reverse();
            String Cover = MangaPropertiesNode.SelectSingleNode(".//img[1]/@src").Attributes["src"].Value;
            Cover = Cover.Substring(0, Cover.LastIndexOf('?'));

            MangaObject MangaObj = new MangaObject()
            {
                Name = HtmlEntity.DeEntitize(MangaName),
                Description = HtmlEntity.DeEntitize(Desciption),
                AlternateNames = (from AltName in AlternateNames select HtmlEntity.DeEntitize(AltName)).ToList(),
                CoverLocations = { new LocationObject() {
                    Url = Cover,
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    ExtensionLanguage = ExtensionDescriptionAttribute.Language } },
                Authors = Authors.ToList(),
                Artists = Artists.ToList(),
                Genres = Genres.ToList(),
                Released = Chapters.First().Released,
                Chapters = Chapters
            };
            MangaObj.AlternateNames.RemoveAll(an => an.ToLower().Equals("none"));
            MangaObj.Genres.RemoveAll(g => g.ToLower().Equals("none"));
            return MangaObj;
        }

        public ChapterObject ParseChapterObject(string content)
        {
            HtmlDocument ChapterObjectDocument = new HtmlDocument();
            ChapterObjectDocument.LoadHtml(content);

            return new ChapterObject()
            {
                Pages = (from HtmlNode PageNode in ChapterObjectDocument.DocumentNode.SelectNodes("//section[contains(@class,'readpage_top')]/div[contains(@class,'go_page')]/span/select/option")
                         select new PageObject()
                         {
                             Url = PageNode.Attributes["value"].Value,
                             PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText)
                         }).ToList()
            };
        }

        public PageObject ParsePageObject(string content)
        {
            HtmlDocument PageObjectDocument = new HtmlDocument();
            PageObjectDocument.LoadHtml(content);

            HtmlNode PageNode = PageObjectDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'go_page')]/span/select/option[@selected]"),
                PrevNode = PageNode.SelectSingleNode(".//preceding-sibling::option"),
                NextNode = PageNode.SelectSingleNode(".//following-sibling::option");

            String ImgSrc = PageObjectDocument.GetElementbyId("image").Attributes["src"].Value;
            ImgSrc = ImgSrc.Substring(0, ImgSrc.LastIndexOf('?'));
            Uri ImageLink = new Uri(ImgSrc);
            String Name = ImageLink.ToString().Split('/').Last();

            return new PageObject()
            {
                Name = Name,
                PageNumber = UInt32.Parse(PageNode.NextSibling.InnerText),
                Url = PageNode.Attributes["value"].Value,
                NextUrl = (NextNode != null) ? NextNode.Attributes["value"].Value : null,
                PrevUrl = (PrevNode != null) ? PrevNode.Attributes["value"].Value : null,
                ImgUrl = ImageLink.ToString()
            };
        }

        public List<SearchResultObject> ParseSearch(string content)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();
            HtmlDocument SearchResultDocument = new HtmlDocument();
            SearchResultDocument.LoadHtml(content);
            HtmlWeb HtmlWeb = new HtmlWeb();
            HtmlNodeCollection HtmlSearchResults = SearchResultDocument.DocumentNode.SelectNodes(".//div[contains(@class,'result_search')]/dl");
            if (!Equals(HtmlSearchResults, null) && !Equals(HtmlSearchResults[0].InnerText.ToLower(), "No Manga Series".ToLower()))
            {
                foreach (HtmlNode SearchResultNode in HtmlSearchResults)
                {
                    try
                    {
                        String Name = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["rel"].Value,
                            Url = SearchResultNode.SelectSingleNode(".//dt/a[1]").Attributes["href"].Value;
                        HtmlWeb.PreRequest = new HtmlWeb.PreRequestHandler(req =>
                        {
                            req.CookieContainer = new CookieContainer();
                            if (!Equals(Cookies, null))
                                req.CookieContainer.Add(Cookies);
                            req.Method = "POST";
                            req.ContentType = "application/x-www-form-urlencoded";
                            String PayloadContent = String.Format("name={0}", Uri.EscapeDataString(Name));
                            Byte[] PayloadBuffer = Encoding.UTF8.GetBytes(PayloadContent.ToCharArray());
                            req.ContentLength = PayloadBuffer.Length;
                            req.GetRequestStream().Write(PayloadBuffer, 0, PayloadBuffer.Length);

                            return true;
                        });
                        String[] Details = HtmlWeb.Load(
                            String.Format("{0}/ajax/series.php", ExtensionDescriptionAttribute.RootUrl)
                            ).DocumentNode.InnerText.Replace("\\/", "/").Split(new String[] { "\",\"" }, StringSplitOptions.None);
                        LocationObject Cover = new LocationObject()
                        {
                            Url = Details[1].Substring(0, Details[1].LastIndexOf('?')),
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language
                        };
                        Double Rating = -1;
                        Double.TryParse(Details[3], out Rating);

                        SearchResults.Add(new SearchResultObject()
                        {
                            Name = Name,
                            Rating = Rating,
                            Description = HtmlEntity.DeEntitize(Details[8]),
                            Artists = (from String Staff in Details[5].Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries) select Staff.Trim()).ToList(),
                            Authors = (from String Staff in Details[5].Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries) select Staff.Trim()).ToList(),
                            Cover = Cover,
                            Url = Url,
                            ExtensionName = ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = ExtensionDescriptionAttribute.Language
                        });
                    }
                    catch { }
                    finally { HtmlWeb.PreRequest = null; }
                }
            }
            return SearchResults;
        }
    }
}
