using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BakaBox;
using BakaBox.Controls;
using fastJSON;
using HtmlAgilityPack;
using Manga;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;

/* 
 * MangaPanda Manga Plugin for myManga
 * By Reuben Castelino
 */

namespace MangaPanda
{
    [MangaPlugin]
    [PluginSite("MangaPanda")]
    [PluginAuthor("Reuben Castelino")]
    [PluginVersion("0.0.1")]
    public class MangaPanda : IMangaPluginBase, IMangaPlugin
    {
        #region IMangaPlugin Vars
        public string SiteName { get { return "MangaPanda"; } }
        public string SiteURLFormat { get { return @"mangapanda\.com"; } }
        public SupportedMethods SupportedMethods { get { return SupportedMethods.All; } }
        #endregion

        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public class MangaPandaChapterJSON
        {
            public class Item
            {
                public String chapter { get; set; }
                public String chapter_name { get; set; }
                public String chapterlink { get; set; }
            }

            public Item[] data { get; set; }
        }

        private String ChapterNameRegEx
        { get { return "<h2.*href=\"(/(?<ID>\\d+))?/(?<Name>[\\w-]+)(\\.html)?.*title=\".*?>(?<Title>.+)\\sManga</a></h2>"; } }
        private String InfoNameRegEx
        { get { return @"<h1>(?<Name>.*)\sManga</h1>"; } }
        public string SiteRefererHeader { get { return "http://www.mangapanda.com/"; } }

        #region IMangaPlugin Members
        public MangaPanda() { }

        public MangaArchiveInfo LoadChapterInformation(String ChapterPath)
        {
            ChapterPath = ConvertOldChapterUrlToNew(ChapterPath);
            OnProgressChanged(1);

            if (!ChapterPath.StartsWith("http://"))
                ChapterPath = "http://" + ChapterPath;
            if (ChapterPath.EndsWith("/"))
                ChapterPath = ChapterPath.Remove(ChapterPath.LastIndexOf('/'));
            OnProgressChanged(2);
            MangaArchiveInfo MAI = new MangaArchiveInfo();
            MAI.Site = SiteName;
            MAI.PageEntries.Clear();
            OnProgressChanged(3);
            Match vcscMatch = Regex.Match(ChapterPath, @"(http://www\.mangapanda\.com/)([a-z0-9\-]+)/(?<Chapter>\d+)"), PageMatch;
            String PageHTML, Chapter = vcscMatch.Groups["Chapter"].Value,
                RegExImageSearch = @"src=.(?<OnlinePath>(http://i\d{1,4}\.mangapanda\.com/)([a-z0-9\-]+)/(\d+)/)(?<File>[a-z0-9\-]+\.jpg)",
                PagePath = ChapterPath + "/{0}";
            Double Progress = 5, Step = 100D - (Double)Progress;
            OnProgressChanged(4);

            MAI.Volume = 0;
            MAI.Chapter = UInt32.Parse(Chapter);
            MAI.SubChapter = 0;
            OnProgressChanged((Int32)Math.Round(Progress));

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                PageHTML = WebClient.DownloadString(String.Format(PagePath, 1));
                Match NumberOfPagesMatch = Regex.Match(PageHTML, @"of\s(\d+)"), InfoMatch = Regex.Match(PageHTML, ChapterNameRegEx);
                if (NumberOfPagesMatch.Success)
                {
                    UInt32 NumberOfPages = UInt32.Parse(NumberOfPagesMatch.Groups[1].Value);
                    Step /= (Double)NumberOfPages;
                    MAI.Name = InfoMatch.Groups["Title"].Value;
                    if (InfoMatch.Groups["ID"].Success)
                        MAI.ID = UInt32.Parse(InfoMatch.Groups["ID"].Value);
                    else
                        MAI.ID = ChapterId(ChapterPath.Remove(ChapterPath.LastIndexOf('/')));

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
                            while (WebClient.IsBusy) System.Threading.Thread.Sleep(500);
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
            return MAI;
        }

        public MangaInfo LoadMangaInformation(String InfoPage)
        {
            OnProgressChanged(1);
            MangaInfo MI = new MangaInfo() { Site = SiteName, InfoPage = InfoPage };
            MI.Site = SiteName;
            OnProgressChanged(3);

            String PageHTML;
            Double Progress = 10, Step = 99D - (Double)Progress;
            OnProgressChanged((Int32)Math.Round(Progress));

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                PageHTML = WebClient.DownloadString(InfoPage);
                HtmlDocument _PageDoc = new HtmlDocument();
                HtmlNode _PageElement;

                _PageDoc.LoadHtml(PageHTML);

                MI.Name = Regex.Match(PageHTML, InfoNameRegEx).Groups["Name"].Value;

                _PageElement = _PageDoc.GetElementbyId("mangaproperties");
                List<Object> _MangaInfo = new List<Object>();

                foreach (HtmlNode _TR in _PageElement.SelectNodes("//tr/td[2]"))
                {
                    if (_MangaInfo.Count < 8)
                    {
                        _MangaInfo.Add(_TR.InnerText.Trim());
                    }
                    else break;
                }
                _MangaInfo.TrimExcess();

                MI.AltTitle = _MangaInfo[1] as String;
                MI.Released = _MangaInfo[2] as String;
                MI.Status = (_MangaInfo[3].Equals("Ongoing") ? MangaStatus.Ongoing : MangaStatus.Complete);
                MI.Author = _MangaInfo[4] as String;
                MI.Artist = _MangaInfo[5] as String;
                MI.ReadDirection = (_MangaInfo[6].Equals("Right to Left") ?
                    ReadDirection.FromRight : ReadDirection.FromLeft);
                MI.Genre = _MangaInfo[7] as String;

                try { MI.ID = UInt32.Parse(Regex.Match(InfoPage, @"mangapanda\.com/(?<ID>\d+)/[\w-]*.html").Groups["ID"].Value); }
                catch { MI.ID = ChapterId(MI.InfoPage); }

                OnProgressChanged(7, MI as MangaData);

                MI.ChapterEntries.Clear();
                MI.ChapterEntries = ChapterList(MI.ID);
                MI.ChapterEntries.TrimExcess();
                OnProgressChanged(90);

                ChapterEntry _tmpCE = MI.ChapterEntries[0];
                MI.Volume = _tmpCE.Volume;
                MI.Chapter = _tmpCE.Chapter;
                MI.SubChapter = _tmpCE.SubChapter;
                _tmpCE = null;
            }
            OnProgressChanged(100);
            GC.Collect();
            return MI;
        }

