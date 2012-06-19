using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

using BakaBox.Controls;
using BakaBox;
using fastJSON;
using Manga.Plugin;
using Manga.Core;
using Manga;
using Manga.Info;
using Manga.Archive;
using System.ComponentModel;
using System.IO;
using HtmlAgilityPack;

namespace MangaHere
{
    [MangaPlugin]
    [PluginSite("MangaHere")]
    [PluginAuthor("James Parks")]
    [PluginVersion("0.0.1")]
    public class MangaHere : IMangaPluginBase, IMangaPlugin
    {
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public class MangaHereJSON
        {
            public String query { get; set; }
            public String[] suggestions { get; set; }
            public String[] data { get; set; }
        }
        internal class MangaHereData
        {
            public class Data
            {
                public Data(String s, String d)
                {
                    suggestion = s;
                    data = d;
                }
                public String suggestion { get; internal set; }
                public String data { get; internal set; }
            }

            public List<Data> DataList { get; private set; }

            public MangaHereData(MangaHereJSON MangaHereJSON)
            {
                DataList = new List<Data>(MangaHereJSON.data.Length);
                for (int i = 0; i < DataList.Capacity; ++i)
                    DataList.Add(new Data(MangaHereJSON.suggestions[i], MangaHereJSON.data[i]));
            }
        }

        #region IMangaPlugin Vars
        public SupportedMethods SupportedMethods
        {get { return SupportedMethods.All; }}
        public string SiteName
        { get { return "MangaHere"; } }
        public string SiteURLFormat
        { get { return @"mangahere\.com"; } }
        #endregion

        private String ChapterNameRegEx { get { return @"<title>(?<Name>.+?)\s\d"; } }
        private String InfoNameRegEx { get { return @"<title>(?<Name>.+?)\sManga"; } }
        public string SiteRefererHeader { get { return "http://www.mangahere.com/"; } }

        #region IMangaPlugin Members
        public MangaArchiveInfo LoadChapterInformation(string ChapterPath)
        {
            OnProgressChanged(1);
            if (!ChapterPath.StartsWith("http://"))
                ChapterPath = "http://" + ChapterPath;
            if (ChapterPath.EndsWith("/"))
                ChapterPath = ChapterPath.Remove(ChapterPath.LastIndexOf('/'));
            if (ChapterPath.EndsWith(".html"))
                ChapterPath = ChapterPath.Remove(ChapterPath.LastIndexOf('/'));

            MangaArchiveInfo MAI = new MangaArchiveInfo();
            MAI.Site = SiteName;
            String PageHTML,
                RegExImageSearch = @"(?<OnlinePath>http://.+?(mhcdn\.net/store/manga)/([\d\./-]+?)/compressed/)(?<File>(.+?).jpg)",
                VolChapSubChapRegex = @"(v(?<Volume>\d{2,}))?/?(c(?<Chapter>\d{3,}))(\.(?<SubChapter>\d{1,}))?",
                PagePath = ChapterPath + "/{0}.html", 
                TotalPagesRegEx = @"total_pages\s=\s(?<Pages>\d+)";
            OnProgressChanged(3);
            Double Progress = 5, Step;
            OnProgressChanged(4);

            MatchCollection ImageMatches;
            Match _Item = Regex.Match(ChapterPath, VolChapSubChapRegex), NumberOfPagesMatch, InfoMatch;
            MAI.Volume = _Item.Groups["Volume"].Success ? UInt32.Parse(_Item.Groups["Volume"].Value) : 0;
            MAI.Chapter = _Item.Groups["Chapter"].Success ? UInt32.Parse(_Item.Groups["Chapter"].Value) : 0;
            MAI.SubChapter = _Item.Groups["SubChapter"].Success ? UInt32.Parse(_Item.Groups["SubChapter"].Value) : 0;
            OnProgressChanged((Int32)Math.Round(Progress));

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                PageHTML = WebClient.DownloadString(String.Format(PagePath, 1));
                NumberOfPagesMatch = Regex.Match(PageHTML, TotalPagesRegEx);
                InfoMatch = Regex.Match(PageHTML, ChapterNameRegEx);
                
                if (NumberOfPagesMatch.Success)
                {
                    MAI.Name = InfoMatch.Groups["Name"].Value;
                    if (Regex.IsMatch(PageHTML, @"(licensed)(.*?)(not\savailable)"))
                        ThrowLicensed(MAI.Name);

                    MAI.ID = ParseID(PageHTML);
                    UInt32 NumberOfPages = UInt32.Parse(NumberOfPagesMatch.Groups["Pages"].Value);
                    Step = (100D - (Double)Progress) / (Double)NumberOfPages;

                    ImageMatches = Regex.Matches(PageHTML, RegExImageSearch);
                    if (ImageMatches.Count > 0)
                        MAI.PageEntries.Add(new PageEntry()
                        {
                            LocationInfo = new LocationInfo()
                            {
                                FileName = ImageMatches[0].Groups["File"].Value,
                                OnlinePath = ImageMatches[0].Groups["OnlinePath"].Value,
                                AltOnlinePath = ImageMatches[1].Groups["OnlinePath"].Value
                            },
                            PageNumber = 1,
                            Downloaded = false
                        });

                    OnProgressChanged((Int32)Math.Round(Progress += Step), MAI as MangaData);

                    if (NumberOfPages > 1)
                        for (UInt32 page = 2; page <= NumberOfPages; ++page)
                        {
                            do { System.Threading.Thread.Sleep(100); } while (WebClient.IsBusy);
                            WebClient.Headers.Clear();
                            WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                            PageHTML = WebClient.DownloadString(String.Format(PagePath, page));
                            ImageMatches = Regex.Matches(PageHTML, RegExImageSearch);
                            if (ImageMatches.Count > 0)
                                MAI.PageEntries.Add(new PageEntry()
                                {
                                    LocationInfo = new LocationInfo()
                                    {
                                        FileName = ImageMatches[0].Groups["File"].Value,
                                        OnlinePath = ImageMatches[0].Groups["OnlinePath"].Value,
                                        AltOnlinePath = ImageMatches[1].Groups["OnlinePath"].Value
                                    },
                                    PageNumber = page,
                                    Downloaded = false
                                });
                            OnProgressChanged((Int32)Math.Round(Progress += Step));
                        }
                }
                else MAI = null;
            }

