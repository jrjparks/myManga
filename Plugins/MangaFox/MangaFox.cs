using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;
using Manga;
using BakaBox.Controls;
using BakaBox;

namespace MangaFox
{
    [MangaPlugin]
    [PluginSite("MangaFox")]
    [PluginAuthor("James Parks")]
    [PluginVersion("0.0.3")]
    public class MangaFox : PluginProgressChanged, IMangaPlugin
    {
        #region IMangaPlugin Vars
        public string SiteName { get { return "MangaFox"; } }
        public string SiteURLFormat { get { return @"mangafox\.me"; } }
        public string SiteRefererHeader { get { return "http://www.mangafox.me/"; } }
        public SupportedMethods SupportedMethods { get { return SupportedMethods.All; } }
        #endregion
        
        private String ChapterNameRegEx { get { return @"(?:>)(?<Name>.+)(?:\sManga</a>)"; } }
        private String InfoNameRegEx { get { return @"<title>(?<Name>.+?)\sManga"; } }

        #region IMangaPlugin Members
        public MangaFox() { }

        public MangaArchiveInfo LoadChapterInformation(String ChapterPath)
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
                RegExImageSearch = @"(?<OnlinePath>http://.+?(mfcdn\.net/store/manga)/([\d\./-]+?)/compressed/)(?<File>(.+?).jpg)",
                VolChapSubChapRegex = @"(v(?<Volume>\d{2,}))?/?(c(?<Chapter>\d{3,}))(\.(?<SubChapter>\d{1,}))?",
                PagePath = ChapterPath + "/{0}.html";
            OnProgressChanged(3);
            Double Progress = 5, Step = 100D - (Double)Progress;
            OnProgressChanged(4);

            Match _Item = Regex.Match(ChapterPath, VolChapSubChapRegex), PageMatch;
            MAI.Volume = _Item.Groups["Volume"].Success ? UInt32.Parse(_Item.Groups["Volume"].Value) : 0;
            MAI.Chapter = _Item.Groups["Chapter"].Success ? UInt32.Parse(_Item.Groups["Chapter"].Value) : 0;
            MAI.SubChapter = _Item.Groups["SubChapter"].Success ? UInt32.Parse(_Item.Groups["SubChapter"].Value) : 0;
            OnProgressChanged((Int32)Math.Round(Progress));