        public String ManipulateMangaData(MangaData MangaData, DataType ManipulationType)
        {
            switch (ManipulationType)
            {
                case DataType.ChapterInformation:
                    return String.Format("http://www.mangapanda.com/{0}/{1}", NameToUrlName(MangaData.Name), MangaData.Chapter);

                case DataType.MangaInformation:
                    return String.Format("http://www.mangapanda.com/{0}", NameToUrlName(MangaData.Name));
            }
            return String.Empty;
        }

        public CoverData GetCoverImage(MangaInfo MangaInfo)
        {
            CoverData _Cover = new CoverData();
            String PageHTML,
                FileLocation,
                CoverRegex = @"(?<File>http://s\d\.mangapanda\.com/cover/(?<Name>[\w-]+)/(?<FileName>[\w-]+l\d+?)(?<Extention>\.[\w]{3,4}))";
            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                PageHTML = WebClient.DownloadString(MangaInfo.InfoPage);
                Match coverMatch = Regex.Match(PageHTML, CoverRegex);
                if (coverMatch.Success)
                {
                    FileLocation = coverMatch.Groups["File"].Value;
                    _Cover.CoverName = coverMatch.Groups["FileName"].Value;
                    WebClient.Headers.Clear();
                    WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                    Stream tmpImage = new MemoryStream(WebClient.DownloadData(FileLocation));
                    tmpImage.Position = 0;
                    _Cover.CoverStream = new MemoryStream();
                    tmpImage.CopyTo(_Cover.CoverStream);
                    tmpImage.Close();
                }
            }
            return _Cover;
        }