            return MAI;
        }

        public MangaInfo LoadMangaInformation(string InfoPage)
        {
            OnProgressChanged(1);
            MangaInfo MI = new MangaInfo() { InfoPage = InfoPage, Site = SiteName};
            String PageHTML;
            OnProgressChanged(3);

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                PageHTML = WebClient.DownloadString(MI.InfoPage);
                HtmlDocument _PageDoc = new HtmlDocument();
                HtmlNode _PageElement;

                _PageDoc.LoadHtml(PageHTML);

                MI.Name = Regex.Match(PageHTML, InfoNameRegEx).Groups["Name"].Value;
                if (Regex.IsMatch(PageHTML, @"(licensed)(.*?)(not\savailable)"))
                    ThrowLicensed(MI.Name);

                MI.ID = ParseID(PageHTML);
                OnProgressChanged(7, MI as MangaData);

                _PageElement = _PageDoc.GetElementbyId("title");
                //Add Info Parser

                MI.ChapterEntries.Clear();
                MI.ChapterEntries = ChapterList(MI);
                MI.ChapterEntries.TrimExcess();
                OnProgressChanged(90);

                ChapterEntry _tmpCE = MI.ChapterEntries[0];
                MI.Volume = _tmpCE.Volume;
                MI.Chapter = _tmpCE.Chapter;
                MI.SubChapter = _tmpCE.SubChapter;
            }
            OnProgressChanged(100);
            return MI;
        }

        public string ManipulateMangaData(MangaData MangaData, DataType ManipulationType)
        {
            String Name = NameToUrlName(MangaData.Name);
            switch (ManipulationType)
            {
                case DataType.ChapterInformation:
                    if (MangaData.SubChapter.Equals(0))
                        return String.Format("http://www.mangafox.me/manga/{0}/v{1}/c{2}/", NameToUrlName(Name), MangaData.Volume, MangaData.Chapter);
                    else
                        return String.Format("http://www.mangahere.com/manga/{0}/v{1}/c{2}.{3}/", NameToUrlName(Name), MangaData.Volume, MangaData.Chapter, MangaData.SubChapter);

                case DataType.MangaInformation:
                    return String.Format("http://www.mangahere.com/manga/{0}/", NameToUrlName(Name));
            }
            return String.Empty;
        }

        public CoverData GetCoverImage(MangaInfo MangaInfo)
        {
            CoverData _Cover = new CoverData();
            String ImagePath = String.Format("http://m.mhcdn.net/store/manga/{0}/cover.jpg", MangaInfo.ID);

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                Stream tmpImage = new MemoryStream(WebClient.DownloadData(ImagePath));
                tmpImage.Position = 0;
                _Cover.CoverStream = new MemoryStream();
                tmpImage.CopyTo(_Cover.CoverStream);
                tmpImage.Close();
            }
            return _Cover;
        }

        public SearchInfoCollection Search(string Text, int Limit)
        {
            Double Progress = 0D, Step;
            SearchInfoCollection SearchCollection = new SearchInfoCollection();
            String SearchPath = String.Format("http://www.mangahere.com/ajax/search.php?query={0}", Text), _Data;
            MangaHereData searchData;

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                _Data = WebClient.DownloadString(SearchPath);
                searchData = new MangaHereData(fastJSON.JSON.Instance.ToObject<MangaHereJSON>(_Data));
                OnProgressChanged((Int32)(Progress = 50));
                Step = Progress / searchData.DataList.Count;

                foreach (MangaHereData.Data Data in searchData.DataList)
                {
                    if (SearchCollection.Count >= Limit) break;
                    UInt32 ID = GetSeriesID(Data.data);
                    if (!TestForLicense(Data.data))
                        SearchCollection.Add(new SearchInfo()
                        {
                            Title = Data.suggestion,
                            InformationLocation = Data.data,
                            ID = ID,
                            CoverLocation = String.Format("http://www.mangahere.com/icon/{0}.jpg", ID)
                        });
                    OnProgressChanged((Int32)(Progress += Step));
                }
            }
            OnProgressChanged(100);
            return SearchCollection;
        }
        #endregion

        private Boolean TestForLicense(String Path)
        {
            Boolean License = false;
            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
                License = Regex.IsMatch(WebClient.DownloadString(Path), @"(licensed)(.*?)(not\savailable)");
            return License;
        }

        private String NameToUrlName(String Name)
        { return Regex.Replace(Name, @"\W+", "_").Trim('_').ToLower(); }

        private UInt32 GetSeriesID(String MangaRoot)
        {
            String sID;
            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                sID = WebClient.DownloadString(MangaRoot);
            }
            return ParseID(sID);
        }
        private UInt32 ParseID(String HTML)
        { return Parse.TryParse<UInt32>(Regex.Match(HTML, @"(?<=series_id=)(?<ID>\d+)").Groups["ID"].Value, 0); }

        private ChapterEntryCollection ChapterList(MangaInfo MangaInfo)
        {
            ChapterEntryCollection _Chapters = new ChapterEntryCollection();
            String ParseRegEx =
                String.Format(
                    "\\[\"(?<VC>[\\w\\d\\s\\.]+?)(\\:(?<Title>.+?))?\",\"http://www\\.mangahere\\.com/manga/\"\\+series_name\\+\"/(?<Location>{0})/\"\\]",
                    @"(v(?<Volume>\d{2,}))?/?(c(?<Chapter>\d{3,}))(\.(?<SubChapter>\d{1,}))?");

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                OnProgressChanged(1);
                String _url = String.Format("http://www.mangahere.com/get_chapters{0}.js", MangaInfo.ID),
                    _ChapterContent = WebClient.DownloadString(_url);
                Double Progress, Step;
                OnProgressChanged((Int32)(Progress = 10));

                MatchCollection _Items = Regex.Matches(_ChapterContent, ParseRegEx);
                Step = (Int32)((100D - Progress) / _Items.Count);
                foreach (Match _Item in _Items)
                {
                    if (_Item.Success)
                        _Chapters.Add(
                            new ChapterEntry()
                            {
                                Name = _Item.Groups["Title"].Value,
                                UrlLink = Path.Combine(MangaInfo.InfoPage, _Item.Groups["Location"].Value).Replace("\\", "/"),
                                Volume = _Item.Groups["Volume"].Success ? UInt32.Parse(_Item.Groups["Volume"].Value) : 0,
                                Chapter = _Item.Groups["Chapter"].Success ? UInt32.Parse(_Item.Groups["Chapter"].Value) : 0,
                                SubChapter = _Item.Groups["SubChapter"].Success ? UInt32.Parse(_Item.Groups["SubChapter"].Value) : 0
                            }
                        );
                    OnProgressChanged((Int32)(Progress += Step));
                }
            }
            OnProgressChanged(100);
            return _Chapters;
        }
    }
}