            using (WebClient WebClient = new WebClient())
            {
                WebClient.Encoding = Encoding.UTF8;
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                PageHTML = WebClient.DownloadString(String.Format(PagePath, 1));
                Match NumberOfPagesMatch = Regex.Match(PageHTML, @"of\s(\d+)"), InfoMatch = Regex.Match(PageHTML, ChapterNameRegEx);

                if (NumberOfPagesMatch.Success)
                {
                    MAI.Name = InfoMatch.Groups["Name"].Value;
                    MAI.ID = ParseID(PageHTML);
                    UInt32 NumberOfPages = UInt32.Parse(NumberOfPagesMatch.Groups[1].Value);
                    Step /= (Double)NumberOfPages;

                    PageMatch = Regex.Match(PageHTML, RegExImageSearch);
                    if (PageMatch.Success)
                        MAI.PageEntries.Add(new PageEntry()
                        {
                            LocationInfo = new LocationInfo()
                            {
                                FileName = PageMatch.Groups["File"].Value,
                                OnlinePath = PageMatch.Groups["OnlinePath"].Value
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
                            PageMatch = Regex.Match(PageHTML, RegExImageSearch);
                            if (PageMatch.Success)
                                MAI.PageEntries.Add(new PageEntry()
                                {
                                    LocationInfo = new LocationInfo()
                                    {
                                        FileName = PageMatch.Groups["File"].Value,
                                        OnlinePath = PageMatch.Groups["OnlinePath"].Value
                                    },
                                    PageNumber = page,
                                    Downloaded = false
                                });
                            OnProgressChanged((Int32)Math.Round(Progress += Step));
                        }
                }
                else MAI = null;
            }
            OnProgressChanged(100);
            return MAI;
        }

        public MangaInfo LoadMangaInformation(String InfoPage)
        {
            OnProgressChanged(1);
            MangaInfo MI = new MangaInfo() { InfoPage = InfoPage, Site = SiteName};
            String PageHTML;
            OnProgressChanged(3);

            using (WebClient WebClient = new WebClient())
            {
                WebClient.Encoding = Encoding.UTF8;
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                PageHTML = WebClient.DownloadString(MI.InfoPage);
                HtmlDocument _PageDoc = new HtmlDocument();
                HtmlNode _PageElement;
                
                _PageDoc.LoadHtml(PageHTML);

                MI.Name = Regex.Match(PageHTML, InfoNameRegEx).Groups["Name"].Value;
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

        public String ManipulateMangaData(MangaData MangaData, DataType ManipulationType)
        {
            String Name = NameToUrlName(MangaData.Name);
            switch (ManipulationType)
            {
                case DataType.ChapterInformation:
                    if (MangaData.SubChapter.Equals(0))
                        return String.Format("http://www.mangafox.me/manga/{0}/v{1}/c{2}/", NameToUrlName(Name), MangaData.Volume, MangaData.Chapter);
                    else
                        return String.Format("http://www.mangafox.me/manga/{0}/v{1}/c{2}.{3}/", NameToUrlName(Name), MangaData.Volume, MangaData.Chapter, MangaData.SubChapter);

                case DataType.MangaInformation:
                    return String.Format("http://www.mangafox.me/manga/{0}/", NameToUrlName(Name));
            }
            return String.Empty;
        }

        public CoverData GetCoverImage(MangaInfo MangaInfo)
        {
            CoverData _Cover = new CoverData();
            String CoverRegex = @"(?<File>(http://).+?/(cover\.jpg))",
                PageHTML, FileLocation;
            using (WebClient WebClient = new WebClient())
            {
                WebClient.Encoding = Encoding.UTF8;
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                PageHTML = WebClient.DownloadString(MangaInfo.InfoPage);
                Match coverMatch = Regex.Match(PageHTML, CoverRegex);
                if (coverMatch.Success)
                {
                    FileLocation = coverMatch.Groups["File"].Value;
                    _Cover.CoverName = "Cover.jpg";
                    WebClient.Headers.Clear();
                    WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                    Stream tmpImage = new MemoryStream(WebClient.DownloadData(String.Format("{0}?0", FileLocation)));
                    tmpImage.Position = 0;
                    _Cover.CoverStream = new MemoryStream();
                    tmpImage.CopyTo(_Cover.CoverStream);
                    tmpImage.Close();
                }
            }
            return _Cover;
        }

        public SearchInfoCollection Search(String Text, Int32 Limit)
        {
            Double Progress = 0D, Step;
            SearchInfoCollection SearchCollection = new SearchInfoCollection();
            
            if (100 < Limit) Limit = 100;
            else if (Limit < 1) Limit = 1;
            String SearchPath = String.Format("http://mangafox.me/ajax/search.php?term={0}", Text), _Data;
            String[] _DataArray, _ItemDataArray;
            OnProgressChanged((Int32)(Progress += 5));

            using (WebClient WebClient = new WebClient())
            {
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                WebClient.Encoding = Encoding.UTF8;
                _Data = WebClient.DownloadString(SearchPath);

                _DataArray = _Data.Trim(new char[] { ']', '[' }).Split(new String[] { "],[" }, StringSplitOptions.None);
                Step = (100D - Progress) / _DataArray.Length;
                foreach (String _Match in _DataArray)
                {
                    _ItemDataArray = _Match.Trim('"').Split(new String[] { "\",\"" }, StringSplitOptions.None);
                    SearchCollection.Add(
                                new SearchInfo(
                                    _ItemDataArray[1],
                                    String.Format("http://mangafox.me/icon/{0}.jpg", _ItemDataArray[0]),
                                    _ItemDataArray[4],
                                    String.Format("http://www.mangafox.me/manga/{0}", _ItemDataArray[2]),
                                    UInt32.Parse(_ItemDataArray[0])));
                    OnProgressChanged((Int32)(Progress += Step));
                }
                SearchCollection.TrimExcess();
            }
            return SearchCollection;
        }
        #endregion

        private String NameToUrlName(String Name)
        { return Regex.Replace(Name, @"\W+", "_").Trim('_').ToLower(); }

        private UInt32 ChapterId(String MangaRoot)
        {
            String _IdString;
            using (WebClient WebClient = new WebClient())
            {
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                WebClient.Encoding = Encoding.UTF8;
                _IdString = WebClient.DownloadString(MangaRoot);
            }
            return ParseID(_IdString);
        }
        private UInt32 ParseID(String HTML)
        { return Parse.TryParse<UInt32>(Regex.Match(HTML, @"sid=(?<ID>\d+?);").Groups["ID"].Value, 0); }

        private ChapterEntryCollection ChapterList(MangaInfo MangaInfo)
        {
            ChapterEntryCollection _Chapters = new ChapterEntryCollection();
            String ParseRegEx = 
                String.Format(
                "\\[\"(?<VC>[\\w\\d\\s\\.]+?)(\\:(?<Title>.+?))?\",\"(?<Location>{0})\"\\]",
                @"(v(?<Volume>\d{2,}))?/?(c(?<Chapter>\d{3,}))(\.(?<SubChapter>\d{1,}))?");
            using (WebClient WebClient = new WebClient())
            {
                OnProgressChanged(1);
                WebClient.Encoding = Encoding.UTF8;
                WebClient.Headers.Clear();
                WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                String _url = String.Format("http://mangafox.me/media/js/list.{0}.js", MangaInfo.ID),
                    _ChapterContent = WebClient.DownloadString(_url);
                Int32 Progress, Step;
                OnProgressChanged(Progress = 10);

                MatchCollection _Items = Regex.Matches(_ChapterContent, ParseRegEx);
                Step = (Int32)((100D - Progress) / _Items.Count);
                foreach (Match _Item in _Items)
                {
                    if (_Item.Success)
                        _Chapters.Add(
                            new ChapterEntry()
                            {
                                Name = _Item.Groups["Title"].Value,
                                UrlLink = Path.Combine(MangaInfo.InfoPage, _Item.Groups["Location"].Value).Replace("\\","/"),
                                Volume = _Item.Groups["Volume"].Success ? UInt32.Parse(_Item.Groups["Volume"].Value) : 0,
                                Chapter = _Item.Groups["Chapter"].Success ? UInt32.Parse(_Item.Groups["Chapter"].Value) : 0,
                                SubChapter = _Item.Groups["SubChapter"].Success ? UInt32.Parse(_Item.Groups["SubChapter"].Value) : 0
                            }
                        );
                    OnProgressChanged(Progress += Step);
                }
            }
            OnProgressChanged(100);
            return _Chapters;
        }
    }
}