        /// <summary>
        /// Search Manga Site for Manga with text
        /// </summary>
        /// <param name="Text">Text to search for.</param>
        /// <param name="Limit">Limit results.</param>
        /// <returns>Returns a collection of SearchInfo</returns>
        public SearchInfoCollection Search(String Text, Int32 Limit)
        {
            Double Progress = 0D, Step;
            SearchInfoCollection SearchCollection = new SearchInfoCollection();

            if (100 < Limit) Limit = 100;
            else if (Limit < 1) Limit = 1;
            String SearchPath = String.Format("http://www.mangapanda.com/actions/search/?q={0}&limit={1}", Text, Limit),
                _Data;
            String[] _DataArray;
            OnProgressChanged((Int32)(Progress += 5));

            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                using (Stream _s = new MemoryStream(WebClient.DownloadData(SearchPath)))
                {
                    using (StreamReader _sr = new StreamReader(_s))
                    {
                        Step = (100D - Progress) / Limit;
                        while ((_Data = _sr.ReadLine()) != null && SearchCollection.Count < Limit)
                        {
                            _DataArray = _Data.Split('|');
                            SearchCollection.Add(
                                new SearchInfo(
                                    _DataArray[0],
                                    _DataArray[1],
                                    _DataArray[3],
                                    ConvertOldInformationUrlToNew(String.Format("http://www.mangapanda.com{0}", _DataArray[4])),
                                    UInt32.Parse(_DataArray[5])));
                            OnProgressChanged((Int32)(Progress += Step));
                        }
                    }
                }
            }
            SearchCollection.TrimExcess();
            return SearchCollection;
        }
        #endregion

        private String NameToUrlName(String Name)
        {
            return Name.Replace(' ', '-').ToLower();
        }
        private String ConvertOldInformationUrlToNew(String OldUrl)
        {
            Match OldMatch = Regex.Match(OldUrl, @"/(?<ID>\d+?)/(?<Title>[\w\-]+?).html");
            if (OldMatch.Groups["Title"].Success)
                return String.Format("http://www.mangapanda.com/{0}", OldMatch.Groups["Title"].Value);
            return OldUrl;
        }
        private String ConvertOldChapterUrlToNew(String OldUrl)
        {
            Match OldMatch = Regex.Match(OldUrl, @"/(\d+?\-\d+?\-\d+?)/(?<Title>[\w\-]+)/chapter\-(?<Chapter>\d+).html");
            if (OldMatch.Groups["Title"].Success && OldMatch.Groups["Chapter"].Success)
                return String.Format("http://www.mangapanda.com/{0}/{1}", OldMatch.Groups["Title"].Value, OldMatch.Groups["Chapter"].Value);
            return OldUrl;
        }

        private UInt32 ChapterId(String MangaRoot)
        {
            String _IdString;
            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                _IdString = WebClient.DownloadString(String.Format("{0}/1", MangaRoot));
            }
            return ParseID(_IdString);
        }
        private UInt32 ParseID(String HTML)
        { return Parse.TryParse<UInt32>(Regex.Match(HTML, @"(?:\['mangaid'\].*?)(?<ID>\d+?)(?:;)").Groups["ID"].Value, 0); }

        private ChapterEntryCollection ChapterList(UInt32 ID)
        {
            ChapterEntryCollection _Chapters = new ChapterEntryCollection();
            using (WebClient WebClient = ConfigureWebClient(SiteRefererHeader))
            {
                OnProgressChanged(1);
                String _url = String.Format("http://www.mangapanda.com/actions/selector/?id={0}&which=0", ID),
                    _ChapterContent = "{\"data\":" + WebClient.DownloadString(_url) + "}";
                Int32 Progress, Step;
                OnProgressChanged(Progress = 10);

                MangaPandaChapterJSON _json = JSON.Instance.ToObject<MangaPandaChapterJSON>(_ChapterContent);
                MangaPandaChapterJSON.Item _Item;
                Step = (Int32)((100D - Progress) / _json.data.Length);
                for (Int32 i = 0; i < _json.data.Length; ++i)
                {
                    _Item = _json.data[i];
                    _Chapters.Add(
                        new ChapterEntry()
                        {
                            Chapter = Parse.TryParse<UInt32>(_Item.chapter),
                            Name = _Item.chapter_name,
                            UrlLink = ConvertOldChapterUrlToNew(String.Format("http://www.mangapanda.com{0}", _Item.chapterlink.Replace(@"\/", "/")))
                        });
                    OnProgressChanged(Progress += Step);
                }
                _json = null;
                _Item = null;
            }
            OnProgressChanged(100);
            return _Chapters;
        }
    }
}