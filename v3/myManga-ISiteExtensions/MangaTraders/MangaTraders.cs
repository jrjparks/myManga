using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System.Text;
using System.Text.RegularExpressions;

namespace MangaTraders
{
    [ISiteExtensionDescription(
        "MangaTraders",
        "mangatraders.org",
        "http://mangatraders.org/",
        RootUrl = "http://mangatraders.org",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public sealed class MangaTraders : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute _SiteExtensionDescriptionAttribute;
        public ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute
        { get { return _SiteExtensionDescriptionAttribute ?? (_SiteExtensionDescriptionAttribute = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public SearchRequestObject GetSearchRequestObject(String searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/advanced-search/result.php?seriesName={1}", SiteExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm)),
                Method = SearchMethod.GET,
                Referer = SiteExtensionDescriptionAttribute.RefererHeader
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


            String Cover = SiteExtensionDescriptionAttribute.RootUrl + MangaObjectNode.SelectSingleNode(".//div[1]/img/@src").Attributes["src"].Value;

            List<ChapterObject> Chapters = new List<ChapterObject>();
            MangaObjectDocument.LoadHtml(MangaChaptersContent);
            HtmlNodeCollection RawChapterList = MangaObjectDocument.DocumentNode.SelectNodes(".//div[contains(@class,'row')]");
            foreach (HtmlNode RawChapterNode in RawChapterList.Skip(1))
            {
                HtmlNode ChapterNumberNode = RawChapterNode.SelectSingleNode(".//div[1]/a"),
                    ReleaseDate = RawChapterNode.SelectSingleNode(".//div[2]/time");
                String ChapterNumber = Regex.Match(ChapterNumberNode.InnerText, @"\d+(\.\d+)?").Value;
                String[] ChapterSub = ChapterNumber.Trim().Split('.');

                ChapterObject Chapter = new ChapterObject()
                {
                    Name = HtmlEntity.DeEntitize(RawChapterNode.SelectSingleNode(".//div[1]/gray").InnerText),
                    Chapter = UInt32.Parse(ChapterSub[0]),
                    Locations =
                    {
                        new LocationObject(){
                            ExtensionName = SiteExtensionDescriptionAttribute.Name, 
                            Url = SiteExtensionDescriptionAttribute.RootUrl + ChapterNumberNode.Attributes["href"].Value
                        },
                    },
                    Released = ReleaseDate.InnerText.ToLower().StartsWith("today") ? DateTime.Today : ReleaseDate.InnerText.ToLower().StartsWith("yesterday") ? DateTime.Today.AddDays(-1) : DateTime.Parse(ReleaseDate.InnerText.ToLower())
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
                Covers = { Cover },
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
            String[] ChapterUrlSections = ChapterUrl.Substring(SiteExtensionDescriptionAttribute.RootUrl.Length).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

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
            HtmlNodeCollection SearchResultNodes = MainContainer.SelectNodes(".//div[contains(@class,'well')]/div[@class='row']");

            foreach (HtmlNode SearchResultNode in SearchResultNodes.Skip(2))
            {
                String ImgUrl = SiteExtensionDescriptionAttribute.RootUrl + SearchResultNode.SelectSingleNode(".//img").Attributes["src"].Value.Substring(2),
                    Name = String.Empty,
                    Link = String.Empty;
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
                    CoverUrl = ImgUrl,
                    Name = Name,
                    Url = String.Format("{0}/read-online/{1}", SiteExtensionDescriptionAttribute.RootUrl, Link),
                    ExtensionName = SiteExtensionDescriptionAttribute.Name,
                    Rating = -1,
                    Artists = AuthorsArtists,
                    Authors = AuthorsArtists
                });
            }

            return SearchResults;
        }
    }
}
